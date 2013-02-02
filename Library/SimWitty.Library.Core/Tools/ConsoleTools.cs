// <copyright file="ConsoleTools.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security;

    /// <summary>
    /// A collection of tools for automating common console inputs and outputs.
    /// </summary>
    public static class ConsoleTools
    {
        /// <summary>
        /// The selected communications mode { Alice (transmit), Bob (receive), Quit (exit the program), Unspecified }
        /// </summary>
        public enum CommunicationMode
        {
            /// <summary>
            /// The Alice mode is for writing or transmitting messages (A -> B).
            /// </summary>
            Alice,

            /// <summary>
            /// The Bob mode is for reading or receiving messages (A -> B).
            /// </summary>
            Bob,

            /// <summary>
            /// Quit indicates that the user has selected to terminate the program.
            /// </summary>
            Quit,

            /// <summary>
            /// The user has not specified the mode.
            /// </summary>
            Unspecified
        }

        /// <summary>
        /// Create a horizontal line for use in Console applications. If the length exceeds the width of the console, the line will be the width of the console.
        /// </summary>
        /// <param name="length">The number of characters in the resulting line.</param>
        /// <returns>Returns a horizontal line.</returns>
        public static string HorizontalLine(int length)
        {
            char line = '-';
            return HorizontalLine(length, line);
        }

        /// <summary>
        /// Create a horizontal line for use in Console applications. If the length exceeds the width of the console, the line will be the width of the console.
        /// </summary>
        /// <param name="length">The number of characters in the resulting line.</param>
        /// <param name="line">The character to use for the resulting line (the default is '-').</param>
        /// <returns>Returns a horizontal line.</returns>
        public static string HorizontalLine(int length, char line)
        {
            length = Math.Min(length, Console.WindowWidth - 1);
            return new string(line, length);
        }

        /// <summary>
        /// Determine which length is the longest in an array, and return a horizontal line that is that length. If the maximum length exceeds the width of the console, the line will be the width of the console.
        /// </summary>
        /// <param name="lengths">An array of possible lengths.</param>
        /// <returns>Returns a horizontal line.</returns>
        public static string HorizontalLine(int[] lengths)
        {
            Array.Sort(lengths);
            int length = lengths[lengths.Length - 1];
            return HorizontalLine(length);
        }

        /// <summary>
        /// Determine which length is the longest in an array, and return a horizontal line that is that length. If the maximum length exceeds the width of the console, the line will be the width of the console.
        /// </summary>
        /// <param name="lengths">An array of possible lengths.</param>
        /// <param name="line">The character to use for the resulting line (the default is '-').</param>
        /// <returns>Returns a horizontal line.</returns>
        public static string HorizontalLine(int[] lengths, char line)
        {
            Array.Sort(lengths);
            int length = lengths[lengths.Length - 1];
            return HorizontalLine(length, line);
        }

        /// <summary>
        /// Write to the Console a common startup message that displays: assembly title, file name, file version, description, product, copyright, and company.
        /// </summary>
        public static void PrintStartup()
        {
            // The following values are available from the assembly and executable
            string name = string.Empty;
            string title = string.Empty;
            string description = string.Empty;
            string company = string.Empty;
            string product = string.Empty;
            string copyright = string.Empty;
            string trademark = string.Empty;
            string version = string.Empty;
            string informationalVersion = string.Empty;
            string filename = string.Empty;
            string fileVersion = string.Empty;

            // Get the current assembly
            Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            name = assembly.FullName;

            // Populate the values from the assembly attributes
            foreach (var attrib in assembly.GetCustomAttributes(false))
            {
                if (attrib is AssemblyTitleAttribute) title = ((AssemblyTitleAttribute)attrib).Title;
                if (attrib is AssemblyDescriptionAttribute) description = ((AssemblyDescriptionAttribute)attrib).Description;
                if (attrib is AssemblyCompanyAttribute) company = ((AssemblyCompanyAttribute)attrib).Company;
                if (attrib is AssemblyProductAttribute) product = ((AssemblyProductAttribute)attrib).Product;
                if (attrib is AssemblyCopyrightAttribute) copyright = ((AssemblyCopyrightAttribute)attrib).Copyright;
                if (attrib is AssemblyTrademarkAttribute) trademark = ((AssemblyTrademarkAttribute)attrib).Trademark;
                if (attrib is AssemblyVersionAttribute) version = ((AssemblyVersionAttribute)attrib).Version;
                if (attrib is AssemblyInformationalVersionAttribute) informationalVersion = ((AssemblyInformationalVersionAttribute)attrib).InformationalVersion;
            }

            // Populate the values from the file system
            filename = Path.GetFileName(assembly.Location);
            System.Diagnostics.FileVersionInfo versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            fileVersion = versionInfo.FileVersion;

            // Output lines
            string line1 = title;
            string line2 = string.Format("{0} v{1}", filename, fileVersion);
            string line3 = description;
            string line4 = product;
            string line5 = copyright;
            string line6 = company;
            string line7 = HorizontalLine(new int[6] { line1.Length, line2.Length, line3.Length, line4.Length, line5.Length, line6.Length });
            
            // Console output
            Console.Clear();
            Console.WriteLine(line1);
            Console.WriteLine(line2);
            Console.WriteLine(line3);
            Console.WriteLine(line4);
            Console.WriteLine(line5);
            Console.WriteLine(line6);
            Console.WriteLine(line7);
        }

        /// <summary>
        /// Try to obtain user input on the communication mode and encryption passphrase.
        /// </summary>
        /// <param name="passphrase">When successful, contains the SecureString passphrase for encryption.</param>
        /// <returns>The return value indicates whether the input was successfully completed.</returns>
        public static bool TryInput(out SecureString passphrase)
        {
            // Assign default values
            passphrase = new SecureString();

            // Input the passphrase
            Console.WriteLine();
            Console.Write("Enter a 16-character or 32-character passphrase: ");

            do
            {
                // Read the next key without displaying it on the console, and exit if Enter is pressed
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter) break;

                // Display a *, append the character to the passphrase, and exit if the passphrase is 32-characters long
                Console.Write("*");
                passphrase.AppendChar(k.KeyChar);
                if (passphrase.Length == 32) break;
            }
            while (true);

            Console.WriteLine();
            Console.WriteLine();
            return true;
        }
        
        /// <summary>
        /// Try to obtain user input on the communication mode and encryption passphrase.
        /// </summary>
        /// <param name="mode">When successful, contains the user's selected communication mode. Otherwise, unspecified.</param>
        /// <param name="passphrase">When successful, contains the SecureString passphrase for encryption.</param>
        /// <returns>The return value indicates whether the input was successfully completed.</returns>
        public static bool TryInput(out CommunicationMode mode, out SecureString passphrase)
        {
            // Assign default values
            mode = CommunicationMode.Unspecified;
            passphrase = new SecureString();

            // Input the communications mode
            do
            {
                Console.Write("Enter A to send messages, B to receive messages, or Q to quit: ");
                ConsoleKeyInfo k = Console.ReadKey(false);
                Console.WriteLine();

                switch (k.Key.ToString().ToUpper())
                {
                    case "A":
                        mode = CommunicationMode.Alice;
                        break;
                    case "B":
                        mode = CommunicationMode.Bob;
                        break;
                    case "Q":
                        mode = CommunicationMode.Quit;
                        break;
                    default:
                        Console.WriteLine("Invalid selection: {0}", k.Key.ToString());
                        break;
                }
            } 
            while (mode == CommunicationMode.Unspecified);

            if (mode == CommunicationMode.Quit)
            {
                return false;
            }

            // Input the passphrase
            Console.WriteLine();
            Console.Write("Enter a 16-character or 32-character passphrase: ");

            do
            {
                // Read the next key without displaying it on the console, and exit if Enter is pressed
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter) break;

                // Display a *, append the character to the passphrase, and exit if the passphrase is 32-characters long
                Console.Write("*");
                passphrase.AppendChar(k.KeyChar);
                if (passphrase.Length == 32) break;
            } 
            while (true);

            Console.WriteLine();
            Console.WriteLine();
            return true;
        }

        /// <summary>
        /// Try to obtain user input on the communication mode and encryption passphrase.
        /// </summary>
        /// <param name="mode">When successful, contains the user's selected communication mode. Otherwise, unspecified.</param>
        /// <param name="passphrase">When successful, contains the SecureString passphrase for encryption.</param>
        /// <param name="file">When successful, contains the path to a file for processing.</param>
        /// <returns>The return value indicates whether the input was successfully completed.</returns>
        public static bool TryInput(out CommunicationMode mode, out SecureString passphrase, out FileInfo file)
        {
            // Assign default values
            file = null;

            // Obtain input values from other TryInput methods
            bool result = TryInput(out mode, out passphrase);
            if (!result) return false;

            // Input the filename
            do
            {
                Console.Write("Enter the file name: ");
                string path = Console.ReadLine();
                if (File.Exists(path))
                {
                    file = new FileInfo(path);
                    break;
                }

                Console.WriteLine("File not found.");
            }
            while (true);

            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// Try to obtain user input on the communication mode and encryption passphrase.
        /// </summary>
        /// <param name="mode">When successful, contains the user's selected communication mode. Otherwise, unspecified.</param>
        /// <param name="passphrase">When successful, contains the SecureString passphrase for encryption.</param>
        /// <param name="remoteAddress">When successful, contains an IP address bound on the remote computer.</param>
        /// <returns>The return value indicates whether the input was successfully completed.</returns>
        public static bool TryInput(out CommunicationMode mode, out SecureString passphrase, out System.Net.IPAddress remoteAddress)
        {
            // Assign default values
            remoteAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // Obtain input values from other TryInput methods
            bool result = TryInput(out mode, out passphrase);
            if (!result) return false;

            // Input the remote IP address
            do
            {
                // Prompt the user
                Console.Write("Please enter the remote IP address: ");
                string line = Console.ReadLine();

                // Assess the input and exit the loop if the address is valid
                result = System.Net.IPAddress.TryParse(line, out remoteAddress);
                if (result) break;

                // Try again
                Console.WriteLine("Invalid IP address entered.");
            }
            while (true);

            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// Try to obtain user input on the communication mode and encryption passphrase.
        /// </summary>
        /// <param name="mode">When successful, contains the user's selected communication mode. Otherwise, unspecified.</param>
        /// <param name="passphrase">When successful, contains the SecureString passphrase for encryption.</param>
        /// <param name="remoteAddress">When successful, contains an IP address bound on the remote computer.</param>
        /// <param name="localAddress">When successful, contains an IP address bound on the local computer.</param>
        /// <returns>The return value indicates whether the input was successfully completed.</returns>
        public static bool TryInput(out CommunicationMode mode, out SecureString passphrase, out System.Net.IPAddress remoteAddress, out System.Net.IPAddress localAddress)
        {
            // Assign default values
            localAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // Obtain input values from other TryInput methods
            bool result = TryInput(out mode, out passphrase, out remoteAddress);
            if (!result) return false;

            // Display available IP addresses on the local computer
            string line = "Available IP addresses:";
            Console.WriteLine(line);
            Console.WriteLine(HorizontalLine(line.Length));

            System.Net.IPAddress[] addressList =
                System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;

            for (int i = 0; i < addressList.Length; i++)
            {
                // Display only the valid IPv4 and IPv6 addresses
                if (addressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || addressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    Console.WriteLine(" {0} = {1}", i.ToString(), addressList[i].ToString());
                }
            }

            // Input the local IP Address
            do
            {
                Console.Write("Please enter the number of the IP address: ");
                string input = Console.ReadLine();
                ushort selection = ushort.MaxValue;
                result = ushort.TryParse(input, out selection);

                if (result && selection < addressList.Length)
                {
                    // Set the local address to the value selected
                    localAddress = addressList[selection];

                    // Verify the local and remote addresses are in the same family
                    if (remoteAddress.AddressFamily == localAddress.AddressFamily)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Both addresses must be in the same IP address family (IPv4, IPv6).");
                    }
                }

                Console.WriteLine("Invalid IP address selected.");
                Console.WriteLine();
            }
            while (true);

            Console.WriteLine();
            return result;
        }
    }
}
