using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SimWitty.Services.Swsyslog
{
    partial class SwsyslogService : SimWitty.Library.Collector.SimWittyService
    {
        public SwsyslogService()
        {
            InitializeComponent();
        }

        SyslogCapture syslog;


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

            int logsPerCsv = 0;
            try
            {
                logsPerCsv = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LogsPerCsv"]);
            }
            catch
            {
                Log(EventLogEntryType.Error, "Unable to read the \"LogsPerCsv\" value from the configuration file.");
                return;
            }


            string syslogFilter = "";
            try
            {
                syslogFilter = System.Configuration.ConfigurationManager.AppSettings["SyslogFilter"];
            }
            catch
            {
                Log(EventLogEntryType.Error, "Unable to read the \"SyslogFilter\" value from the configuration file.");
                return;
            }


            syslog = new SyslogCapture(this, interfaceID, syslogFilter, logsPerCsv);
            syslog.RequestStart();

            Log("The service is now listening for Syslog traffic." + Environment.NewLine +
                "Version: " + this.ServiceVersion + Environment.NewLine +
                "MachineName: " + this.MachineName + Environment.NewLine +
                "CaptureToFolder: " + this.CommonInputFolder.FullName);
        }

        protected override void OnStop()
        {
            syslog.RequestStop();
        }
    }
}
