// <copyright file="IPv4Packet.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Protocols
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Protocol datagram type (TCP, UDP, ICMP, et cetera)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        ICMP = 1,
        Unknown = -1
    }

    /// <summary>
    /// IP Version 4 Packet
    /// </summary>
    public class IPv4Packet
    {
        /// <summary>
        /// Version and header length == 8 bits
        /// </summary>
        private byte versionAndHeaderLength;

        /// <summary>
        /// Version == 4 bits
        /// </summary>
        private ushort version;

        /// <summary>
        /// Header length == 4 bits
        /// </summary>
        private ushort headerLength;

        /// <summary>
        /// Differentiated services (TOS) == 8 bits
        /// </summary>
        private byte differentiatedServices;

        /// <summary>
        /// Total length of the datagram == 16 bits
        /// </summary>
        private ushort totalLength;

        /// <summary>
        /// Packet identification number == 16 bits
        /// </summary>
        private ushort identification;

        /// <summary>
        /// Flags and fragmentation offset == 8 bits
        /// </summary>
        private ushort flagsAndOffset;

        /// <summary>
        /// Time to Live (TTL) == 8 bits
        /// </summary>
        private byte timeToLive;

        /// <summary>
        /// Datagram protocol identifier == 8 bits
        /// </summary>
        private byte datagramProtocol;

        /// <summary>
        /// Packet checksum == 16 bits (short because it is a signed value)
        /// </summary>
        private short checksum;

        /// <summary>
        /// Source IP address
        /// </summary>
        private uint sourceAddress;

        /// <summary>
        /// Destination IP address
        /// </summary>
        private uint destinationAddress;

        /// <summary>
        /// Packet bytes
        /// </summary>
        private byte[] packet;

        /// <summary>
        /// Datagram bytes
        /// </summary>
        private byte[] datagram;

        /// <summary>
        /// Valid if we parsed the header correctly
        /// </summary>
        private bool validPacket = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4Packet" /> class.
        /// </summary>
        /// <param name="bytes">Byte array containing the packet information</param>
        /// <param name="received">A count of the bytes in the packet</param>
        public IPv4Packet(byte[] bytes, int received)
        {
            this.packet = bytes;

            try
            {
                BinaryReader reader = new BinaryReader(new MemoryStream(bytes, 0, received));

                /*
                 * 8 bits for version and header length
                 * 8 bits for differentiated services (TOS)
                 * 16 bits for total length of the datagram (header + message)
                 * 16 bits for identification
                 * 8 bits for flags and fragmentation offset
                 * 8 bits for TTL (Time To Live)
                 * 8 bits for the underlying protocol
                 * 16 bits containing the checksum of the header
                 * Checksum value (can be negative)
                 * 32 bit source IP Address
                 * 32 destination IP Address
                 */ 

                this.versionAndHeaderLength = reader.ReadByte();
                this.differentiatedServices = reader.ReadByte();
                this.totalLength = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                this.identification = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                this.flagsAndOffset = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                this.timeToLive = reader.ReadByte();
                this.datagramProtocol = reader.ReadByte();
                this.checksum = IPAddress.NetworkToHostOrder(reader.ReadInt16());
                this.sourceAddress = (uint)reader.ReadInt32();
                this.destinationAddress = (uint)reader.ReadInt32();

                // Bit shift right, leaving four leading zeros, and dropping the length
                this.version = Convert.ToUInt16(this.versionAndHeaderLength >> 4);

                /*
                 * Bit shift left, leaving four trailing zeros, and dropping the version
                 * Bit shift right, leaving four leading zeros
                 * Convert to ushort to get the number of 32-bit words in the header
                 * Convert to the number of bytes (5 * 32 = 160 bits / 8 bits in a byte = 20 bytes)
                 */ 

                byte tmp = (byte)(this.versionAndHeaderLength << 4);
                tmp = (byte)(tmp >> 4);
                this.headerLength = Convert.ToUInt16(tmp);
                this.headerLength = Convert.ToUInt16((this.headerLength * 32) / 8);

                if (this.Version == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    this.validPacket = true;
                }
                else
                {
                    this.validPacket = false;
                }
            }
            catch
            {
                this.validPacket = false;
            }
        }

        /// <summary>
        /// Gets the checksum (hexadecimal)
        /// </summary>
        public string Checksum
        {
            get
            {
                return string.Format("0x{0:x2}", this.checksum);
            }
        }

        /// <summary>
        /// Gets the packet bytes
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (this.datagram == null)
                {
                    this.datagram = new byte[this.MessageLength];
                    Array.Copy(this.packet, this.HeaderLength, this.datagram, 0, this.MessageLength);
                }

                return this.datagram;
            }
        }

        /// <summary>
        /// Gets the differentiated services (hexadecimal)
        /// </summary>
        public string DifferentiatedServices
        {
            get
            {
                return string.Format("0x{0:x2}", this.differentiatedServices);
            }
        }

        /// <summary>
        /// Gets the destination IP address
        /// </summary>
        public IPAddress DestinationAddress
        {
            get
            {
                return new IPAddress(this.destinationAddress);
            }
        }

        /// <summary>
        /// Gets the flags and offset
        /// </summary>
        public ushort Flags
        {
            get
            {
                return this.flagsAndOffset;
            }
        }

        /// <summary>
        /// Gets the number of bytes in the header
        /// </summary>
        public ushort HeaderLength
        {
            get
            {
                return this.headerLength;
            }
        }

        /// <summary>
        /// Gets the packet identifier
        /// </summary>
        public ushort Identification
        {
            get
            {
                return this.identification;
            }
        }

        /// <summary>
        /// Gets the number of bytes in the message
        /// </summary>
        public ushort MessageLength
        {
            get
            {
                return Convert.ToUInt16(this.Length - this.HeaderLength);
            }
        }

        /// <summary>
        /// Gets the datagram protocol identifier
        /// </summary>
        public ushort ProtocolId
        {
            get
            {
                return Convert.ToUInt16(this.datagramProtocol);
            }
        }

        /// <summary>
        /// Gets the datagram protocol type
        /// </summary>
        public Protocol ProtocolType
        {
            get
            {
                switch (this.datagramProtocol)
                {
                    case 1:
                        return Protocol.ICMP;
                    case 6:
                        return Protocol.TCP;
                    case 17:
                        return Protocol.UDP;
                    default:
                        return Protocol.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets the source IP address
        /// </summary>
        public IPAddress SourceAddress
        {
            get
            {
                return new IPAddress(this.sourceAddress);
            }
        }

        /// <summary>
        /// Gets the total number of bytes in the packet
        /// </summary>
        public int Length
        {
            get
            {
                return this.packet.Length;
            }
        }

        /// <summary>
        /// Gets the Time to Live (TTL) value from the IP header
        /// </summary>
        public ushort TTL
        {
            get
            {
                return Convert.ToUInt16(this.timeToLive);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the packet header was correctly loaded
        /// </summary>
        public bool ValidPacket
        {
            get
            {
                return this.validPacket;
            }
        }

        /// <summary>
        /// Gets the address version
        /// </summary>
        public System.Net.Sockets.AddressFamily Version
        {
            get
            {
                switch (this.version)
                {
                    case 4:
                        return System.Net.Sockets.AddressFamily.InterNetwork;

                    case 6:
                        return System.Net.Sockets.AddressFamily.InterNetworkV6;

                    default:
                        throw new System.ApplicationException("Unrecognized packet type.");
                }
            }
        }
    }
}
