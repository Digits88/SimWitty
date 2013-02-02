// <copyright file="UDPPacket.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Protocols
{
    using System.IO;
    using System.Net;

    /// <summary>
    /// UDP Datagram and DNS packet.
    /// </summary>
    public class UDPPacket
    {
        /// <summary>
        /// UDP packet header checksum value.
        /// </summary>
        private ushort checksum;

        /// <summary>
        /// UDP destination port number.
        /// </summary>
        private ushort destinationPort;

        /// <summary>
        /// UDP packet header length value.
        /// </summary>
        private ushort length;

        /// <summary>
        /// UDP source port number.
        /// </summary>
        private ushort sourcePort;

        /// <summary>
        /// Initializes a new instance of the <see cref="UDPPacket" /> class.
        /// </summary>
        /// <param name="bytes">Datagram bytes from the IP packet containing a UDP/DNS packet.</param>
        public UDPPacket(byte[] bytes)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes, 0, bytes.Length));

            // Pull out the values from the UDP packet
            this.sourcePort = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.destinationPort = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.length = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
            this.checksum = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
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
    }
}
