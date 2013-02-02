// <copyright file="StringTools.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;
    using System.Collections;

    /// <summary>
    /// A collection of tools for working with strings.
    /// </summary>
    public static class StringTools
    {
        /// <summary>
        /// Does string A start with string B? Defaults to ignoring casing.
        /// </summary>
        /// <param name="stringA">The first string to compare.</param>
        /// <param name="stringB">The string prefix to search for.</param>
        /// <returns>Returns a value indicating whether string A begins with string B.</returns>
        public static bool StartsWith(string stringA, string stringB)
        {
            return StartsWith(stringA, stringB, true);
        }

        /// <summary>
        /// Does string A start with string B?
        /// </summary>
        /// <param name="stringA">The first string to compare.</param>
        /// <param name="stringB">The string prefix to search for.</param>
        /// <param name="ignoreCase">True to ignore case during the comparison; otherwise, false.</param>
        /// <returns>Returns a value indicating whether string A begins with string B.</returns>
        public static bool StartsWith(string stringA, string stringB, bool ignoreCase)
        {
            if (stringA.Length < stringB.Length) return false;

            stringA = stringA.Substring(0, stringB.Length);
            int compare = string.Compare(stringA, stringB, ignoreCase);

            if (compare == 0) return true;
            else return false;
        }
    }
}
