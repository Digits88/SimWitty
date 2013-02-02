// <copyright file="Base32.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encoding
{
    using System;
    using System.Collections;
    using System.Text;
    using SimWitty.Library.Core.Tools;

    /// <summary>
    /// Implements Base32 encoding functions
    /// </summary>
    public static class Base32
    {
        // RFC 4648        Base-N Encodings        October 2006
        // http://tools.ietf.org/html/rfc4648#section-6
        
        /// <summary>
        /// Base32 character encoding table
        /// </summary>
        private static char[] encodingTable = new char[32] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '2', '3', '4', '5', '6', '7' };
        
        /// <summary>
        /// To encoded Base32 character array
        /// </summary>
        /// <param name="bits">The bit array containing the sequence of bytes to encode.</param>
        /// <returns>An array of characters that represent bytes in Base32 format.</returns>
        public static char[] ToBase32CharArray(BitArray bits)
        {
            /*
             * Length = 8 bits in a byte
             * Base32 = 5 bits in a character
             * Character length = bits / 5, rounded up to the nearest whole number
             * Bits length = character length * 5 bits in a character
             * Delta is the difference between the bits length for encoding and the actual bits length
             */

            int bitsLength = bits.Length;
            int bitsIn32 = 5;
            int base32length = (bitsLength + bitsIn32 - 1) / bitsIn32;
            int encodeLength = base32length * 5;
            int delta = encodeLength - bitsLength;

            // Use the length to create the resulting char array
            char[] result = new char[base32length];
            
            // Extend the length of the bits array
            bits.Length = encodeLength;

            for (int i = 0; i < bits.Length; i += bitsIn32)
            {
                ushort charcode = BinaryTools.BitsToNumber(
                    bits.Get(i),
                    bits.Get(i + 1),
                    bits.Get(i + 2),
                    bits.Get(i + 3),
                    bits.Get(i + 4),
                    false,
                    false,
                    false);

                result[(i / 5)] = encodingTable[charcode];
            }

            return result;
        }

        /// <summary>
        /// To encoded Base32 character array
        /// </summary>
        /// <param name="singleByte">The single byte to encode.</param>
        /// <returns>An array of characters that represent bytes in Base32 format.</returns>
        public static char[] ToBase32CharArray(byte singleByte)
        {
            return ToBase32CharArray(new byte[] { singleByte });
        }

        /// <summary>
        /// To encoded Base32 character array
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to encode.</param>
        /// <returns>An array of characters that represent bytes in Base32 format.</returns>
        public static char[] ToBase32CharArray(byte[] bytes)
        {
            BitArray bits = new BitArray(bytes);
            return ToBase32CharArray(bits);
        }

        /// <summary>
        /// To encoded Base32 character array
        /// </summary>
        /// <param name="text">The text containing a series of characters to encode. These will be converted to bytes using Unicode</param>
        /// <returns>An array of characters that represent bytes in Base32 format.</returns>
        public static char[] ToBase32CharArray(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            return ToBase32CharArray(bytes);
        }

        /// <summary>
        /// To encoded Base32 string array
        /// </summary>
        /// <param name="bits">The bit array containing the sequence of bytes to encode.</param>
        /// <returns>A string representation of bytes in Base32 format.</returns>
        public static string ToBase32String(BitArray bits)
        {
            return new string(ToBase32CharArray(bits));
        }

        /// <summary>
        /// To encoded Base32 string array
        /// </summary>
        /// <param name="singleByte">The single byte to encode.</param>
        /// <returns>A string representation of bytes in Base32 format.</returns>
        public static string ToBase32String(byte singleByte)
        {
            return ToBase32String(new byte[] { singleByte });
        }

        /// <summary>
        /// To encoded Base32 string array
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to encode.</param>
        /// <returns>A string representation of bytes in Base32 format.</returns>
        public static string ToBase32String(byte[] bytes)
        {
            return new string(ToBase32CharArray(bytes));
        }

        /// <summary>
        /// Get the bits from a Base32 encoded string.
        /// </summary>
        /// <param name="base32text">A Base32 encoded string to decode into a byte array.</param>
        /// <returns>An array of bits from the resulting byte array.</returns>
        public static BitArray GetBits(string base32text)
        {
            char[] input = base32text.ToLower().ToCharArray();

            /*
             * Base32 = 5 bits in a character
             * Length = number of characters * 5 bits per character
             */ 

            int bitsIn32 = 5;
            int resultsLength = input.Length * bitsIn32;

            // Use the length to create the resulting bits array
            BitArray results = new BitArray(resultsLength, false);

            int index = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                int value = Array.IndexOf(encodingTable, c);
                if (value == -1) throw new ApplicationException("The Base32 encoded string contains invalid characters.");

                BitArray bits = new BitArray(new byte[] { Convert.ToByte(value) });

                for (int x = 0; x < bitsIn32; x++)
                {
                    results.Set(index, bits.Get(x));
                    index++;
                }
            }

            return results;
        }

        /// <summary>
        /// Get the bytes from a Base32 encoded string.
        /// </summary>
        /// <param name="base32text">A Base32 encoded string to decode into a byte array.</param>
        /// <returns>An array of bytes decoded from the hexadecimal / Base16 encoded string.</returns>
        public static byte[] GetBytes(string base32text)
        {
            return BinaryTools.BitsToBytes(GetBits(base32text));
        }

        /// <summary>
        /// Get the Unicode string by decoding a Base32 encoded string.
        /// </summary>
        /// <param name="base32text">A Base32 encoded string to decode into a byte array.</param>
        /// <returns>A Unicode string decoded from the byte array, decoded from a Base32 encoded string.</returns>
        public static string GetString(string base32text)
        {
            byte[] bytes = GetBytes(base32text);
            return Encoding.Unicode.GetString(bytes);
        }

        /// <summary>
        /// Validate a Base32 text using the TryParse pattern.
        /// </summary>
        /// <param name="text">A string that may (or may not) be Base32 encoded.</param>
        /// <param name="base32text">If successful, a Base32 encoded string ready to decode into a byte array.</param>
        /// <returns>Returns true if the Base32 string is valid.</returns>
        public static bool TryParse(string text, out string base32text)
        {
            // Set the default value
            base32text = string.Empty;
            char[] input = text.ToLower().ToCharArray();
            bool result = true;

            // Check all the characters
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                int value = Array.IndexOf(encodingTable, c);
                if (!(value >= 0 && value < encodingTable.Length))
                {
                    // Invalid Base32 string because the character is not in the encoding table
                    result = false;
                    break;                
                }
            }

            // If the result is still true, all characters are valid, so copy in the value
            if (result)
            {
                base32text = text;
            }

            return result;
        }
    }
}
