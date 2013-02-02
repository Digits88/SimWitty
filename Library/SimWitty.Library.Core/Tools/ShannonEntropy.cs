// <copyright file="ShannonEntropy.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;

    /// <summary>
    /// A tool to calculate Shannon Entropy, a value representing the potential information within a collection.
    /// </summary>
    public static class ShannonEntropy
    {
        /// <summary>
        /// The index in a double[][] array for the Count value.
        /// </summary>
        private static int count = 0;

        /// <summary>
        /// The index in a double[][] array for the Frequency value.
        /// </summary>
        private static int frequency = 1;

        /// <summary>
        /// The index in a double[][] array for the Encoded value.
        /// </summary>
        private static int encoded = 2;

        /// <summary>
        /// Calculate Shannon Entropy for an array of characters.
        /// </summary>
        /// <param name="bytes">A value set comprised of bytes.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        public static double Calculate(byte[] bytes)
        {
            double[][] t;
            if (bytes.Length == 0) return 0;
            return Calculate(bytes, out t);
        }

        /// <summary>
        /// Calculate Shannon Entropy for an array of bytes.
        /// </summary>
        /// <param name="bytes">A value set comprised of bytes.</param>
        /// <param name="table">A DataTable containing the columns: Value, Count, Frequency, Encoded.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        public static double Calculate(byte[] bytes, out double[][] table)
        {
            // Create the DataTable to be used for calculating entropy
            table = new double[256][];

            for (int i = 0; i < table.Length; i++)
            {
                table[i] = new double[3];
                table[i][count] = 0.0;
                table[i][frequency] = 0.0;
                table[i][encoded] = 0.0;
            }

            // Check the array length
            if (bytes.Length == 0) return 0;

            // Add each character to the DataTable along with the count of that character
            foreach (byte b in bytes)
            {
                int index = Convert.ToInt32(b);
                table[index][count]++;
            }

            return CalculateInternally(ref table);
        }

        /// <summary>
        /// Calculate Shannon Entropy for an array of characters.
        /// </summary>
        /// <param name="characters">A value set comprised of characters.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        public static double Calculate(char[] characters)
        {
            double[][] t;
            if (characters.Length == 0) return 0;
            return Calculate(characters, out t);
        }

        /// <summary>
        /// Calculate Shannon Entropy for an array of characters.
        /// </summary>
        /// <param name="characters">A value set comprised of characters.</param>
        /// <param name="table">A DataTable containing the columns: Value, Count, Frequency, Encoded.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        public static double Calculate(char[] characters, out double[][] table)
        {
            // Create the DataTable to be used for calculating entropy (ushort = 16 bit = 2 byte = 1 Unicode character)
            table = new double[ushort.MaxValue][];

            for (int i = 0; i < table.Length; i++)
            {
                table[i] = new double[3];
                table[i][count] = 0.0;
                table[i][frequency] = 0.0;
                table[i][encoded] = 0.0;
            }

            // Check the array length
            if (characters.Length == 0) return 0;

            // Add each character to the DataTable along with the count of that character
            foreach (char c in characters)
            {
                ushort index = BitConverter.ToUInt16(System.Text.Encoding.Unicode.GetBytes(new char[] { c }), 0);
                table[index][count]++;
            }

            return CalculateInternally(ref table);
        }

        /// <summary>
        /// Calculate Shannon Entropy for an array of characters.
        /// </summary>
        /// <param name="text">A value set comprised of characters in a text string.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        public static double Calculate(string text)
        {
            if (text == string.Empty) return 0;
            return Calculate(text.ToCharArray());
        }

        /// <summary>
        /// Return an array of bytes that contains the message along with random noise that spoofs the entropy value of a communications channel.
        /// </summary>
        /// <param name="signal">The byte array containing the communications.</param>
        /// <param name="channel">The byte array containing values consistent with the entropy of the current communications channel.</param>
        /// <param name="length">The resulting length of array that includes the message and the noise.</param>
        /// <returns>The resulting array will contain: { byte[4] signal length} + { byte[] signal } + { byte[] noise }.</returns>
        public static byte[] Spoof(byte[] signal, byte[] channel, uint length)
        {
            byte[] result = new byte[1];

            // The first four bytes contain the data array length
            byte[] lengthBytes = BitConverter.GetBytes(Convert.ToUInt32(signal.Length));

            // Check the length and return just the signal if the value is too short
            if (length <= (signal.Length + lengthBytes.Length))
            {
                result = new byte[lengthBytes.Length + signal.Length];
                Array.Copy(lengthBytes, 0, result, 0, lengthBytes.Length);
                Array.Copy(signal, 0, result, lengthBytes.Length, signal.Length);
                return result;
            }
            
            // Calculate the starting entropy for the message signal
            double[][] signalEntropy;
            double entropy = Calculate(signal, out signalEntropy);

            // Add the length bytes into the stock entropy
            for (int i = 0; i < lengthBytes.Length; i++)
            {
                int index = Convert.ToInt32(lengthBytes[i]);
                signalEntropy[index][count]++;
            }

            // Calculate the entropy to be spoofed - existing communications channel
            double[][] stockEntropy;
            double channelEntropy = Calculate(channel, out stockEntropy);

            // Populate a buffer with values bringing the signal entropy into line with the channel entropy
            byte[] buffer = new byte[signal.Length + channel.Length + 1];
            int noiseIndex = 0;

            for (int i = 0; i < stockEntropy.Length; i++)
            {
                // If this byte is not in the channel, skip to the next byte
                if (stockEntropy[i][count] == 0) continue;
                
                // Start counting at how many of this byte is in the signal array
                double start = 0;
                if (i < signalEntropy.Length) start = signalEntropy[i][count];

                // Stop counting based on the frequency this byte in the channel
                double stop = stockEntropy[i][frequency] * (double)length;

                // If start is greater than stop, we have enough of this byte in the noise, skip to the next byte
                if (start >= stop) continue;

                byte value = Convert.ToByte(i);
                for (double d = start; d < stop; d++)
                {
                    buffer[noiseIndex] = value;
                    noiseIndex++;
                }

                if (noiseIndex >= (length - lengthBytes.Length - signal.Length)) break;
            }

            // Copy the buffer into the noise byte array and randomize
            byte[] noise = new byte[noiseIndex];
            Array.Copy(buffer, noise, noiseIndex);
            SimWitty.Library.Core.Tools.ArrayTools.Scramble(noise);

            // Create the resulting output array and copy in the length, the data, and the noise
            result = new byte[lengthBytes.Length + signal.Length + noise.Length];
            Array.Copy(lengthBytes, 0, result, 0, lengthBytes.Length);
            Array.Copy(signal, 0, result, lengthBytes.Length, signal.Length);
            Array.Copy(noise, 0, result, lengthBytes.Length + signal.Length, noise.Length);
            return result;
        }

        /// <summary>
        /// Populate the Frequency and Encoded columns and calculate the final entropy value.
        /// </summary>
        /// <param name="valuesTable">A DataTable containing the columns: Value, Count, Frequency, Encoded.</param>
        /// <returns>A positive value indicating the Shannon entropy of the value set.</returns>
        private static double CalculateInternally(ref double[][] valuesTable)
        {
            double total = 0;
            double result = 0;

            // Determine the total number of values in the set by summing the count of each value
            for (int i = 0; i < valuesTable.Length; i++)
            {
                total += valuesTable[i][count];
            }

            // Determine the frequency (count/total) and encoded entropy (Log2 of frequency * frequency) 
            for (int i = 0; i < valuesTable.Length; i++)
            {
                if (valuesTable[i][count] != 0)
                {
                    valuesTable[i][frequency] = valuesTable[i][count] / total;
                    valuesTable[i][encoded] = Math.Log(valuesTable[i][frequency], 2) * valuesTable[i][frequency];
                    result += valuesTable[i][encoded];
                }
            }

            // Results can be compared with:
            // http://www.shannonentropy.netmark.pl/calculate
            // Some differences expected in that this code uses a double rather than rounded to the nearest three digits

            // The encoded entropy is the absolute value of the sum of all encoded entropies
            return Math.Abs(result);
        }
    }
}