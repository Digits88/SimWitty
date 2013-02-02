// <copyright file="Passgenerator.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// The class generates alphanumeric passphrases for encryption and other functions. 
    /// </summary>
    public static class Passgenerator
    {
        /// <summary>
        /// Upper case alphabet, lower case alphabet, and numbers; skip ambiguous characters such as I, l, 1, O, 0.
        /// </summary>
        private static string availableCharacters = "ABCDEFGHJKLMNPQRSTWXYZabcdefgijkmnopqrstwxyz23456789";

        /// <summary>
        /// Generate a passphrase composed of random uppercase and lowercase letters and numbers.
        /// </summary>
        /// <param name="passLength">Number of password characters (1-1024)</param>
        /// <returns>Random passphrase string</returns>
        public static string GetString(int passLength)
        {
            // Boundary check
            if (passLength < 1) passLength = 1;
            if (passLength > 1024) passLength = 1024;

            // Get a character list
            char[] characters = availableCharacters.ToCharArray();

            // Get a new random generator
            CryptRandom rand = new CryptRandom();
            
            // Generate a password
            char[] pass = new char[passLength];

            for (int i = 0; i < passLength; i++)
            {
                int n = rand.Next(0, characters.Length - 1);
                pass[i] = characters[n];
            }

            return new string(pass);
        }

        /// <summary>
        /// Generate a passphrase composed of random uppercase and lowercase letters and numbers.
        /// </summary>
        /// <param name="passLength">Number of password bytes (1-1024)</param>
        /// <returns>Random passphrase byte array</returns>
        public static byte[] GetBytes(int passLength)
        {
            string pass = GetString(passLength);
            return System.Text.Encoding.UTF8.GetBytes(pass);
        }
    }
}
