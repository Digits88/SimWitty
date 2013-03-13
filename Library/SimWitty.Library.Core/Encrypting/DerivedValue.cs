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
            System.Security.Cryptography.SHA256 sha = new System.Security.Cryptography.SHA256Managed();
            string value = string.Empty;
            byte[] buffer = this.GetHash();

            for (uint u = 0; u < rounds; u++)
            {
                byte[] hash = sha.ComputeHash(buffer);
                string hashBase64 = System.Convert.ToBase64String(hash);
                int end = Math.Min(length, hashBase64.Length);
                value = hashBase64.Substring(0, end);
                buffer = System.Text.Encoding.ASCII.GetBytes(value);
            }

            sha.Dispose();
            return value;
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
