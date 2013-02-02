// <copyright file="ArrayTools.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;
    using SimWitty.Library.Core.Encrypting;

    /// <summary>
    /// A collection of tools for working with arrays.
    /// </summary>
    public static class ArrayTools
    {
        /// <summary>
        /// Randomly rearrange the elements in an array (in a thread safe manner).
        /// </summary>
        /// <typeparam name="T">An array of values.</typeparam>
        /// <param name="array">An array of values to shuffle.</param>
        public static void Scramble<T>(T[] array)
        {
            CryptRandom rand = new CryptRandom();
            int length = array.Length;

            while (length > 1)
            {
                int index = rand.Next(length--);
                T temp = array[length];
                array[length] = array[index];
                array[index] = temp;
            }
        }
    }
}
