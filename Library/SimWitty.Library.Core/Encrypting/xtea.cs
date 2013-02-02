// <copyright file="Xtea.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extended (X) Tiny Encryption Algorithm (TEA)
    /// </summary>
    public static class Xtea
    {
        /// <summary>
        /// Decrypt encrypted bytes into clear text bytes using a unsigned integer key. 
        /// </summary>
        /// <param name="ciphertext">An array of encrypted bytes.</param>
        /// <param name="key">An unsigned integer containing the 128-bit encryption key.</param>
        /// <returns>Returns a decrypted byte array.</returns>
        public static byte[] Decrypt(byte[] ciphertext, byte[] key)
        {
            // Create an array to hold the decrypted bytes
            byte[] plaintext = new byte[ciphertext.Length];

            // Loop thru the encrypted bytes, decrypting 8 at a time (2 uint, 64 bit)
            for (int j = 0; j < ciphertext.Length; j += 8)
            {
                // Get the next two encrypted uints (v)
                // Get the next two unencrypted uints (u)
                uint[] v = new uint[] { BitConverter.ToUInt32(ciphertext, j), BitConverter.ToUInt32(ciphertext, j + 4) };
                uint[] u = DecryptBlock(0x40, v, key);

                // Copy the unencrypted uints into the plaintext byte array
                Array.Copy(BitConverter.GetBytes(u[0]), 0, plaintext, j, 4);
                Array.Copy(BitConverter.GetBytes(u[1]), 0, plaintext, j + 4, 4);
            }

            return plaintext;
        }

        /// <summary>
        /// Encrypt clear text bytes into encrypted bytes using a unsigned integer key. 
        /// </summary>
        /// <param name="plaintext">An array of clear text bytes.</param>
        /// <param name="key">An unsigned integer containing the 128-bit encryption key.</param>
        /// <returns>Returns a encrypted byte array.</returns>
        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            byte num = (byte)(8 - (plaintext.Length % 8));
            byte[] destinationArray = new byte[plaintext.Length + num];
            Array.Copy(plaintext, destinationArray, plaintext.Length);

            for (int i = plaintext.Length; i < destinationArray.Length; i++)
            {
                destinationArray[i] = num;
            }

            byte[] buffer2 = new byte[destinationArray.Length];

            for (int j = 0; j < destinationArray.Length; j += 8)
            {
                uint[] v = new uint[] { BitConverter.ToUInt32(destinationArray, j), BitConverter.ToUInt32(destinationArray, j + 4) };
                uint[] numArray2 = EncryptBlock(0x40, v, key);
                Array.Copy(BitConverter.GetBytes(numArray2[0]), 0, buffer2, j, 4);
                Array.Copy(BitConverter.GetBytes(numArray2[1]), 0, buffer2, j + 4, 4);
            }

            return buffer2;
        }

        /// <summary>
        /// XTEA is a block cypher and Process Block performs encryption block-by-block.
        /// </summary>
        /// <param name="numberOfRounds">The number of rounds for encryption.</param>
        /// <param name="vector">The initialization vector for encryption.</param>
        /// <param name="key">An unsigned integer containing the 128-bit encryption key.</param>
        /// <returns>An encrypted block in a unsigned integer.</returns>
        private static uint[] EncryptBlock(uint numberOfRounds, uint[] vector, byte[] key)
        {
            // Convert the key to an unsigned integer 
            uint[] cryptoKey = BytesToUint32(key);

            // Boundary check
            if (cryptoKey.Length != 4)
            {
                throw new ArgumentException();
            }

            if (vector.Length != 2)
            {
                throw new ArgumentException();
            }

            uint num2 = vector[0];
            uint num3 = vector[1];
            uint num4 = 0;
            uint num5 = 0x9e3779b9;

            for (uint i = 0; i < numberOfRounds; i++)
            {
                uint num6 = num3 << 4;
                uint num7 = num3 >> 5;
                uint num8 = (num6 ^ num7) + num3;
                uint num9 = num4 + cryptoKey[(int)((IntPtr)(num4 & 3))];
                num2 += num8 ^ num9;
                num4 += num5;
                uint num10 = num2 << 4;
                uint num11 = num2 >> 5;
                uint num12 = (num10 ^ num11) + num2;
                uint num13 = num4 + cryptoKey[(int)((IntPtr)((num4 >> 11) & 3))];
                num3 += num12 ^ num13;
            }

            vector[0] = num2;
            vector[1] = num3;
            return vector;
        }

        /// <summary>
        /// XTEA is a block cypher and Decrypt Block performs decryption block-by-block.
        /// </summary>
        /// <param name="numberOfRounds">The number of rounds for encryption.</param>
        /// <param name="vector">The initialization vector for encryption.</param>
        /// <param name="key">An unsigned integer containing the 128-bit encryption key.</param>
        /// <returns>An decrypted block in a unsigned integer.</returns>
        private static uint[] DecryptBlock(uint numberOfRounds, uint[] vector, byte[] key)
        {
            // Convert the key to an unsigned integer 
            uint[] cryptoKey = BytesToUint32(key);

            // Boundary check
            if (cryptoKey.Length != 4)
            {
                throw new ArgumentException();
            }

            if (vector.Length != 2)
            {
                // Two uint = eight Byte = 64 bit
                throw new ArgumentException();
            }

            uint[] decrypted = new uint[vector.Length];
            uint num2 = vector[0];
            uint num3 = vector[1];
            uint num4 = 0;
            uint num5 = 0x9e3779b9;

            for (uint i = 0; i < numberOfRounds; i++)
            {
                num4 += num5;
            }

            for (uint i = 0; i < numberOfRounds; i++)
            {
                uint num9 = num4 + cryptoKey[(uint)(num4 >> 11 & 3)];
                uint num8 = (num2 << 4 ^ num2 >> 5) + num2;
                num3 -= num8 ^ num9;
                num4 -= num5;
                uint num7 = num4 + cryptoKey[(uint)(num4 & 3)];
                uint num6 = (num3 << 4 ^ num3 >> 5) + num3;
                num2 -= num6 ^ num7;
            }

            decrypted[0] = num2;
            decrypted[1] = num3;

            return decrypted;
        }
        
        /// <summary>
        /// Convert a byte array to a unsigned integer.
        /// The function will convert every four bytes to a 32-bit unsigned integer. Any bytes not divisible by four will be ignored.
        /// </summary>
        /// <param name="bytes">A byte array equally divisible by four.</param>
        /// <returns>An array of unsigned integers.</returns>
        private static uint[] BytesToUint32(byte[] bytes)
        {
            uint[] result = new uint[bytes.Length / 4];

            for (int x = 0; x < result.Length; x++)
            {
                result[x] = BitConverter.ToUInt32(bytes, x * 4);
            }

            return result;
        }
    }
}
