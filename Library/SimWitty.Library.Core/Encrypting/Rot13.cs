// <copyright file="Rot13.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;

    /// <summary>
    /// Implements Rot13 by calling the Caesar Cipher with an alphabet shift of 13 characters.
    /// </summary>
    public static class Rot13
    {
        /// <summary>
        /// Encrypt a value using the Rot13 Caesar Cipher.
        /// </summary>
        /// <param name="value">The text value to encrypt.</param>
        /// <returns>Returns an encrypted string.</returns>
        public static string Encrypt(string value)
        {
            return CaesarCipher.Encrypt(value, 13);
        }

        /// <summary>
        /// Decrypt a value using the Rot13 Caesar Cipher.
        /// </summary>
        /// <param name="value">The text value to decrypt.</param>
        /// <returns>Returns an decrypted string.</returns>
        public static string Decrypt(string value)
        {
            return CaesarCipher.Encrypt(value, -13);
        }
    }
}
