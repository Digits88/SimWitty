using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using PacketDotNet; // Reference
using SharpPcap; // Reference

namespace SimWitty.Services.Swsyslog
{
    /// <summary>
    /// Packet capture class for the Pcap creation thread
    /// </summary>
    public class SyslogCapture : SimWitty.Library.Collector.SimWittyThread
    {
        private static string _captureToFolder = "";
        private static string _machineName = "";
        private static string _interfaceID = "";
        private static string _syslogFilter = "";

        // Track how many packets have been captured and how many should be captured
        // Used in device_PcapOnPacketArrival(..)
        private static int _logEntriesPerCapture = 0;
        private static int _logEntryCount = 0;

        private static StreamWriter outputFile;

        public SyslogCapture(SimWitty.Library.Collector.SimWittyService parentService, string InterfaceID, string SyslogFilter, int PacketsPerCapture)
        {
            this.parentService = parentService;
            _machineName = parentService.MachineName;
            _captureToFolder = parentService.CommonInputFolder.FullName;
            _interfaceID = InterfaceID;
            _syslogFilter = SyslogFilter;
            _logEntriesPerCapture = PacketsPerCapture;
         
        }


        /// <summary>
        /// Causes a thread to be scheduled for execution.
        /// </summary>
        protected override void ExecuteTask()
        {
            // Get the local drive for checking disk space
            DirectoryInfo folder = new DirectoryInfo(_captureToFolder);
            DriveInfo volume = new DriveInfo(folder.Root.ToString());

            // Determine how much space is needed

            long NeededSpace = 0;
            NeededSpace = (_logEntriesPerCapture + 1000 - 1) / 1000; // 1k packets (rounded up)
            NeededSpace = NeededSpace * 1048576; // 1 MB per 1k packets
            NeededSpace = NeededSpace * 2; // Double for enough free space for one capture with room remaining
            
            // Does the folder exist? 

            if (!folder.Exists)
            {
                this.parentService.Log(EventLogEntryType.Error, 1, "The folder '" + _captureToFolder + "' specified in the configuration file is invalid.");
                this.parentService.Stop();
                return;
            }
                
            int numMillisecondsToCapture = 1000;
            
            // Build the name (Computer _ Interface _ date/time)
            // Use the computer in the name to support capturing from multiple computers
            // Use the interface in the name to support capturing from multiple interfaces on one computer

            int idx = _interfaceID.IndexOf("{") + 1;
            int len = 36; // 36 character guid
            if (idx == 0 || _interfaceID.Length < len)
            {
                this.parentService.Log(EventLogEntryType.Error, 1, "The interface name '" + _interfaceID + " specified in the configuration file is invalid.");
                this.parentService.Stop();
                return;
            }

            string logFile = String.Format("{0}\\{1}_{2}_Syslog_",
                _captureToFolder,
                _machineName,
                _interfaceID.Substring(idx, len));

            // Validate that the interface can be opened for packet capture

            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                this.parentService.Log(EventLogEntryType.Error, 1, "The computer did not have the necessary network interface cards for packet capturing.");
                this.parentService.Stop();
                return;
            }

            bool foundNic = false;
            ICaptureDevice device;
            int i = 0;
            foreach (ICaptureDevice dev in devices)
            {
                if (dev.Name == _interfaceID)
                {
                    foundNic = true;
                    break;
                }
                i++;
            }

            if (!foundNic)
            {
                this.parentService.Log(EventLogEntryType.Error, 1, "The computer does not have an interface named '" + _interfaceID + "'. Please check the config file.");
                this.parentService.Stop();
                return;
            }
            else
            {
                this.parentService.Log("Collecting syslog traffic from the network interface '" + _interfaceID + "'");
                device = devices[i];
            }

            // Open the device and begin capturing

            do
            {
                try
                {
                    // Check available disk space
                    if (NeededSpace > volume.AvailableFreeSpace)
                    {
                        this.parentService.Log(EventLogEntryType.Error, 1, "The disk drive " + folder.Root.ToString() + " is too low on disk space to capture network traffic.");
                        this.parentService.Stop();
                        break;
                    }
                    
                    // Grab the date/time and build the capture file
                    string timeStamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmssffff");
                    string capFile = logFile + timeStamp + ".tmp";

                    /// Handler kick off for dumpfile routine
                    device.OnPacketArrival +=
                        new PacketArrivalEventHandler(device_PcapOnPacketArrival);

                    // Open the log file
                    outputFile = new StreamWriter(File.Open(capFile, FileMode.Create));

                    // Start capturing a number of packets
                    device.Open(DeviceMode.Promiscuous, numMillisecondsToCapture);
                    device.Filter = _syslogFilter;
                    device.StartCapture();

                    do
                    {
                        if (_logEntryCount >= _logEntriesPerCapture) { break; }
                        if (!this.Running) { break; }
                        System.Threading.Thread.Sleep(1);
                    } while (true);

                    // Close the pcap device
                    device.StopCapture();
                    device.Close();
                    _logEntryCount = 0;

                    // Rename the file as a pcap file
                    outputFile.Close();
                    FileInfo logDataFile = new FileInfo(capFile);
                    logDataFile.MoveTo(Path.ChangeExtension(capFile, "csv"));
                }
                catch (Exception ex)
                {
                    // Warning only: do not stop the service with a this.parentService.Stop();
                    int pauseminutes = 5;
                    this.parentService.Log(EventLogEntryType.Warning, "Pausing data capture for " + pauseminutes.ToString() + " minutes because of an exception during packet capture." + Environment.NewLine + ex.ToString());
                    System.Threading.Thread.Sleep(pauseminutes * 60 * 1000);
                }

            } while (this.Running);

            // End the thread and return
            this.Stop();
        }

        /// <summary>
        /// Dumps each received packet to a pcap file
        /// </summary>
        private static void device_PcapOnPacketArrival(object sender, SharpPcap.CaptureEventArgs e)
        {

            SyslogMessage msg = null;

            try
            {
                Packet link =  Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data);
                EthernetPacket ethernet = (EthernetPacket)link;
                IpPacket ip = (IpPacket)ethernet.PayloadPacket;
                UdpPacket udp = (UdpPacket)ip.PayloadPacket;
                msg = new SyslogMessage(udp);
                outputFile.WriteLine(System.Text.Encoding.ASCII.GetString(udp.PayloadData));
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SwsyslogService", ex.ToString(), EventLogEntryType.Error);
            }


            // if the output file is ready to write, write the log file ...
            if (msg != null && outputFile.BaseStream.CanWrite)
            {
                //outputFile.WriteLine(msg.ToString());
            }

            _logEntryCount++;
        }

    }
}