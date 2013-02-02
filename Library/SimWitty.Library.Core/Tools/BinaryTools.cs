// <copyright file="BinaryTools.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;
    using System.Collections;
 
    /// <summary>
    /// A collection of tools for working in binary with bits and bytes.
    /// </summary>
    public static class BinaryTools
    {
        /// <summary>
        /// Convert BitArray to an array of bytes.
        /// </summary>
        /// <param name="bits">Bits to convert to bytes.</param>
        /// <returns>A byte array from the original BitArray.</returns>
        public static byte[] BitsToBytes(BitArray bits)
        {
            return BitsToBytes(bits, bits.Length);
        }

        /// <summary>
        /// Convert BitArray to an array of bytes.
        /// </summary>
        /// <param name="bits">Bits to convert to bytes.</param>
        /// <param name="length">The number of bits from the array to convert to bytes.</param>
        /// <returns>A byte array from the original BitArray.</returns>
        public static byte[] BitsToBytes(BitArray bits, int length)
        {
            bool[] tribbles = new bool[bits.Length];
            bits.CopyTo(tribbles, 0);
            return BitsToBytes(tribbles, length);
        }

        /// <summary>
        /// Convert an array of boolean values to an array of bytes.
        /// </summary>
        /// <param name="bits">Bits to convert to bytes.</param>
        /// <returns>A byte array from the original BitArray.</returns>
        public static byte[] BitsToBytes(bool[] bits)
        {
            return BitsToBytes(bits, bits.Length);
        }

        /// <summary>
        /// Convert an array of boolean values to an array of bytes.
        /// </summary>
        /// <param name="bits">Bits to convert to bytes.</param>
        /// <param name="length">The number of bits from the array to convert to bytes.</param>
        /// <returns>A byte array from the original BitArray.</returns>
        public static byte[] BitsToBytes(bool[] bits, int length)
        {
            // Boundary check the array length
            if (length > bits.Length) throw new ApplicationException("When calling BitsToBytes, the length parameter cannot exceed the length of the bits boolean array.");

            // Find the number of bytes, rounded down to the nearest whole number
            length = (int)(Math.Floor(length / 8.0) * 8);

            // Boundary check the array length
            if (length == 0) throw new ApplicationException("There are 8 bits to a byte. When calling BitsToBytes, pass in a bits array that is divisible by 8.");

            // Create the byte array and populate
            byte[] result = new byte[length / 8];

            for (int x = 0; x < length; x += 8)
            {
                for (int y = x; y < (x + 8); y++)
                {
                    if (bits[y])
                    {
                        result[x / 8] |= (byte)(1 << (y - x));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Convert an 8-bit BitArray to an equivalent 16-bit unsigned integer.
        /// </summary>
        /// <param name="bits">An 8-bit BitArray.</param>
        /// <returns>A 16-bit unsigned integer.</returns>
        public static ushort BitsToNumber(BitArray bits)
        {
            if (bits.Length != 8) throw new ApplicationException("BitsToNumber was passed an invalid BitArray. The BitArray must be 8-bit or 1-byte in length.");
            return BitsToNumber(bits[0], bits[1], bits[2], bits[3], bits[4], bits[5], bits[6], bits[7]);
        }
        
        /// <summary>
        /// Convert an 8-bit boolean to an equivalent 16-bit unsigned integer.
        /// </summary>
        /// <param name="one">A boolean representing the first bit.</param>
        /// <param name="two">A boolean representing the second bit.</param>
        /// <param name="three">A boolean representing the third bit.</param>
        /// <param name="four">A boolean representing the forth bit.</param>
        /// <param name="five">A boolean representing the fifth bit.</param>
        /// <param name="six">A boolean representing the sixth bit.</param>
        /// <param name="seven">A boolean representing the seventh bit.</param>
        /// <param name="eight">A boolean representing the eight bit.</param>
        /// <returns>A 16-bit unsigned integer.</returns>
        public static ushort BitsToNumber(bool one, bool two, bool three, bool four, bool five, bool six, bool seven, bool eight)
        {
            ushort result = 0;

            if (eight) result += 128;
            if (seven) result += 64;
            if (six) result += 32;
            if (five) result += 16;
            if (four) result += 8;
            if (three) result += 4;
            if (two) result += 2;
            if (one) result += 1;

            return result;
        }

        /// <summary>
        /// Blend to byte arrays together by inserting the bits of a secondary array into the bits of a primary array at a specific placement. For example, with a placement of 5, the first four bits come from the primary array followed by one bit from the secondary array.
        /// </summary>
        /// <param name="primaryBytes">An array of bytes that will serve as the primary array.</param>
        /// <param name="secondaryBytes">An array of bytes that will serve as a secondary array.</param>
        /// <param name="placement">The placement in the array of the secondary bits. For example, 5 means every fifth bit will be from the secondary array.</param>
        /// <returns>Returns a BitArray containing values from the primary and secondary arrays.</returns>
        public static BitArray BlendBits(byte[] primaryBytes, byte[] secondaryBytes, int placement)
        {
            // Bounds check the placement value
            if (placement < 2 || placement > 64) throw new ApplicationException("The minimum placement value for BlendBits is 2 and the maximum is 64.");

            // Bounds check the array length
            if (primaryBytes.Length < 1 || secondaryBytes.Length < 1) throw new ApplicationException("The minimum length for byte arrays is 1 for BlendBits.");

            // The length is either the ratio of the primary length and the placement (4/5) or secondary length (1/5), which ever is larger.
            int length = Math.Max(
                RatioCeiling(primaryBytes.Length, placement - 1, placement),
                RatioCeiling(secondaryBytes.Length, 1, placement));

            // Get a random byte array sized to the results length
            byte[] randomBytes = RandomBytes(length);

            // Convert the primary and secondary to BitArray, and create the result BitArray
            BitArray primary = new BitArray(primaryBytes);
            BitArray secondary = new BitArray(secondaryBytes);
            BitArray results = new BitArray(randomBytes);

            // Create index counters for the primary and seconary arrays
            int primaryIndex = 0;
            int secondaryIndex = 0;

            // For each index, if the modulus of the place is not 0, set the primary. Otherwise, set bit to the secondary array.
            for (int index = 0; index < results.Length; index++)
            {
                if ((index % placement) != 0)
                {
                    if (primaryIndex < primary.Length)
                    {
                        results.Set(index, primary.Get(primaryIndex));
                        primaryIndex++;
                    }
                }
                else
                {
                    if (secondaryIndex < secondary.Length)
                    {
                        results.Set(index, secondary.Get(secondaryIndex));
                        secondaryIndex++;
                    }
                }
            }

            // Return the resulting blended array
            return results;
        }

        /// <summary>
        /// Return the least significant bit from a byte.
        /// </summary>
        /// <param name="someByte">The byte to get the bit from.</param>
        /// <returns>The least significant bit as a boolean.</returns>
        public static bool LeastSignificantBit(byte someByte)
        {
            BitArray bits = new BitArray(new byte[] { someByte });
            return Convert.ToBoolean(bits.Get(0));
        }

        /// <summary>
        /// Return the least significant bit from a number.
        /// </summary>
        /// <param name="someNumber">The number to get the bit from.</param>
        /// <returns>The least significant bit as a boolean.</returns>
        public static bool LeastSignificantBit(int someNumber)
        {
            return Convert.ToBoolean(someNumber % 2);
        }

        /// <summary>
        /// Number of bits required to represent a number.
        /// </summary>
        /// <param name="someNumber">Any numeric value.</param>
        /// <returns>The least significant bit as a boolean.</returns>
        public static int NumberOfBits(int someNumber)
        {
            double position = Math.Ceiling(Math.Log(someNumber + 1, 2));
            return Convert.ToInt32(position);
        }

        /// <summary>
        /// Create an array of bytes a specific length and fill it with random values.
        /// </summary>
        /// <param name="length">The number of bytes to return in the array.</param>
        /// <returns>A random array of bytes.</returns>
        public static byte[] RandomBytes(int length)
        {
            byte[] result = new byte[length];

            // Create a new Random generator seeded with the length and the current time
            long s = length * DateTime.Now.Millisecond;
            int seed = (int)Math.Min(s, int.MaxValue);
            Random r = new Random(seed);

            // For every byte in the array, create a new random value
            for (int x = 0; x < result.Length; x++)
            {
                int i = r.Next(0, 255);
                result[x] = Convert.ToByte(i);
            }

            return result;
        }

        /// <summary>
        /// Set the least significant bit.
        /// </summary>
        /// <param name="input">Byte to update.</param>
        /// <param name="bit">Value to set the least significant bit.</param>
        /// <returns>Updated byte value.</returns>
        public static byte SetLeastSignificantBit(byte input, bool bit)
        {
            // Create a byte array so that we can use the BitArray object
            byte[] value = new byte[1];
            value[0] = input;

            // Populate the BitArray and set the least significant bit
            BitArray bits = new BitArray(value);
            bits.Set(0, bit);

            // Update the value from the BitArray, and return the first element
            bits.CopyTo(value, 0);
            return value[0];
        }

        /// <summary>
        /// Un-blend a BitArray into two arrays of bytes based on the placement of the bits. For example, with a placement of 5, the first four bits come from the primary array followed by one bit from the secondary array.
        /// </summary>
        /// <param name="bits">A BitArray containing bits from two source byte arrays.</param>
        /// <param name="primaryBytes">Reference to the resulting primary byte array.</param>
        /// <param name="secondaryBytes">Reference to the resulting secondary byte array.</param>
        /// <param name="placement">The placement in the array of the secondary bits. For example, 5 means every fifth bit will be from the secondary array.</param>
        public static void UnblendBits(BitArray bits, ref byte[] primaryBytes, ref byte[] secondaryBytes, int placement)
        {
            // Bounds check the placement value
            if (placement < 2 || placement > 64) throw new ApplicationException("The minimum placement value for UnblendBits is 2 and the maximum is 64.");
            
            // Create the primary and secondary BitArrays
            BitArray primary = new BitArray(bits.Length);
            BitArray secondary = new BitArray(bits.Length);

            // Create index counters for the primary and seconary arrays
            int primaryIndex = 0;
            int secondaryIndex = 0;

            // For each index, if the modulus of the place is not 0, set the primary bit. Otherwise, set the secondary bit.
            for (int index = 0; index < bits.Length; index++)
            {
                if ((index % placement) != 0)
                {
                    if (primaryIndex < primary.Length)
                    {
                        primary.Set(primaryIndex, bits.Get(index));
                        primaryIndex++;
                    }
                }
                else
                {
                    if (secondaryIndex < secondary.Length)
                    {
                        secondary.Set(secondaryIndex, bits.Get(index));
                        secondaryIndex++;
                    }
                }
            }

            // Convert the bits to bytes with a length not exceeding the values inputed (remember length starts at 1 and index starts at 0, so add 1)
            primaryBytes = BitsToBytes(primary, Math.Min(primaryIndex + 1, primary.Length));
            secondaryBytes = BitsToBytes(secondary, Math.Min(secondaryIndex + 1, secondary.Length));

            return;            
        }

        /// <summary>
        /// Calculate a ratio of k/x = n/d, where value is k, n is numerator, and d is denominator. Return the highest whole number for x.
        /// </summary>
        /// <param name="value">In the ratio of k/x = n/d, the value is k.</param>
        /// <param name="numerator">In the ratio of k/x = n/d, the numerator is n.</param>
        /// <param name="denominator">In the ratio of k/x = n/d, the denominator is d.</param>
        /// <returns>Return x in the ratio of k/x = n/d, rounded up to the nearest whole number.</returns>
        private static int RatioCeiling(int value, int numerator, int denominator)
        {
            /*
             *  k     n
             * --- = ---
             *  x     d
             * 
             * Solve for x.
             * 
             */

            decimal k = (decimal)value;
            decimal d = (decimal)denominator;
            decimal n = (decimal)numerator;
            decimal result = k * d / n;
            decimal x = Math.Ceiling(result);
            return (int)x;
        }
    }
}
