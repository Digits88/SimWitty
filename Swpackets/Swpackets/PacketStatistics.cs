using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Numerics;
using PacketDotNet; // reference
using SharpPcap; // reference

namespace SimWitty.Services.Swpackets
{
    public enum HttpMethod
    {
        UNKNOWN,
        OPTIONS,
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
        TRACE,
        CONNECT
    };

    /// <summary>
    /// Packet statistics class for the Csv creation thread
    /// </summary>
    public class PacketStatistics : SimWitty.Library.Collector.SimWittyThread
    {

        // Volatile is used as hint to the compiler that this data
        // member will be accessed by multiple threads.
        private volatile string _captureToFolder = ""; // This is the folder the pcap files will be queued into.
        private volatile string _processToFolder = ""; // When processed, the pcap files will move to this folder.
        private volatile string _statisticsToFolder = ""; // When processed, the resulting csv statistics files will move to this folder.

        public PacketStatistics(SimWitty.Library.Collector.SimWittyService parentService)
        {
            this.parentService = parentService;
            _captureToFolder = parentService.SourceInputFolder.FullName;
            _processToFolder = parentService.SourceOutputFolder.FullName;
            _statisticsToFolder = parentService.CommonInputFolder.FullName;
        }
               

        /// <summary>
        /// Causes a thread to be scheduled for execution.
        /// </summary>
        protected override void ExecuteTask()
        {
            string ver = SharpPcap.Version.VersionString;

            // Get the local processing folders
            DirectoryInfo captureDir = new DirectoryInfo(_captureToFolder);
            DirectoryInfo processDir = new DirectoryInfo(_processToFolder);
            DirectoryInfo outputDir = new DirectoryInfo(_statisticsToFolder);

            // Get the local drive for checking disk space
            DriveInfo volume = new DriveInfo(outputDir.Root.ToString());


            FileInfo pcapFile = null;
            string pcapFilename = "";
            string pcapFilter = "*.pcap";

            DateTime start = DateTime.Now;

            do
            {
                //Check if there are files if not wait 10 sec then check again
                do
                {
                    TimeSpan timer = DateTime.Now - start;
                    int seconds = (int)timer.TotalSeconds;
                    Console.WriteLine("-> {0} seconds to process.", seconds.ToString());
                    start = DateTime.Now;

                    // Null the file until we find one that is unlocked
                    pcapFile = null;
                    try
                    {
                        foreach (FileInfo p in captureDir.GetFiles(pcapFilter))
                        {
                            Console.WriteLine(p.FullName);

                            if (!this.Running) { break; } 

                            // Check to see if the file is locked using a file stream and FileShare.None
                            // If the file is not locked, keep it locked for at least a second

                            FileStream fs = new FileStream(p.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                            fs.Lock(0, fs.Length);
                            System.Threading.Thread.Sleep(1000);

                            // Get the file a
                            pcapFile = p;
                            pcapFilename = processDir.FullName + "\\" + pcapFile.Name;
                            long NeededSpace = p.Length * 2; // Double for enough free space for one capture with room remaining



                            // Unlock and close
                            fs.Unlock(0, fs.Length);
                            fs.Close();

                            // Check available disk space
                            if (NeededSpace > volume.AvailableFreeSpace)
                            {
                                this.parentService.Log(EventLogEntryType.Error, 1, "The disk drive " + outputDir.Root.ToString() + " is too low on disk space to analyze packet capture statistics.");
                                this.parentService.Stop();
                                return;
                            }
                    
                            // Move the file for processing
                            pcapFile.MoveTo(pcapFilename);
                            break;
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        // The file selected is locked by another thread or process
                        pcapFile = null;
                    }
                    catch (Exception ex)
                    {
                        // Log any errors
                        this.parentService.Log(EventLogEntryType.Warning,
                            "{0} thread ({1}) error opening pcap file.\n{2}",
                            this.ThreadName,
                            this.ThreadId.ToString(),
                            ex.ToString());

                        pcapFile = null;
                    }

                    // If we have gotten this far, then no files are available or all files are locked
                    // Sleep the thread
                    if (!(pcapFile == null))
                        System.Threading.Thread.Sleep(10000);

                } while (pcapFile == null);

                parsePcap(pcapFilename, outputDir);

            } while (this.Running);

            // End the thread and return
            this.Stop();
        }

        /// <summary>
        /// Return an unsigned BigInteger from an IP address
        /// </summary>
        /// <param name="ip">Ethernet MAC address</param>
        /// <returns>BigInteger value of the address</returns>
        private static BigInteger GetAddressNumber(System.Net.NetworkInformation.PhysicalAddress mac)
        {
            return GetAddressNumber(mac.GetAddressBytes());
        }

        /// <summary>
        /// Return an unsigned BigInteger from an IP address
        /// </summary>
        /// <param name="ip">IPv4 or IPv6 address</param>
        /// <returns>BigInteger value of the address</returns>
        private static BigInteger GetAddressNumber(System.Net.IPAddress ip)
        {
            return GetAddressNumber(ip.GetAddressBytes());
        }
        
        /// <summary>
        /// Return an unsigned BigInteger from byte array
        /// </summary>
        /// <param name="ip">Byte array representing a network address</param>
        /// <returns>BigInteger value of the address</returns>
        private static BigInteger GetAddressNumber(byte[] bytesin)
        {

            int len = bytesin.Length;
            byte[] bytesout = new byte[len + 1];

            // Populate bytesout with the reverse of bytesin
            // The reason is that IP Addresses and BigInteger use different endians

            for (int i = 0; i < bytesin.Length; i++)
            {
                bytesout[i] = bytesin[len - i - 1];
            }

            // BigInteger is signed by default
            // Add a zero-byte value to the end of the array to create unsigned
            bytesout[len] = (Byte)0;

            // Return the BigInt, unsigned, from IP address
            return new BigInteger(bytesout);
        }


        /// <summary>
        /// Parse packet capture (.pcap) file and create statistics output (.csv).
        /// </summary>
        /// <param name="filename">Packet capture (.pcap) file</param>
        /// <param name="outputFoldername">Folder to write output (.csv) file</param>
        static void parsePcap(string filename, DirectoryInfo outputFolder)
        {
            OfflineCaptureDevice pcapfile = new OfflineCaptureDevice(filename);
            pcapfile.Open();
            
            // Create a statistics file in the output statistics folder
            string shortname = System.IO.Path.GetFileNameWithoutExtension(pcapfile.Name);


            string netCsvFilename =
                outputFolder.FullName + "\\" +
                shortname +
                "-net.csv";

            string appCsvFilename =
                outputFolder.FullName + "\\" +
                shortname +
                "-app.csv";

            // It is possible that the pcap file is in use by another thread
            // This can be detected by checking to see if the .csv file exists
            // Return and find another pcap if it does

            if (File.Exists(netCsvFilename)) return;
            if (File.Exists(appCsvFilename)) return;

            // Create stream writers for the output

            StreamWriter netCsvWriter = new StreamWriter(netCsvFilename);
            netCsvWriter.WriteLine("Pcap,DateStamp,TimeIndex,srcMAC,dstMAC,srcIP,srcIPstr,dstIP,dstIPstr,srcPORT,dstPORT,Type,Bytes,AppBytes");

            StreamWriter appCsvWriter = new StreamWriter(appCsvFilename);
            appCsvWriter.WriteLine("Pcap,DateStamp,TimeIndex,srcMAC,dstMAC,srcIP,srcIPstr,dstIP,dstIPstr,value1,value2,value3,value4");
            
            // Loop thru the packets and record the statistics

            RawCapture next = null;

            while (true)
            {
                try
                {
                    if ((next = pcapfile.GetNextPacket()) == null) break;
                }
                catch
                {
                    continue;
                }

                // TCP/IP Communications

                string srcMACstr = "unknown"; // Hexadecimal string
                string srcIP = "";
                string srcIPstr = ""; // Hexadecimal string
                int srcPort = 0;

                string dstMACstr = "unknown"; // Hexadecimal string
                string dstIP = "";
                string dstIPstr = ""; // Hexadecimal string
                int dstPort = 0;

                DateTime time = next.Timeval.Date;
                Int64 linkByteCount = 0;
                Int64 appByteCount = 0;
                int itype = 0;
                string appLayerStats = "";

                // Ethernet packets

                if (next.LinkLayerType != LinkLayers.Ethernet)
                    continue;

                if (next.Data.Length < 20)
                    continue;

                Packet link = null;
                EthernetPacket ethernet = null;

                try
                {
                    link = Packet.ParsePacket(LinkLayers.Ethernet, next.Data);
                    ethernet = (EthernetPacket)link;
                }
                catch 
                {
                    continue;
                }

                srcMACstr = ethernet.SourceHwAddress.ToString();
                dstMACstr = ethernet.DestinationHwAddress.ToString();


                linkByteCount = link.Bytes.Length;

                if (link.PayloadPacket is IpPacket)
                {
                    IpPacket ip = (IpPacket)link.PayloadPacket;
                    
                    srcIP = ip.SourceAddress.ToString();
                    srcIPstr = SimWitty.Library.Core.Encoding.Base16.ToBase16String(ip.SourceAddress.GetAddressBytes());
                    
                    dstIP = ip.DestinationAddress.ToString();
                    dstIPstr = SimWitty.Library.Core.Encoding.Base16.ToBase16String(ip.DestinationAddress.GetAddressBytes());

                    if (ip.PayloadPacket is TcpPacket)
                    {
                        TcpPacket tcp = (TcpPacket)ip.PayloadPacket;
                        srcPort = tcp.SourcePort;
                        dstPort = tcp.DestinationPort;
                        appByteCount = tcp.PayloadData.Length;
                        itype = 1;
                        appLayerStats = ApplicationLayerHandling(itype, dstPort, tcp.PayloadData);
                    }

                    if (ip.PayloadPacket is UdpPacket)
                    {
                        UdpPacket udp = (UdpPacket)ip.PayloadPacket;
                        srcPort = udp.SourcePort;
                        dstPort = udp.DestinationPort;
                        appByteCount = udp.PayloadData.Length;
                        itype = 2;
                    }

                }
                // "Pcap,DateStamp,TimeIndex,srcMAC,dstMAC,srcIP,srcIPstr,dstIP,dstIPstr,srcPORT,dstPORT,Type,Bytes,AppBytes"
                netCsvWriter.WriteLine("{0},{1} {2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                    shortname,
                    time.ToShortDateString(),
                    time.ToLongTimeString(),
                    time.Hour + "_" + time.Minute,
                    srcMACstr,
                    dstMACstr,
                    srcIP,
                    srcIPstr,
                    dstIP,
                    dstIPstr,
                    srcPort,
                    dstPort,
                    itype.ToString(),
                    linkByteCount.ToString(),
                    appByteCount.ToString()
                    );

                // "Pcap,DateStamp,TimeIndex,srcMAC,srcMACstr,dstMAC,dstMACstr,srcIP,srcIPstr,dstIP,dstIPstr,value1,value2,value3,value4"
                if (appLayerStats.Length != 0)
                {
                    appCsvWriter.WriteLine("{0},{1} {2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        shortname,
                        time.ToShortDateString(),
                        time.ToLongTimeString(),
                        time.Hour + "_" + time.Minute,
                        srcMACstr,
                        dstMACstr,
                        srcIP,
                        srcIPstr,
                        dstIP.ToString(),
                        dstIPstr,
                        appLayerStats);
                }

            }

            // Close the output
            netCsvWriter.Flush();
            netCsvWriter.Close();
            appCsvWriter.Flush();
            appCsvWriter.Close();

            // Close the pcap device
            pcapfile.Close();

            // The pause that refreshes
            System.Threading.Thread.Sleep(1000);

        }





        /// <summary>
        /// Call subroutines based on communications port. MUST RETURN a four column csv value v,v,v,v.
        /// </summary>
        /// <param name="transport">TCP (1) or UDP (2)</param>
        /// <param name="destinationPort">Port number (1-65,535)</param>
        /// <param name="PayloadData">Network packet data in byte array</param>
        private static string ApplicationLayerHandling(
            int transport,
            int destinationPort,
            byte[] PayloadData)
        {
            // Currently only working with TCP (1), so exit if otherwise
            if (transport != 1)
                return "";

            string entry = "";

            // Select the application layer protocol by port
            switch (destinationPort)
            {
                // HTTP port 80, HTTPS port 443
                case 80:
                case 443:
                    entry = parseHttp(PayloadData);
                    break;

                default:
                    break;
            }

            return entry;
        }

        /// <summary>
        /// Parse HTTP packet payload for ApplicationLayerHandling(..)
        /// </summary>
        /// <param name="tcpPayloadData">HTTP data packet in byte array</param>
        /// <returns>MUST RETURN a four column csv value v,v,v,v</returns>
        private static string parseHttp(byte[] tcpPayloadData)
        {
            if (tcpPayloadData.Length == 0)
                return "";

            char[] seperator = { '\r', '\n' };
            char[] c = Encoding.GetEncoding(1252).GetChars(tcpPayloadData);
            string ansi = new string(c);
            string[] lines = ansi.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 1) return "";

            // Determine the HTTP Request method (and exit if this is not a defined HTTP request)
            HttpMethod hm = HttpMethod.UNKNOWN;

            // Cool trick: convert the enum to a string and foreach thru the values
            foreach (string method in Enum.GetNames(typeof(HttpMethod)))
            {
                if (SimWitty.Library.Core.Tools.StringTools.StartsWith(lines[0], method))
                {
                    // Convert the enum string back to the enum value
                    hm = (HttpMethod)Enum.Parse(typeof(HttpMethod), method, true);
                    break;
                }
            }

            if (hm == HttpMethod.UNKNOWN)
                return "";

            // The first line is the request line
            //  Request-Line = Method SP Request-URI SP HTTP-Version CRLF

            string[] requestline = lines[0].Split(' ');

            if (requestline.Length != 3)
                return "";

            string uri = requestline[1];
            string httpver = requestline[2];
            string host = "";

            // Avoid the quote being escaped out

            if (uri.Substring(uri.Length - 1, 1) == "\\")
                uri += " ";

            // Get the host

            foreach (string line in lines)
            {
                if (SimWitty.Library.Core.Tools.StringTools.StartsWith(line, "Host:"))
                {
                    if (line.Length > 6)
                    {
                        host = line.Substring(6);
                        break;
                    }
                }
            }

            // Output 
            return String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                httpver,
                hm.ToString(),
                host,
                uri);
        }
    }
}
