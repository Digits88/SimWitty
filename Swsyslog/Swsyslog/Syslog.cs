using System;
using System.Net;
using System.Text.RegularExpressions;
using PacketDotNet; // reference
using SharpPcap; // reference

namespace SimWitty.Services.Swsyslog
{

    /// <summary>
    /// Encapsulates a single syslog message, as received from a remote host.
    /// </summary>
    public class SyslogMessage
    {
        /// <summary>
        /// Creates a new instance of the SyslogMessage class.
        /// </summary>
        /// <param name="priority">Specifies the encoded PRI field, containing the facility and severity values.</param>
        /// <param name="timestamp">Specifies the timestamp, if present in the packet.</param>
        /// <param name="hostname">Specifies the hostname, if present in the packet.  The hostname can only be present if the timestamp is also present (RFC3164).</param>
        /// <param name="message">Specifies the textual content of the message.</param>
        public SyslogMessage(int? priority, DateTime timestamp, string hostname, string message)
        {
            if (priority.HasValue)
            {
                this.facility = (int)Math.Floor((double)priority.Value / 8);
                this.severity = priority % 8;
            }
            else
            {
                this.facility = null;
                this.severity = null;
            }
            this.timestamp = timestamp;
            this.hostname = hostname;
            this.message = message;
        }

