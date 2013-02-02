// <copyright file="Cryptkeeper.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Encrypting
{
    using System;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using SimWitty.Library.Core.Encoding;

    /// <summary>
    /// Crypt Keeper is a class for simplifying the AES encryption and decryption of bytes and strings.
    /// </summary>
    public class Cryptkeeper
    {
        /// <summary>
        /// The AES key is stored in a SecureString to prevent its disclosure in memory.
        /// </summary>
        private SecureString keyString;

        /// <summary>
        /// The AES encryption bit size (128-bit or 256-bit).
        /// </summary>
        private int keysize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cryptkeeper" /> class.
        /// </summary>
        /// <param name="encryptionKey">The encryption key represented in a byte array. Important note: such a byte array can be read in memory. Dispose of it quickly.</param>
        public Cryptkeeper(byte[] encryptionKey)
        {
            string err = "The Cryptkeeper key must be 16 bytes (128-bit encryption) or 32 bytes (256-bit) in length.";
            int length = 0;

            if (encryptionKey == null) throw new ApplicationException(err);
            if (encryptionKey.Length == 0) throw new ApplicationException(err);

            if (encryptionKey.Length > 1 && encryptionKey.Length <= 16)
            {
                this.keysize = 128; // 128 bits is 16 bytes 
                length = 16;
            }

            if (encryptionKey.Length > 16 && encryptionKey.Length <= 32)
            {
                this.keysize = 256; // 256 bits is 32 bytes
                length = 32;
            }

            if (encryptionKey.Length > 32) throw new ApplicationException(err);

            this.keyString = new SecureString();

            for (int i = 0; i < length; i++)
            {
                byte b = 0xFF;
                if (i < encryptionKey.Length) b = encryptionKey[i];
                char c = Convert.ToChar(b);
                this.keyString.AppendChar(c);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cryptkeeper" /> class.
        /// </summary>
        /// <param name="passphrase">The encryption key or passphrase represented in a SecureString. This is the preferred method of instantiating this class because it protects the key in memory.</param>
        public Cryptkeeper(System.Security.SecureString passphrase)
        {
            string err = "Cryptkeeper passphrase must be 16 characters (128-bit encryption) or 32 characters (256-bit) in length.";

            if (passphrase == null) throw new ApplicationException(err);
            if (passphrase.Length == 0) throw new ApplicationException(err);

            if (passphrase.Length > 1 && passphrase.Length <= 16)
            {
                this.keysize = 128; // 128 bits is 16 bytes or 16 characters in UTF8
                this.keyString = passphrase.Copy();
                return;
            }

            if (passphrase.Length > 16 && passphrase.Length <= 32)
            {
                this.keysize = 256; // 256 bits is 32 bytes or 32 characters in UTF8
                this.keyString = passphrase.Copy();
                return;
            }

            if (passphrase.Length > 32) throw new ApplicationException(err);
        }
        
        /// <summary>
        /// The actions the class should take { Encrypt, Decrypt }.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
        public enum Action
        {
            Encrypt,
            Decrypt
        }

        /// <summary>
        /// The forms of encoding to use { None, Base16, Base32, Base64 }.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
        public enum EncodingMethod
        {
            None,
            Base16,
            Base32,
            Base64
        }

        /// <summary>
        /// Gets the standard settings for the AES object.
        /// </summary>
        /// <returns>Returns the defacto AES object with the Cryptkeeper settings.</returns>
        public AesManaged Crypto
        {
            get
            {
                AesManaged result = new AesManaged();
                result.KeySize = this.keysize;
                result.Key = this.Key;
                result.BlockSize = 128;
                result.Mode = CipherMode.ECB; // .CBC will lead to the first 16 characters being malformed
                result.Padding = PaddingMode.PKCS7; // PKCS #7 for text characters
                return result;
            }
        }

        /// <summary>
        /// Gets the encryption key in an unencrypted byte array. Important note: this key can be read in memory using this value. Store the resulting value briefly and dispose of it quickly.
        /// </summary>
        public byte[] Key
        {
            get
            {
                int length = this.keysize / 8;
                byte[] result = new byte[length];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = 0xFF;
                }

                IntPtr pointer = IntPtr.Zero;
                string cleartext = string.Empty;

                try
                {
                    pointer = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(this.keyString);
                    cleartext = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(pointer);
                }
                catch (Exception ex)
                {
                    cleartext = ex.ToString();
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(pointer);
                    }
                }

                char[] values = cleartext.ToCharArray();
                cleartext = string.Empty;

                for (int i = 0; i < result.Length; i++)
                {
                    if (i < values.Length)
                    {
                        result[i] = Convert.ToByte(values[i]);
                        values[i] = ' ';
                    }
                }

                return result;
            }
        }
        
        /// <summary>
        /// Decrypt encrypted bytes using AES with the specified encryption key.
        /// </summary>
        /// <param name="cipherbytes">An array of encrypted bytes to decrypt.</param>
        /// <returns>The resulting clear text string.</returns>
        public string Decrypt(byte[] cipherbytes)
        {
            AesManaged aes = this.Crypto;
            ICryptoTransform transform = aes.CreateDecryptor();
            
            byte[] resultArray = transform.TransformFinalBlock(cipherbytes, 0, cipherbytes.Length);
            string cleartext = Encoding.Unicode.GetString(resultArray);
            return cleartext.Trim();
        }

        /// <summary>
        /// Decrypt cipher text using AES with the specified encryption key.
        /// </summary>
        /// <param name="ciphertext">The cipher text, Base64 encoded, to decrypt.</param>
        /// <returns>The resulting clear text string.</returns>
        public string Decrypt(string ciphertext)
        {
            return this.Decrypt(Convert.FromBase64String(ciphertext));
        }

        /// <summary>
        /// Encrypt clear bytes using AES with the specified encryption key
        /// </summary>
        /// <param name="clearbytes">The array of bytes to encrypt.</param>
        /// <returns>The resulting encrypted value in Base64 encoding.</returns>
        public string Encrypt(byte[] clearbytes)
        {
            AesManaged aes = this.Crypto;
            ICryptoTransform transform = aes.CreateEncryptor();
            byte[] resultArray = transform.TransformFinalBlock(clearbytes, 0, clearbytes.Length);
            return Convert.ToBase64String(resultArray);
        }

        /// <summary>
        /// Encrypt clear text using AES with the specified encryption key.
        /// </summary>
        /// <param name="cleartext">The clear text to encrypt.</param>
        /// <returns>The resulting encrypted value in Base64 encoding.</returns>
        public string Encrypt(string cleartext)
        {
            cleartext = this.Extend(cleartext);
            return this.Encrypt(Encoding.Unicode.GetBytes(cleartext));
        }
        
        /// <summary>
        /// Get a byte array from a string value.
        /// </summary>
        /// <param name="bytes">A byte array to encrypt or decrypt.</param>
        /// <param name="encryptOrDecrypt">Whether we are encrypting or decrypting.</param>
        /// <returns>The resulting encrypted/decrypted byte array.</returns>
        public byte[] GetBytes(byte[] bytes, Action encryptOrDecrypt)
        {
            AesManaged aes = this.Crypto;
            byte[] resultArray;

            switch (encryptOrDecrypt)
            {
                case Action.Encrypt:
                    ICryptoTransform encrypt = aes.CreateEncryptor();
                    resultArray = encrypt.TransformFinalBlock(bytes, 0, bytes.Length);
                    break;

                case Action.Decrypt:
                    ICryptoTransform decrypt = aes.CreateDecryptor();
                    resultArray = decrypt.TransformFinalBlock(bytes, 0, bytes.Length);
                    break;

                default:
                    string err = string.Format("The function does not recognized Action with a value of '{0}'", ((int)encryptOrDecrypt).ToString());
                    throw new System.Exception(err);
            }

            return resultArray;
        }

        /// <summary>
        /// Get a byte array from a string value.
        /// </summary>
        /// <param name="text">A text string to encrypt or decrypt.</param>
        /// <param name="encryptOrDecrypt">Whether we are encrypting or decrypting.</param>
        /// <returns>The resulting encrypted/decrypted byte array.</returns>
        public byte[] GetBytes(string text, Action encryptOrDecrypt)
        {
            switch (encryptOrDecrypt)
            {
                case Action.Encrypt:
                    byte[] clearbytes = Encoding.Unicode.GetBytes(text);
                    return this.GetBytes(clearbytes, Action.Encrypt);

                case Action.Decrypt:
                    byte[] cipherbytes = Convert.FromBase64String(text);
                    return this.GetBytes(cipherbytes, Action.Decrypt);

                default:
                    string err = string.Format("The function does not recognized Action with a value of '{0}'", ((int)encryptOrDecrypt).ToString());
                    throw new System.Exception(err);
            }
        }

        /// <summary>
        /// Get a clear text or encoded string.
        /// </summary>
        /// <param name="text">A text string to encrypt or decrypt.</param>
        /// <param name="encryptOrDecrypt">Whether we are encrypting or decrypting.</param>
        /// <param name="method">Whether we are encoding or decoding.</param>
        /// <returns>The resulting encrypted/decrypted encoded/decoded string.</returns>
        public string GetString(string text, Action encryptOrDecrypt, EncodingMethod method)
        {
            AesManaged aes = this.Crypto;
            byte[] resultArray = this.GetBytes(text, encryptOrDecrypt);
            string resultString;

            switch (method)
            {
                case EncodingMethod.Base64:
                    resultString = Convert.ToBase64String(resultArray);
                    break;

                case EncodingMethod.Base32:
                    resultString = Base32.ToBase32String(resultArray);
                    break;

                case EncodingMethod.Base16:
                    resultString = Base16.ToBase16String(resultArray);
                    break;

                default:
                    resultString = Encoding.Unicode.GetString(resultArray);
                    break;
            }

            return resultString;
        }

        /// <summary>
        /// Pad out a Unicode string such that it ends on a key size boundary.
        /// </summary>
        /// <param name="input">An input string to extend.</param>
        /// <returns>The original input string plus space characters.</returns>
        private string Extend(string input)
        {
            // Boundary check - the input must be at least one character in length
            if (input == null) input = " ";
            if (input == string.Empty) input = " ";

            // The length is the next highest number evenly divisible by the keysize
            decimal characters = this.keysize / 16; // The keysize is in bits and each Unicode character is 16-bit.
            decimal inputlength = (decimal)input.Length;
            int length = (int)(Math.Ceiling(inputlength / characters) * characters);

            // Pad out the input with spaces
            for (int i = input.Length; i < length; i++)
            {
                input += " ";
            }

            return input;
        }
    }
}
