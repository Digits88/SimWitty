// <copyright file="DnsPacket.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Protocols
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// UDP Datagram and DNS packet.
    /// </summary>
    public class DnsPacket
    {
        /// <summary>
        /// The text of the DNS query.
        /// </summary>
        private string text;

        /// <summary>
        /// UDP packet header checksum value.
        /// </summary>
        private ushort checksum;

        /// <summary>
        /// UDP destination port number.
        /// </summary>
        private ushort destinationPort;

        /// <summary>
        /// DNS packet flags.
        /// </summary>
        private ushort flags;

        /// <summary>
        /// UDP packet header length value.
        /// </summary>
        private ushort length;

        /// <summary>
        /// UDP source port number.
        /// </summary>
        private ushort sourcePort;

        /// <summary>
        /// DNS total additional Resource Records (RR).
        /// </summary>
        private ushort totalAdditionalRRs;

        /// <summary>
        /// DNS total answer Resource Records (RR).
        /// </summary>
        private ushort totalAnswerRRs;

        /// <summary>
        /// DNS total DNS authority Resource Records (RR).
        /// </summary>
        private ushort totalAuthorityRRs;

        /// <summary>
        /// DNS total questions.
        /// </summary>
        private ushort totalQuestions;

        /// <summary>
        /// DNS packet transaction identifier.
        /// </summary>
        private ushort transactionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsPacket" /> class.
        /// </summary>
        /// <param name="bytes">Datagram bytes from the IP packet containing a UDP/DNS packet.</param>
        public DnsPacket(byte[] bytes)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes, 0, bytes.Length));

            // Pull out the values from the UDP packet
            this.sourcePort = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.destinationPort = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.length = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.checksum = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

            // Pull ou the values from the DNS packet
            this.transactionId = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.flags = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.totalQuestions = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.totalAnswerRRs = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.totalAuthorityRRs = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.totalAdditionalRRs = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            reader.ReadByte();

            // Find the remaining length of the packet
            int count = (int)(reader.BaseStream.Length - reader.BaseStream.Position);

            // Read the rest of the packet
            byte[] rest = reader.ReadBytes(count);
            char[] chars = new char[count];
            count = 0;

            // Convert the rest of the packet to a text string, ending with 00 bytes
            for (int i = 0; i < rest.Length; i++)
            {
                int value = Convert.ToInt32(rest[i]);
                if (value != 0)
                {
                    chars[i] = Convert.ToChar(value);
                }
                else
                {
                    count = i + 1;
                    break;
                }
            }

            this.text = new string(chars, 0, count);
        }

        /// <summary>
        /// Gets the the text of the DNS query.
        /// </summary>
        public string Text
        {
            get { return this.text; }
        }

        /// <summary>
        /// Gets the UDP packet header checksum value.
        /// </summary>
        public ushort Checksum
        {
            get { return this.checksum; }
        }

        /// <summary>
        /// Gets the UDP destination port number.
        /// </summary>
        public ushort DestinationPort
        {
            get { return this.destinationPort; }
        }

        /// <summary>
        /// Gets the DNS packet flags.
        /// </summary>
        public ushort Flags
        {
            get { return this.flags; }
        }

        /// <summary>
        /// Gets the UDP packet header length value.
        /// </summary>
        public ushort Length
        {
            get { return this.length; }
        }

        /// <summary>
        /// Gets the UDP source port number.
        /// </summary>
        public ushort SourcePort
        {
            get { return this.sourcePort; }
        }

        /// <summary>
        /// Gets the DNS total additional Resource Records (RR).
        /// </summary>
        public ushort TotalAdditionalRRs
        {
            get { return this.totalAdditionalRRs; }
        }

        /// <summary>
        /// Gets the DNS total answer Resource Records (RR).
        /// </summary>
        public ushort TotalAnswerRRs
        {
            get { return this.totalAnswerRRs; }
        }

        /// <summary>
        /// Gets the DNS total DNS authority Resource Records (RR).
        /// </summary>
        public ushort TotalAuthorityRRs
        {
            get { return this.totalAuthorityRRs; }
        }

        /// <summary>
        /// Gets the DNS total questions.
        /// </summary>
        public ushort TotalQuestions
        {
            get { return this.totalQuestions; }
        }

        /// <summary>
        /// Gets the DNS packet transaction identifier.
        /// </summary>
        public ushort TransactionId
        {
            get { return this.transactionId; }
        }
    }
}