        public SyslogMessage(PacketDotNet.UdpPacket udpSyslogPacket)
        {
            Regex msgRegex = new Regex(@"
(\<(?<PRI>\d{1,3})\>){0,1}
(?<HDR>
  (?<TIMESTAMP>
    (?<MMM>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s
    (?<DD>[ 0-9][0-9])\s
    (?<HH>[0-9]{2})\:(?<MM>[0-9]{2})\:(?<SS>[0-9]{2})
  )\s
  (?<HOSTNAME>
    [^ ]+?
  )\s
){0,1}
(?<MSG>.*)
", RegexOptions.IgnorePatternWhitespace);

            string packet = System.Text.Encoding.ASCII.GetString(udpSyslogPacket.PayloadData);

            Match m = msgRegex.Match(packet);
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            // If the syslog message is invalid or empty, exit
            if (m != null && !string.IsNullOrEmpty(packet))
            {
                System.Diagnostics.EventLog.WriteEntry("SwsyslogService", "No match." + Environment.NewLine + packet, System.Diagnostics.EventLogEntryType.Warning);
                return;
            }
            else
            {
                System.Diagnostics.EventLog.WriteEntry("SwsyslogService", "Match." + Environment.NewLine + packet, System.Diagnostics.EventLogEntryType.Information);
            }

            //parse PRI section into priority
            int pri;
            int? priority = int.TryParse(m.Groups["PRI"].Value, out pri) ? new int?(pri) : null;

            //parse the HEADER section - contains TIMESTAMP and HOSTNAME
            string hostname = null;
            Nullable<DateTime> timestamp = null;
            if (!string.IsNullOrEmpty(m.Groups["HDR"].Value))
            {
                if (!string.IsNullOrEmpty(m.Groups["TIMESTAMP"].Value))
                {
                    try
                    {
                        timestamp = new DateTime(
                          DateTime.Now.Year,
                          MonthNumber(m.Groups["MMM"].Value),
                          int.Parse(m.Groups["DD"].Value),
                          int.Parse(m.Groups["HH"].Value),
                          int.Parse(m.Groups["MM"].Value),
                          int.Parse(m.Groups["SS"].Value)
                          );
                    }
                    catch (ArgumentException)
                    {
                        //ignore invalid timestamps
                    }
                }

                if (!string.IsNullOrEmpty(m.Groups["HOSTNAME"].Value))
                {
                    hostname = m.Groups["HOSTNAME"].Value;
                }
            }

            if (!timestamp.HasValue)
            {
                //add timestamp as per RFC3164
                timestamp = DateTime.Now;
            }
            if (string.IsNullOrEmpty(hostname))
            {
                IPEndPoint ipe = (IPEndPoint)ep;
                IPHostEntry he = Dns.GetHostEntry(ipe.Address);
                if (he != null && !string.IsNullOrEmpty(he.HostName))
                    hostname = he.HostName;
                else
                    hostname = ep.ToString();
            }

            if (priority.HasValue)
            {
                this.facility = (int)Math.Floor((double)priority.Value / 8);
                this.severity = priority % 8;
            }
            else
            {
                this.facility = null;
                this.severity = null;
            }
            this.timestamp = timestamp.Value;
            this.hostname = hostname;
            this.message = m.Groups["MSG"].Value;

        }

        private static int MonthNumber(string monthName)
        {
            switch (monthName.ToLower().Substring(0, 3))
            {
                case "jan": return 1;
                case "feb": return 2;
                case "mar": return 3;
                case "apr": return 4;
                case "may": return 5;
                case "jun": return 6;
                case "jul": return 7;
                case "aug": return 8;
                case "sep": return 9;
                case "oct": return 10;
                case "nov": return 11;
                case "dec": return 12;
                default:
                    throw new Exception("Unrecognised month name: " + monthName);
            }
        }

        private int? facility;
        /// <summary>
        /// Returns an integer specifying the facility.  The following are commonly used:
        ///       0             kernel messages
        ///       1             user-level messages
        ///       2             mail system
        ///       3             system daemons
        ///       4             security/authorization messages (note 1)
        ///       5             messages generated internally by syslogd
        ///       6             line printer subsystem
        ///       7             network news subsystem
        ///       8             UUCP subsystem
        ///       9             clock daemon (note 2)
        ///      10             security/authorization messages (note 1)
        ///      11             FTP daemon
        ///      12             NTP subsystem
        ///      13             log audit (note 1)
        ///      14             log alert (note 1)
        ///      15             clock daemon (note 2)
        ///      16             local use 0  (local0)
        ///      17             local use 1  (local1)
        ///      18             local use 2  (local2)
        ///      19             local use 3  (local3)
        ///      20             local use 4  (local4)
        ///      21             local use 5  (local5)
        ///      22             local use 6  (local6)
        ///      23             local use 7  (local7)
        /// </summary>
        public int? Facility
        {
            get { return facility; }
        }

        private int? severity;
        /// <summary>
        /// Returns an integer number specifying the severity.  The following values are commonly used:
        ///       0       Emergency: system is unusable
        ///       1       Alert: action must be taken immediately
        ///       2       Critical: critical conditions
        ///       3       Error: error conditions
        ///       4       Warning: warning conditions
        ///       5       Notice: normal but significant condition
        ///       6       Informational: informational messages
        ///       7       Debug: debug-level messages
        /// </summary>
        public int? Severity
        {
            get { return severity; }
        }

        private DateTime timestamp;
        /// <summary>
        /// Returns a DateTime specifying the moment at which the event is known to have happened.  As per RFC3164,
        /// if the host does not send this value, it may be added by a relay.
        /// </summary>
        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        private string hostname;
        /// <summary>
        /// Returns the DNS hostname where the message originated, or the IP address if the hostname is unknown.
        /// </summary>
        public string Hostname
        {
            get { return hostname; }
            set { hostname = value; }
        }

        private string message;
        /// <summary>
        /// Returns a string indicating the textual content of the message.
        /// </summary>
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        /// <summary>
        /// Converts the values of this instance to a string in a comma separated value format (.csv)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = String.Format("{0},{1},{2},{3},\"{4}\"",
                this.facility,
                this.severity,
                this.timestamp,
                this.hostname,
                this.message);
            return s;
        }

        /// <summary>
        /// Converts the values of this instance to a string in a properties file format (.properties)
        /// </summary>
        /// <returns></returns>
        public string ToPropertyString()
        {
            string s = String.Format("Facility={0}\nSeverity={1}\nTimestamp={2}\nHostname={3}\nMessage={4}",
                this.facility,
                this.severity,
                this.timestamp,
                this.hostname,
                this.message);
            return s;
        }
    }
        
}
