// <copyright file="DerivedValue.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;
    using System.Runtime.InteropServices; // Marshal
    using System.Security; // SecureString, 

    /// <summary>
    /// Obtain a derived value from a pre-shared key using several rounds of hashing.
    /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
    /// </summary>
    public class DerivedValue
    {
        /// <summary>
        /// Internal pre-shared key value.
        /// </summary>
        private SecureString preshared;

        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedValue" /> class.
        /// </summary>
        /// <param name="encryptionKey">The encryption key represented in a byte array. Important note: such a byte array can be read in memory. Dispose of it quickly.</param>
        public DerivedValue(byte[] encryptionKey)
        {
            this.preshared = new SecureString();

            for (int i = 0; i < encryptionKey.Length; i++)
            {
                byte b = encryptionKey[i];
                char c = Convert.ToChar(b);
                this.preshared.AppendChar(c);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedValue" /> class.
        /// </summary>
        /// <param name="passphrase">The encryption key or passphrase represented in a SecureString. This is the preferred method of instantiating this class because it protects the key in memory.</param>
        public DerivedValue(SecureString passphrase)
        {
            this.preshared = passphrase.Copy();
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <param name="length">The character length of the resulting value.</param>
        /// <returns>A value derived from several rounds of hashing and truncating.</returns>
        public string GetString(uint rounds, int length)
        {
            byte[] value = this.GetBytes(rounds, length);
            string base64 = Convert.ToBase64String(value);

            if (length > base64.Length) length = base64.Length;
            return base64.Substring(0, length);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <param name="length">The character length of the resulting value.</param>
        /// <returns>A value derived from several rounds of hashing and truncating.</returns>
        public byte[] GetBytes(uint rounds, int length)
        {
            System.Security.Cryptography.SHA256 sha = new System.Security.Cryptography.SHA256Managed();
            byte[] buffer = this.GetHash();

            if (length < 1) length = 1;
            if (length > buffer.Length) length = buffer.Length;

            for (uint u = 0; u < rounds; u++)
            {
                byte[] hash = sha.ComputeHash(buffer);
                buffer = new byte[length];
                Array.Copy(hash, buffer, length);
            }

            sha.Dispose();
            return buffer;
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 16-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public ushort GetUInt16()
        {
            return this.GetUInt16(3);
        }
        
        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 16-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public ushort GetUInt16(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 2);
            return BitConverter.ToUInt16(value, 0);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 32-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public uint GetUInt32()
        {
            return this.GetUInt32(3);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 32-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public uint GetUInt32(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 4);
            return BitConverter.ToUInt32(value, 0);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 64-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public ulong GetUInt64()
        {
            return this.GetUInt64(3);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 64-bit unsigned integer value derived from several rounds of hashing and truncating.</returns>
        public ulong GetUInt64(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 8);
            return BitConverter.ToUInt64(value, 0);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 16-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public short GetInt16()
        {
            return this.GetInt16(3);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 16-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public short GetInt16(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 2);
            return BitConverter.ToInt16(value, 0);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 32-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public int GetInt32()
        {
            return this.GetInt32(3);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 32-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public int GetInt32(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 4);
            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <returns>A 64-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public long GetInt64()
        {
            return this.GetInt64(3);
        }   

        /// <summary>
        /// Obtain a derived value from a pre-shared key using several rounds of hashing.
        /// The truncated value has a high rate of collisions. However, with a high collision rate, the ability to reverse the value to a pre-shared key is lowered.
        /// </summary>
        /// <param name="rounds">The number of times to hash before returning the value.</param>
        /// <returns>A 64-bit signed integer value derived from several rounds of hashing and truncating.</returns>
        public long GetInt64(uint rounds)
        {
            byte[] value = this.GetBytes(rounds, 8);
            return BitConverter.ToInt64(value, 0);
        }        

        /// <summary>
        /// Get the hash value of the pre-shared key.
        /// All other members should use this method to reduce the likelihood of data leakage.
        /// </summary>
        /// <returns>A computed hash of the pre-shared key.</returns>
        private byte[] GetHash()
        {
            int length = this.preshared.Length;
            byte[] clearbytes = new byte[length];
            IntPtr pointer = IntPtr.Zero;
            
            // Marshal the preshared key as a byte array
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocAnsi(this.preshared);
                Marshal.Copy(pointer, clearbytes, 0, length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (pointer != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocAnsi(pointer);
            }

            System.Security.Cryptography.SHA256 sha = new System.Security.Cryptography.SHA256Managed();
            byte[] hash = sha.ComputeHash(clearbytes);

            // Nulling the byte marks the byte for garbage collection but does not remove it from memory.
            // Therefore, we zero the clear text byte to reduce the likelihood of data leakage.
            for (int i = 0; i < length; i++)
            {
                clearbytes[i] = 0;
            }

            // Return the hash value
            return hash;
        }
    }
}
