// <copyright file="Base16.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encoding
{
    using System;
    using System.Collections;
    using System.Text;
    using SimWitty.Library.Core.Tools;

    /// <summary>
    /// Implements Base16 encoding functions
    /// </summary>
    public static class Base16
    {
        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to encode.</param>
        /// <returns>An array of two character values that represent the bytes in hexadecimal / Base16 format.</returns>
        public static string[] ToBase16Array(byte[] bytes)
        {
            string[] results = new string[bytes.Length];
           
            for (int i = 0; i < bytes.Length; i++)
            {
                ushort charcode = Convert.ToUInt16(bytes[i]);
                string value = string.Format("{0:X}", charcode);
                if (value.Length == 1) value = "0" + value;
                results[i] = value;
            }

            return results;
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="text">The text containing a series of characters to encode. These will be converted to bytes using Unicode.</param>
        /// <returns>An array of two character values that represent the bytes in hexadecimal / Base16 format.</returns>
        public static string[] ToBase16Array(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            return ToBase16Array(bytes);
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="bits">The bit array containing the sequence of bytes to encode.</param>
        /// <returns>A string representation of bytes in hexadecimal / Base16 format.</returns>
        public static string ToBase16String(BitArray bits)
        {
            return ToBase16String(BinaryTools.BitsToBytes(bits));
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="singleByte">The single byte to encode.</param>
        /// <returns>A string representation of bytes in hexadecimal / Base16 format.</returns>
        public static string ToBase16String(byte singleByte)
        {
            return ToBase16String(new byte[] { singleByte });
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to encode.</param>
        /// <returns>A string representation of bytes in hexadecimal / Base16 format.</returns>
        public static string ToBase16String(byte[] bytes)
        {
            return string.Join(string.Empty, ToBase16Array(bytes));
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="singleByte">The single byte to encode.</param>
        /// <param name="separator">If true, the characters (AAFF) are separated by a colon (AA:FF).</param>
        /// <returns>A string representation of bytes in hexadecimal / Base16 format.</returns>
        public static string ToBase16String(byte singleByte, bool separator)
        {
            return ToBase16String(new byte[] { singleByte }, separator);
        }

        /// <summary>
        /// To encoded Base16 string array
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to encode.</param>
        /// <param name="separator">If true, the characters (AAFF) are separated by a colon (AA:FF).</param>
        /// <returns>A string representation of bytes in hexadecimal / Base16 format.</returns>
        public static string ToBase16String(byte[] bytes, bool separator)
        {
            if (separator)
            {
                return string.Join(":", ToBase16Array(bytes));
            }
            else
            {
                return string.Join(string.Empty, ToBase16Array(bytes));
            }
        }

        /// <summary>
        /// Get the bits from a hexadecimal / Base16 encoded string.
        /// </summary>
        /// <param name="base16text">A hexadecimal / Base16 encoded string to decode into a byte array.</param>
        /// <returns>An array of bits from the resulting byte array.</returns>
        public static BitArray GetBits(string base16text)
        {
            return new BitArray(GetBytes(base16text));
        }

        /// <summary>
        /// Get the bytes from a hexadecimal / Base16 encoded string.
        /// </summary>
        /// <param name="base16text">A hexadecimal / Base16 encoded string to decode into a byte array.</param>
        /// <returns>An array of bytes decoded from the hexadecimal / Base16 encoded string.</returns>
        public static byte[] GetBytes(string base16text)
        {
            char[] base16array = base16text.ToCharArray();

            int encodedLength = base16text.Length / 2;
            
            string[] hexvalues = new string[encodedLength];
            byte[] results = new byte[encodedLength];
            
            if (base16text.IndexOf(':') != -1)
            {
                hexvalues = base16text.Split(':');
            }
            else
            {
                for (int i = 0; i < base16text.Length; i += 2)
                {
                    hexvalues[i / 2] = string.Concat(
                        base16array[i],
                        base16array[i + 1]);
                }
            }
            
            for (int i = 0; i < results.Length; i++)
            {
                ushort charcode = Convert.ToUInt16(hexvalues[i], 16);
                results[i] = Convert.ToByte(charcode);
            }

            return results;
        }

        /// <summary>
        /// Get the Unicode string by decoding a hexadecimal / Base16 encoded string.
        /// </summary>
        /// <param name="base16text">A hexadecimal / Base16 encoded string to decode into a byte array.</param>
        /// <returns>A Unicode string decoded from the byte array, decoded from a hexadecimal / Base16 encoded string.</returns>
        public static string GetString(string base16text)
        {
            byte[] bytes = GetBytes(base16text);
            return Encoding.Unicode.GetString(bytes);
        }
    }
}
