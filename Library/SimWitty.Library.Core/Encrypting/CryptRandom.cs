// <copyright file="CryptRandom.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

/*
 * Original version by Stephen Toub and Shawn Farkas.
 * Random pool and thread safety added by Markus Olsson (freakcode.com).
 * 
 * Original source: http://msdn.microsoft.com/en-us/magazine/cc163367.aspx
 * 
 * Some benchmarks (2009-03-18):
 * 
 *  Results produced by calling Next() 1 000 000 times on my machine (dual core 3Ghz)
 * 
 *      System.Random completed in 20.4993 ms (avg 0 ms) (first: 0.3454 ms)
 *      CryptoRandom with pool completed in 132.2408 ms (avg 0.0001 ms) (first: 0.025 ms)
 *      CryptoRandom without pool completed in 2 sec 587.708 ms (avg 0.0025 ms) (first: 1.4142 ms)
 *      
 *      |---------------------|------------------------------------|
 *      | Implementation      | Slowdown compared to System.Random |
 *      |---------------------|------------------------------------|
 *      | System.Random       | 0                                  |
 *      | CryptoRand w pool   | 6,6x                               |
 *      | CryptoRand w/o pool | 19,5x                              |
 *      |---------------------|------------------------------------|
 * 
 * ent (http://www.fourmilab.ch/) results for 16mb of data produced by this class:
 * 
 *  > Entropy = 7.999989 bits per byte.
 *  >
 *  > Optimum compression would reduce the size of this 16777216 byte file by 0 percent.
 *  >
 *  > Chi square distribution for 16777216 samples is 260.64, 
 *  > and randomly would exceed this value 50.00 percent of the times.
 *  >
 *  > Arithmetic mean value of data bytes is 127.4974 (127.5 = random).
 *  > Monte Carlo value for Pi is 3.141838823 (error 0.01 percent).
 *  > Serial correlation coefficient is 0.000348 (totally uncorrelated = 0.0).
 * 
 *  your mileage may vary ;)
 *  
 * Updated for the SimWitty project (2012-0819)
 * Minor changes to the namespace, formatting, et cetera.
 *  
 */

namespace SimWitty.Library.Core.Encrypting
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;

    /// <summary>
    /// A random number generator based on the RNGCryptoServiceProvider.
    /// Adapted from the "Tales from the CryptoRandom" article in MSDN Magazine (September 2007)
    /// but with explicit guarantee to be thread safe. Note that this implementation also includes
    /// an optional (enabled by default) random buffer which provides a significant speed boost as
    /// it greatly reduces the amount of calls into unmanaged land.
    /// </summary>
    public class CryptRandom : Random
    {
        /// <summary>
        /// Internal RNGCryptoServiceProvider
        /// </summary>
        private RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        /// <summary>
        /// Internal byte buffer
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// Internal position within the buffer
        /// </summary>
        private int bufferPosition;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CryptRandom"/> class with.
        /// Using this overload will enable the random buffer pool.
        /// </summary>
        public CryptRandom() : this(true) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptRandom"/> class.
        /// This method will disregard whatever value is passed as seed and it's only implemented
        /// in order to be fully backwards compatible with <see cref="System.Random"/>.
        /// Using this overload will enable the random buffer pool.
        /// </summary>
        /// <param name="ignoredSeed">The ignored seed.</param>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ignoredSeed", Justification = "Cannot remove this parameter as we implement the full API of System.Random")]
        public CryptRandom(int ignoredSeed) : this(true) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptRandom"/> class with
        /// optional random buffer.
        /// </summary>
        /// <param name="enableRandomPool">set to <c>true</c> to enable the random pool buffer for increased performance.</param>
        public CryptRandom(bool enableRandomPool)
        {
            this.IsRandomPoolEnabled = enableRandomPool;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has random pool enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has random pool enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsRandomPoolEnabled { get; private set; }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue"/>.
        /// </returns>
        public override int Next()
        {
            // Mask away the sign bit so that we always return nonnegative integers
            return (int)this.GetRandomUInt32() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to zero.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals zero, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="maxValue"/> is less than zero.
        /// </exception>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue");

            return this.Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/> but not <paramref name="maxValue"/>. If <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="minValue"/> is greater than <paramref name="maxValue"/>.
        /// </exception>
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException("minValue");

            if (minValue == maxValue)
                return minValue;

            long diff = maxValue - minValue;

            while (true)
            {
                uint rand = this.GetRandomUInt32();

                long max = 1 + (long)uint.MaxValue;
                long remainder = max % diff;

                if (rand < max - remainder)
                    return (int)(minValue + (rand % diff));
            }
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble()
        {
            return this.GetRandomUInt32() / (1.0 + uint.MaxValue);
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="buffer"/> is null.
        /// </exception>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            lock (this)
            {
                if (this.IsRandomPoolEnabled && this.buffer == null)
                    this.InitBuffer();

                // Can we fit the requested number of bytes in the buffer?
                if (this.IsRandomPoolEnabled && this.buffer.Length <= buffer.Length)
                {
                    int count = buffer.Length;

                    this.EnsureRandomBuffer(count);

                    Buffer.BlockCopy(this.buffer, this.bufferPosition, buffer, 0, count);

                    this.bufferPosition += count;
                }
                else
                {
                    // Draw bytes directly from the RNGCryptoProvider
                    this.rng.GetBytes(buffer);
                }
            }
        }

        /// <summary>
        /// Ensures that we have enough bytes in the random buffer.
        /// </summary>
        /// <param name="requiredBytes">The number of required bytes.</param>
        private void EnsureRandomBuffer(int requiredBytes)
        {
            if (this.buffer == null)
                this.InitBuffer();

            if (requiredBytes > this.buffer.Length)
                throw new ArgumentOutOfRangeException("requiredBytes", "cannot be greater than random buffer");

            if ((this.buffer.Length - this.bufferPosition) < requiredBytes)
                this.InitBuffer();
        }

        /// <summary>
        /// Gets one random unsigned 32-bit integer in a thread safe manner.
        /// </summary>
        /// <returns>A random unsigned 32-bit integer.</returns>
        private uint GetRandomUInt32()
        {
            lock (this)
            {
                this.EnsureRandomBuffer(4);

                uint rand = BitConverter.ToUInt32(this.buffer, this.bufferPosition);

                this.bufferPosition += 4;

                return rand;
            }
        }

        /// <summary>
        /// Initialize the buffer array.
        /// </summary>
        private void InitBuffer()
        {
            if (this.IsRandomPoolEnabled)
            {
                if (this.buffer == null || this.buffer.Length != 512)
                    this.buffer = new byte[512];
            }
            else
            {
                if (this.buffer == null || this.buffer.Length != 4)
                    this.buffer = new byte[4];
            }

            this.rng.GetBytes(this.buffer);
            this.bufferPosition = 0;
        }
    }
}