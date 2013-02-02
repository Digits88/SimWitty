// <copyright file="CollectorTools.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Configuration; // Reference
    using System.Data;
    using System.Security.Cryptography;

    /// <summary>
    /// A collection of tools for SimWitty Collector Services.
    /// </summary>
    public class CollectorTools
    {
        /// <summary>
        /// Encrypt the sourceString, returns this result as an AES encrypted, BASE64 encoded string
        /// </summary>
        /// <param name="plainSourceStringToEncrypt">String to encrypt</param>
        /// <returns>an AES encrypted, BASE64 encoded string</returns>
        public static string EncryptString(string plainSourceStringToEncrypt)
        {
            return EncryptString(plainSourceStringToEncrypt, SharedSecurityKey());
        }

        /// <summary>
        /// Encrypt the sourceString, returns this result as an AES encrypted, BASE64 encoded string
        /// </summary>
        /// <param name="plainSourceStringToEncrypt">String to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <returns>an AES encrypted, BASE64 encoded string</returns>
        public static string EncryptString(string plainSourceStringToEncrypt, byte[] key)
        {
            // Set up the encryption objects
            AesCryptoServiceProvider acsp = GetProvider(key);
            byte[] sourceBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(plainSourceStringToEncrypt);
            ICryptoTransform ictE = acsp.CreateEncryptor();
            
            // Set up stream to contain the encryption
            System.IO.MemoryStream msS = new System.IO.MemoryStream();
            
            // Perform the encrpytion, storing output into the stream
            CryptoStream csS = new CryptoStream(msS, ictE, CryptoStreamMode.Write);
            csS.Write(sourceBytes, 0, sourceBytes.Length);
            csS.FlushFinalBlock();
            
            // sourceBytes are now encrypted as an array of secure bytes
            byte[] encryptedBytes = msS.ToArray(); // .ToArray() is important, don't mess with the buffer
            
            return System.Convert.ToBase64String(encryptedBytes);
        }
        
        /// <summary>
        /// Decrypt a BASE64 encoded string of AES encrypted data
        /// </summary>
        /// <param name="base64StringToDecrypt">an AES encrypted, BASE64 encoded string</param>
        /// <returns>an unencrypted string</returns>
        public static string DecryptString(string base64StringToDecrypt)
        {
            return DecryptString(base64StringToDecrypt, SharedSecurityKey());
        }
 
        /// <summary>
        /// Decrypt a BASE64 encoded string of AES encrypted data
        /// </summary>
        /// <param name="base64StringToDecrypt">an AES encrypted, BASE64 encoded string</param>
        /// <param name="key">Encryption key</param>
        /// <returns>an unencrypted string</returns>
        public static string DecryptString(string base64StringToDecrypt, byte[] key)
        {            
            // Set up the encryption objects
            AesCryptoServiceProvider acsp = GetProvider(key);
            byte[] rawBytes = System.Convert.FromBase64String(base64StringToDecrypt);
            ICryptoTransform ictD = acsp.CreateDecryptor();
            
            // RawBytes now contains original byte array, still in Encrypted state
            
            // Decrypt into stream
            System.IO.MemoryStream msD = new System.IO.MemoryStream(rawBytes, 0, rawBytes.Length);
            CryptoStream csD = new CryptoStream(msD, ictD, CryptoStreamMode.Read);
            
            // csD now contains original byte array, fully decrypted
            
            // return the content of msD as a regular string
            return (new System.IO.StreamReader(csD)).ReadToEnd();
        }

        /// <summary>
        /// Obtain the shared encryption key in a byte[] array. 
        /// </summary>
        /// <returns>Encryption key</returns>
        public static byte[] SharedSecurityKey()
        {
            // TODO - Develop a shared PKI infrastructure for SimWitty Collectors and central database
            byte[] key = { 6, 5, 4, 3, 2, 3, 5, 7, 11, 17 };
            return key;
        }
        
        /// <summary>
        /// Fill in the Collector table values (name, key, enabled).
        /// </summary>
        /// <param name="collector">The collector name (reference).</param>
        /// <param name="collectorId">The collector ID in the table (reference).</param>
        /// <param name="collectorEnabled">Whether the collector is enabled or not (reference).</param>
        /// <param name="sqlConnectString">SQL connection string.</param>
        /// <returns>null if successful, otherwise, returns the last Exception.</returns>
        public static System.Exception FillCollector(ref string collector, ref int collectorId, ref bool collectorEnabled, string sqlConnectString)
        {
            // Set the default values
            // These are what the app will see if an error has occurred
            collector = "<unknown>";
            collectorId = -1;
            collectorEnabled = false;
            
            // Using Null-Coalescing Operator ( ?? )
            // Collector is from AppSettings if not null, otherwise from MachineName
            // Collector is from AppSettings if not empty, otherwise from MachineName
            collector = ConfigurationManager.AppSettings["Collector"] ?? System.Environment.MachineName.ToLower();
            if (collector.Length == 0) collector = System.Environment.MachineName.ToLower();

            // Connect to the database
            Database db = new Database(sqlConnectString);
            db.Connect();

            if (!db.IsConnected)
            {
                if (db.HasException)
                    return db.LastException;
                else
                    return null;
            }

            // Get the collector ID
            string q = "SELECT KeyCollector, Enabled From Collector Where Collector='" + collector + "';";
            DataTable table = db.GetDataTable(q);

            if (table.Rows.Count == 0)
            {
                return new Exception("An entry for " + collector + " is not present in the Collector table. Cannot continue.");
            }

            try
            {
                collectorId = Convert.ToInt32(table.Rows[0]["KeyCollector"]);
                collectorEnabled = Convert.ToInt32(table.Rows[0]["Enabled"]) == 1;
                db.Disconnect();
            }
            catch (Exception ex)
            {
                db.Disconnect();
                return ex;
            }

            if (!collectorEnabled)
            {
                return new Exception("Unable to find an enabled and valid entry in the Collector table for the collector named '" + collector + "'.");
            }

            return null;
        }
        
        /// <summary>
        /// Set up the AES encryption object
        /// </summary>
        /// <param name="key">Encryption key</param>
        /// <returns>Returns an AES cryptographic provider.</returns>
        private static AesCryptoServiceProvider GetProvider(byte[] key)
        {
            // Set up the encryption objects
            AesCryptoServiceProvider result = new AesCryptoServiceProvider();
            byte[] realKey = GetKey(key, result);
            result.Key = realKey;
            result.IV = realKey;
            return result;
        }

        /// <summary>
        /// Get a cryptographic key that matches the provider's key length.
        /// </summary>
        /// <param name="suggestedKey">The encryption key to use.</param>
        /// <param name="p">The cryptographic provider to use.</param>
        /// <returns>Returns a padded or truncated key that matches the provider's length.</returns>
        private static byte[] GetKey(byte[] suggestedKey, AesCryptoServiceProvider p)
        {
            byte[] rawKey = suggestedKey;
            List<byte> keyList = new List<byte>();

            for (int i = 0; i < p.LegalKeySizes[0].MinSize / 8; i++)
            {
                keyList.Add(rawKey[(i / 8) % rawKey.Length]);
            }

            byte[] k = keyList.ToArray();
            return k;
        }
    }
}
