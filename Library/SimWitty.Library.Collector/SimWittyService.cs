// <copyright file="SimWittyService.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
    using System.ServiceProcess;
    
    /// <summary>
    /// Extends the ServiceBase class for SimWitty collector services.
    /// </summary>
    public partial class SimWittyService : ServiceBase
    {
        /// <summary>
        /// Errors folder for common file formatted messages.
        /// </summary>
        private DirectoryInfo commonErrorsFolder = null;

        /// <summary>
        /// Input folder for common file formatted messages.
        /// </summary>
        private DirectoryInfo commonInputFolder = null;

        /// <summary>
        /// Output folder for common file formatted messages.
        /// </summary>
        private DirectoryInfo commonOutputFolder = null;

        /// <summary>
        /// Errors folder for native source formatted messages.
        /// </summary>
        private DirectoryInfo sourceErrorsFolder = null;

        /// <summary>
        /// Input folder for native source formatted messages.
        /// </summary>
        private DirectoryInfo sourceInputFolder = null;

        /// <summary>
        /// Output folder for native source formatted messages.
        /// </summary>
        private DirectoryInfo sourceOutputFolder = null;

        /// <summary>
        /// The version information for the collector service.
        /// </summary>
        private System.Version serviceVersion;

        /// <summary>
        /// The name string for the collector service.
        /// </summary>
        private string serviceName = string.Empty;

        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SimWittyService"/> class.
        /// </summary>
        public SimWittyService()
        {
        }

        #endregion  

        #region Properties

        /// <summary>
        /// Gets or sets the Service Name.
        /// </summary>
        public new string ServiceName 
        {
            get
            {
                if (this.serviceName.Length == 0) this.FillServiceName();
                return this.serviceName;
            }

            set
            {
                this.serviceName = value;
            }
        }

        /// <summary>
        /// Gets the service's assembly version. Use ToString() to export the version.
        /// </summary>
        public System.Version ServiceVersion
        {
            get
            {
                if (this.serviceVersion == null)
                {
                    System.Reflection.Assembly thisAssembly = this.GetType().Assembly;
                    this.serviceVersion = thisAssembly.GetName().Version;
                }

                return this.serviceVersion;
            }
        }

        /// <summary>
        /// Gets the folder that contains common files (.CSV) that have generated errors by the SSIS package. 
        /// </summary>
        public System.IO.DirectoryInfo CommonErrorsFolder
        {
            get
            {
                if (this.commonErrorsFolder == null) this.LoadFoldersFromConfig();
                return this.commonErrorsFolder;
            }
        }

        /// <summary>
        /// Gets the common folder. When source files are processed by the service, the common files (.CSV) are created in this folder. SSIS package uses this folder as an input.
        /// </summary>
        public System.IO.DirectoryInfo CommonInputFolder
        {
            get
            {
                if (this.commonInputFolder == null) this.LoadFoldersFromConfig();
                return this.commonInputFolder;
            }
        }

        /// <summary>
        /// Gets the common folder. The folder that contains common files (.CSV) that have been processed by SSIS. SSIS package use the folder as an output.
        /// </summary>
        public System.IO.DirectoryInfo CommonOutputFolder
        {
            get
            {
                if (this.commonOutputFolder == null) this.LoadFoldersFromConfig();
                return this.commonOutputFolder;
            }
        }

        /// <summary>
        /// Gets the computer name used by the collection service. Returns the local computer name unless overridden in the app.config.
        /// </summary>
        public string MachineName
        {
            get
            {
                string machineName = string.Empty;

                try
                {
                    machineName = System.Configuration.ConfigurationManager.AppSettings["MachineName"];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to read in the configuration file." + Environment.NewLine + ex.ToString());
                }

                if (machineName == null || machineName.Length == 0) machineName = Environment.MachineName;
                return machineName;
            }
        }

        /// <summary>
        /// Gets the source folder. This is the folder that source files move to if the service processing thread raises an exception.
        /// </summary>
        public System.IO.DirectoryInfo SourceErrorsFolder
        {
            get
            {
                if (this.sourceErrorsFolder == null) this.LoadFoldersFromConfig();
                return this.sourceErrorsFolder;
            }
        }

        /// <summary>
        /// Gets the source folder. This is the folder that the service queues source data files into.
        /// </summary>
        public System.IO.DirectoryInfo SourceInputFolder
        {
            get
            {
                if (this.sourceInputFolder == null) this.LoadFoldersFromConfig();
                return this.sourceInputFolder;
            }
        }

        /// <summary>
        /// Gets the source folder. When processed, the source data files move to this folder. The common data files are created in the this.commonInputFolder.
        /// </summary>
        public System.IO.DirectoryInfo SourceOutputFolder
        {
            get
            {
                if (this.sourceOutputFolder == null) this.LoadFoldersFromConfig();
                return this.sourceOutputFolder;
            }
        }

        #endregion
        
        #region Methods
        
        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="message">String to write to the event log.</param>
        public void Log(string message)
        {
            this.Log(EventLogEntryType.Information, 0, message);
        }

        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="message">String to write to the event log.</param>
        /// <param name="args">Array containing zero or more strings to format (String.Format).</param>
        public void Log(string message, params string[] args)
        {
            message = string.Format(message, args);
            this.Log(EventLogEntryType.Information, 0, message);
        }

        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="type">Type of entry (Error, FailureAudit, Information, SuccessAudit, Warning).</param>
        /// <param name="message">String to write to the event log.</param>
        public void Log(EventLogEntryType type, string message)
        {
            this.Log(type, 0, message);
        }

        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="type">Type of entry (Error, FailureAudit, Information, SuccessAudit, Warning).</param>
        /// <param name="message">String to write to the event log.</param>
        /// <param name="args">Array containing zero or more strings to format (String.Format).</param>
        public void Log(EventLogEntryType type, string message, params string[] args)
        {
            message = string.Format(message, args);
            this.Log(type, 0, message);
        }

        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="type">Type of entry (Error, FailureAudit, Information, SuccessAudit, Warning).</param>
        /// <param name="eventID">Unique event identifier.</param>
        /// <param name="message">String to write to the event log.</param>
        public void Log(EventLogEntryType type, int eventID, string message)
        {
            // The source is the name of the service, unless the name is blank
            string source = this.serviceName;
            if (source.Length == 0)
                source = "EventSystem";

            // Create the source in in the event registry if needed
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, "Application");

            // Log it
            EventLog.WriteEntry(source, message, type, eventID);
        }

        /// <summary>
        /// Write an entry to the event log with a given event type, event ID, and given message text. Defaults to the type 'Information' and event id '0' if not specified.
        /// </summary>
        /// <param name="type">Type of entry (Error, FailureAudit, Information, SuccessAudit, Warning).</param>
        /// <param name="eventID">Unique event identifier.</param>
        /// <param name="message">String to write to the event log.</param>
        /// <param name="args">Array containing zero or more strings to format (String.Format).</param>
        public void Log(EventLogEntryType type, int eventID, string message, params string[] args)
        {
            message = string.Format(message, args);
            this.Log(type, eventID, message);
        }

        /// <summary>
        /// Internal routine for validating a folder path. Updates the path with the exact folder name (if it exists).
        /// </summary>
        /// <param name="path">Folder path to validate (e.g., E:\Queue\Capture).</param>
        /// <param name="description">Description of the use for the folder (e.g., CaptureQueue).</param>
        /// <param name="createIfMissing">Set to True prompts to recreate the folder.</param>
        /// <returns>True if the folder exists.</returns>
        public bool ValidateFolder(ref string path, string description, bool createIfMissing)
        {
            if (createIfMissing)
            {
                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    this.Log(
                        EventLogEntryType.Warning,
                        11,
                        "Could not create '{0}' for '{1}'. {2}",
                        path,
                        description,
                        ex.ToString());
                }
            }

            return this.ValidateFolder(ref path, description);
        }
        
        /// <summary>
        /// Internal routine for validating a folder path. Updates the path with the exact folder name (if it exists).
        /// </summary>
        /// <param name="path">Folder path to validate (e.g., E:\Queue\Capture).</param>
        /// <param name="description">Description of the use for the folder (e.g., CaptureQueue).</param>
        /// <returns>True if the folder exists.</returns>
        public bool ValidateFolder(ref string path, string description)
        {
            if (path == null || path.Length == 0 || Directory.Exists(path) == false)
            {
                this.Log(
                    EventLogEntryType.Error,
                    12,
                    "{0}: unable to open '{1}'. Please check to ensure that the folder exists and that the service account has read/write Ntfs access.",
                    description,
                    path);
                this.Stop();
                return false;
            }

            // Get the folder (which clears up things like casing and trailing \)
            DirectoryInfo folder = new DirectoryInfo(path);
            path = folder.FullName;

            // Startup message (msg) and checkpoint (chk)
            string msg = "SimWitty startup . . .";
            string chk = string.Empty;

            // Build the path to the checkpoint file
            string filename = folder.FullName + "\\Validate.chk";

            // Test writing to the folder
            try
            {
                StreamWriter output =
                    new StreamWriter(File.Open(filename, FileMode.Create));
                output.Write(msg);
                output.Close();
            }
            catch
            {
                this.Log(
                    EventLogEntryType.Error,
                    12,
                    "{0}: unable to open '{1}'. Please check to ensure that the service account has read/write Ntfs access.",
                    description,
                    path);
                this.Stop();
                return false;
            }

            // Test reading from the folder
            try
            {
                StreamReader input =
                    new StreamReader(File.Open(filename, FileMode.Open));
                chk = input.ReadToEnd();
                input.Close();
            }
            catch
            {
                this.Log(
                    EventLogEntryType.Error,
                    12,
                    "{0}: unable to open '{1}'. Please check to ensure that the service account has read Ntfs access.",
                    description,
                    path);
                this.Stop();
                return false;
            }

            // Remove the checkpoint file
            File.Delete(filename);

            // Test that what went out came in
            if (msg != chk)
            {
                this.Log(
                    EventLogEntryType.Error,
                    12,
                    "{0}: unable to validate '{1}'. An unexpected error occurred when validating the folder.",
                    description,
                    path);
                this.Stop();
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Test Start calls OnStart(..) to simulate service startup. Use this only while debugging from a console app.
        /// </summary>
        /// <param name="args">Service startup arguments.</param>
        public void TestStart(string[] args)
        {
            this.OnStart(args);
        }

        /// <summary>
        /// Test Stop calls OnStop() to simulate service startup. Use this only while debugging from a console app.
        /// </summary>
        public void TestStop()
        {
            this.OnStop();
        }        
        
        /// <summary>
        /// Internal routine to fill the this.serviceName from the Win32_Service name.
        /// </summary>
        private void FillServiceName()
        {
            // Get the name
            // http://www.vistax64.com/net-general/171078-how-retrieve-service-name-runtime.html

            // Get process ID of current running service instance
            string name = string.Empty;
            int pid = Process.GetCurrentProcess().Id;

            // WMI call to get the service by pid and then read in the name
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name FROM Win32_Service WHERE ProcessId = " + pid);
            foreach (ManagementObject queryObj in searcher.Get())
            {
                name = queryObj["Name"].ToString();
                break;
            }

            if (name.Length > 0)
            {
                this.serviceName = name;
            }
            else
            {
                // error handling defaults to the process executible name
                this.serviceName = Process.GetCurrentProcess().ProcessName.ToString();
            }
        }

        /// <summary>
        /// Load the Common and Source folders from the app.config file
        /// </summary>
        private void LoadFoldersFromConfig()
        {
            string csvErrorsFolder = string.Empty; // This folder is used by the SSIS package for errors. Not typically used by the service.
            string csvInputFolder = string.Empty;  // When processed, the resulting csv statistics files will move to this folder.
            string csvOutputFolder = string.Empty; // This folder is used by the SSIS package for processings. Not typically used by the service.

            string srcErrorsFolder = string.Empty; // This is the folder that data files will move to if the service raises an exception.
            string srcInputFolder = string.Empty;  // This is the folder the source data files will be queued into.
            string srcOutputFolder = string.Empty; // When processed, the source data files will move to this folder.

            // Read the folder values from the configuration file
            try
            {
                csvErrorsFolder = System.Configuration.ConfigurationManager.AppSettings["CsvErrorsFolder"];
                csvInputFolder = System.Configuration.ConfigurationManager.AppSettings["CsvInputFolder"];
                csvOutputFolder = System.Configuration.ConfigurationManager.AppSettings["CsvOutputFolder"];

                srcErrorsFolder = System.Configuration.ConfigurationManager.AppSettings["SrcErrorsFolder"];
                srcInputFolder = System.Configuration.ConfigurationManager.AppSettings["SrcInputFolder"];
                srcOutputFolder = System.Configuration.ConfigurationManager.AppSettings["SrcOutputFolder"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read in the configuration file." + Environment.NewLine + ex.ToString());
                return;
            }

            //// Validate the folder exists and that the service has read/write access

            if (this.ValidateFolder(ref csvErrorsFolder, "Common (csv) error folder", false))
                this.commonErrorsFolder = new DirectoryInfo(csvErrorsFolder);

            if (this.ValidateFolder(ref csvInputFolder, "Common (csv) input folder", false))
                this.commonInputFolder = new DirectoryInfo(csvInputFolder);

            if (this.ValidateFolder(ref csvOutputFolder, "Common (csv) output folder", false))
                this.commonOutputFolder = new DirectoryInfo(csvOutputFolder);

            if (this.ValidateFolder(ref srcErrorsFolder, "Source data error folder", false))
                this.sourceErrorsFolder = new DirectoryInfo(srcErrorsFolder);

            if (this.ValidateFolder(ref srcInputFolder, "Source data input folder", false))
                this.sourceInputFolder = new DirectoryInfo(srcInputFolder);

            if (this.ValidateFolder(ref srcOutputFolder, "Source data output folder", false))
                this.sourceOutputFolder = new DirectoryInfo(srcOutputFolder);

            return;
        }

        #endregion
    }
}
