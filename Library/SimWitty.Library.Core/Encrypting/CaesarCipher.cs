// <copyright file="CaesarCipher.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;

    /// <summary>
    /// Caesar Cipher implements the alphabet shift cipher.
    /// </summary>
    public static class CaesarCipher
    {
        /// <summary>
        /// Encrypt a value using the Caesar Cipher.
        /// </summary>
        /// <param name="value">The text value to encrypt.</param>
        /// <param name="shift">The number of characters to shift (e.g., Rot13 is 13 characters).</param>
        /// <returns>Returns an encrypted string.</returns>
        public static string Encrypt(string value, int shift)
        {
            return Caesar(value, shift);
        }

        /// <summary>
        /// Decrypt a value using the Caesar Cipher.
        /// </summary>
        /// <param name="value">The text value to decrypt.</param>
        /// <param name="shift">The number of characters to shift (e.g., Rot13 is 13 characters).</param>
        /// <returns>Returns an decrypted string.</returns>
        public static string Decrypt(string value, int shift)
        {
            return Caesar(value, shift * -1);
        }

        /// <summary>
        /// Perform the Caesar Cipher by shifting upper and lower case alphabetical characters.
        /// </summary>
        /// <param name="value">The text value to encrypt or decrypt.</param>
        /// <param name="shift">The number of characters to shift (e.g., Rot13 is 13 characters).</param>
        /// <returns>Returns a shifted string.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Reviewed. A and Z are upper-case for readability.")]
        private static string Caesar(string value, int shift)
        {
            int a = (int)'a';
            int A = (int)'A';
            int z = (int)'z';
            int Z = (int)'Z';

            char[] buffer = value.ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                int number = Convert.ToInt32(buffer[i]);

                if (number >= a && number <= z)
                {
                    number = Shifter(number, shift, a, z);
                }
                else if (number >= A && number <= Z)
                {
                    number = Shifter(number, shift, A, Z);
                }

                buffer[i] = Convert.ToChar(number);
            }

            return new string(buffer);
        }

        /// <summary>
        /// Shift a number up or down, positive or negative, n-places. 
        /// When exceeding the minimum value, shift back around to the top. 
        /// When exceeding the maximum value, shift back around to the bottom.
        /// </summary>
        /// <param name="value">The value to shift.</param>
        /// <param name="shift">The number of places to shift (positive or negative).</param>
        /// <param name="minimum">The minimum boundary for the shift. </param>
        /// <param name="maximum">The maximum boundary for the shift.</param>
        /// <returns>Returns a integer shifted n-places.</returns>
        private static int Shifter(int value, int shift, int minimum, int maximum)
        {
            if (value < minimum || value > maximum) throw new System.ArgumentOutOfRangeException("value", value, "The value must be between the minimum and maximum specified.");

            int length = maximum - minimum + 1;
            int shifted = value + shift;
            while (shifted < minimum) shifted += length;
            while (shifted > maximum) shifted -= length;

            return shifted;
        }
    }
}
