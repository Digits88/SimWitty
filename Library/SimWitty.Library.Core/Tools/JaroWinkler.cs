// <copyright file="JaroWinkler.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Core.Tools
{
    using System;

    /// <summary>
    /// This tools class automates executing the original Jaro algorithm (Calculate Distance) and the adjusted Jaro-Winkler algorithm (Calculate Adjusted Distance.)
    /// The Jaro-Winkler distance is a measure of similarity between two strings. The score is normalized such that 0 equates to no similarity and 1 is an exact match.
    /// </summary>
    public static class JaroWinkler
    {
        /// <summary>
        /// Jaro-Winkler constant for weighting the prefix scale.
        /// </summary>
        private const double ConstPrefixScale = 0.1;

        /// <summary>
        /// Jaro-Winkler constant for the minimum length of the prefix.
        /// </summary>
        private const int ConstPrefixLength = 4;

        /// <summary>
        /// Calculate the Jaro–Winkler distance, including the adjustment for the string prefix.
        /// </summary>
        /// <param name="sourceString">The first string to compare.</param>
        /// <param name="referenceString">The reference string to compare to.</param>
        /// <returns>Returns a distance score that is normalized such that 0 equates to no similarity and 1 is an exact match.</returns>
        public static double CalculateAdjustedDistance(string sourceString, string referenceString)
        {
            double distance = CalculateDistance(sourceString, referenceString);
            double prefix = GetPrefixLength(sourceString.ToLower().ToCharArray(), referenceString.ToLower().ToCharArray());
            double result = distance + (prefix * ConstPrefixScale * (1.0 - distance));
            return result;
        }

        /// <summary>
        /// Calculate the Jaro distance, excluding the adjustment for the string prefix.
        /// </summary>
        /// <param name="sourceString">The first string to compare.</param>
        /// <param name="referenceString">The reference string to compare to.</param>
        /// <returns>Returns a distance score that is normalized such that 0 equates to no similarity and 1 is an exact match.</returns>
        public static double CalculateDistance(string sourceString, string referenceString)
        {
            if (sourceString == string.Empty) return 0;
            if (referenceString == string.Empty) return 0;

            char[] source = sourceString.ToLower().ToCharArray();
            char[] reference = referenceString.ToLower().ToCharArray();

            // Calculate the matching window or theoretical distance between a character in one string and the same character in another
            double upperBound = (double)Math.Max(source.Length, reference.Length);
            double matchWindow = Math.Floor(upperBound / 2.0) - 1;

            // Find the common characters looking forward: source to reference
            string commonsForward = GetCommonCharacters(source, reference, matchWindow);
            if (commonsForward == string.Empty) return 0;

            // Find the common characters looking in reverse: reference to source
            string commonsReverse = GetCommonCharacters(reference, source, matchWindow);
            if (commonsReverse == string.Empty) return 0;

            // Calculate the transpositions
            char[] forward = commonsForward.ToCharArray();
            char[] reverse = commonsReverse.ToCharArray();
            double transpositions = 0;

            for (int i = 0; i < Math.Min(forward.Length, reverse.Length); i++)
            {
                if (forward[i] != reverse[i]) transpositions++;
            }

            // Calculate the Jaro-Winkler distance value
            // http://en.wikipedia.org/wiki/Jaro%E2%80%93Winkler_distance
            double m = Convert.ToDouble(forward.Length);
            double s1 = Convert.ToDouble(source.Length);
            double s2 = Convert.ToDouble(reference.Length);
            double t = transpositions / 2;

            double score = ((m / s1) + (m / s2) + ((m - t) / m)) / 3;
            if (score > 1.0) score = 1.0;
            if (score < 0.0) score = 0.0;

            return score;
        }

        /// <summary>
        /// Get the common characters from a primary and secondary array.
        /// </summary>
        /// <param name="primary">The first array to compare.</param>
        /// <param name="comparison">The reference array to compare to.</param>
        /// <param name="window">The window (+/-) where, if the character is found, a match is recorded.</param>
        /// <returns>The common characters.</returns>
        private static string GetCommonCharacters(char[] primary, char[] comparison, double window)
        {
            string value = string.Empty;

            char[] secondary = new char[comparison.Length];
            Array.Copy(comparison, secondary, secondary.Length);

            for (int primaryIndex = 0; primaryIndex < primary.Length; primaryIndex++)
            {
                int low = (int)Math.Max(0, primaryIndex - window);
                int high = (int)Math.Min(primaryIndex + window, secondary.Length);

                for (int secondaryIndex = low; secondaryIndex < high; secondaryIndex++)
                {
                    if (primary[(int)primaryIndex] == secondary[(int)secondaryIndex])
                    {
                        value += primary[(int)primaryIndex];
                        secondary[(int)secondaryIndex] = '\0';

                        break;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Calculate the prefix length value for the Jaro-Winkler adjustment.
        /// </summary>
        /// <param name="primary">The first array to compare.</param>
        /// <param name="comparison">The reference array to compare to.</param>
        /// <returns>The index of the first non-matching character.</returns>
        private static double GetPrefixLength(char[] primary, char[] comparison)
        {
            int length = Math.Min(primary.Length, comparison.Length);
            length = Math.Min(length, ConstPrefixLength);

            for (int i = 0; i < length; i++)
            {
                if (primary[i] != comparison[i]) return (double)i;
            }

            return (double)length;
        }
    }
}
