using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using PacketDotNet; // reference
using SharpPcap; // reference


namespace SimWitty.Services.Swpackets
{

    partial class SwpacketsService : SimWitty.Library.Collector.SimWittyService
    {
        PacketStatistics[] stats;
        PacketCapture[] caps;
        int captureThreadCount = 0; // Number of threads used for capturing network traffic and generating pcap files.
        int statisticsThreadCount = 0; // Number of threads used for processing pcap files and generating csv statistics. 
        int packetsPerCapture = 0; // Minimum number of packets per capture

        public SwpacketsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Check the folder paths
            // The method will log an error and return null if the folder is missing or has the wrong permissions

            if (this.CommonErrorsFolder == null) { return; }
            if (this.CommonInputFolder == null) { return; }
            if (this.CommonOutputFolder == null) { return; }
            if (this.SourceErrorsFolder == null) { return; }
            if (this.SourceInputFolder == null) { return; }
            if (this.SourceOutputFolder == null) { return; }


            // Read the values from the app.config file

            string interfaceID = ""; 
            
            try
            {
                interfaceID = System.Configuration.ConfigurationManager.AppSettings["InterfaceID"];
            }
            catch (Exception ex)
            {
                Log(EventLogEntryType.Error, "Unable to read the \"InterfaceID\" value the configuration file." + Environment.NewLine + ex.ToString());
                return;
            }

            try
            {
                packetsPerCapture = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["PacketPerCapture"]);
            }
            catch
            {
                Log(EventLogEntryType.Error, "Unable to read the \"PacketPerCapture\" value from the configuration file.");
                return;
            }

            try
            {
                captureThreadCount = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["CaptureThreadCount"]);
            }
            catch
            {
                captureThreadCount = 0; // Default to not capturing packets.
                Log(EventLogEntryType.Error, "Unable to read the \"CaptureThreadCount\" value from the configuration file. Defaulting to {0}.", captureThreadCount.ToString());
            }

            try
            {
                statisticsThreadCount = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["StatisticsThreadCount"]);
            }
            catch
            {
                statisticsThreadCount = Environment.ProcessorCount; // Default to one thread per processor core.
                Log(EventLogEntryType.Error, "Unable to the read \"StatisticsThreadCount\" value from the configuration file. Defaulting to {0}.", statisticsThreadCount.ToString());
            }

            // Get the list of interfaces from the comma separated value in the config file, and ensure the threads do not exceed the interfaces.
            string[] interfaces = interfaceID.Split(',');
            if (captureThreadCount > interfaces.Length) { captureThreadCount = interfaces.Length; }
          

            
            Log("The service is ready to capture network packets." + Environment.NewLine +
                "Version: " + this.ServiceVersion + Environment.NewLine +
                "MachineName: " + this.MachineName + Environment.NewLine +
                "CaptureToFolder: " + this.SourceInputFolder.FullName + Environment.NewLine +
                "InterfaceID: " + interfaceID);

            // Start the capture thread(s)
            caps = new PacketCapture[captureThreadCount];
            for (int i = 0; i < captureThreadCount; i++)
            {
                caps[i] = new PacketCapture(this, interfaces[i], packetsPerCapture);
                caps[i].RequestStart();
            }
            
            // Start the statistics thread(s)
            stats = new PacketStatistics[statisticsThreadCount];
            for (int i = 0; i < statisticsThreadCount; i++)
            {
                stats[i] = new PacketStatistics(this);
                stats[i].RequestStart();
                System.Threading.Thread.Sleep(1000);
            }

        }

        protected override void OnStop()
        {
            // Stop the capture thread(s)
            for (int i = 0; i < captureThreadCount; i++)
            {
                caps[i].RequestStop();
            }

            // Stop the statistics thread(s)
            for (int i = 0; i < statisticsThreadCount; i++)
            {
                stats[i].RequestStop();
            }

        }
    }
}
