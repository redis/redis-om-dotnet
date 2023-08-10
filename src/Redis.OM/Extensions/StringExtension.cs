using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Redis.OM
{
    /// <summary>
    /// String Search Extensions.
    /// </summary>
    public static class StringExtension
    {
        private static readonly char[] SplitChars;

        static StringExtension()
        {
            SplitChars = new[]
            {
                ',', '.', '<', '>', '{', '}', '[', ']', '"', '\'', ':', ';', '!', '@', '#', '$', '%', '^', '&', '*', '(',
                ')', '-', '+', '=', '~',
            };
        }

        /// <summary>
        /// Checks if the string Levenshtein distance between the source and term is less than the provided distance.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="term">The string to compare the source to.</param>
        /// <param name="distanceThreshold">The threshold for permissible distance (must be 3 or less).</param>
        /// <returns>Whether the strings are within the provided Levenshtein distance of each other.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if distanceThreshold is greater than 3.</exception>
        /// <remarks>This is meant to be a shadow method that runs within an expression, a working implementation is
        /// provided here for completeness.</remarks>
        public static bool FuzzyMatch(this string source, string term, byte distanceThreshold)
        {
            if (distanceThreshold > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceThreshold), distanceThreshold, "Distance must be less than 3.");
            }

            return source.LevenshteinDistance(term) <= distanceThreshold;
        }

        /// <summary>
        /// Checks the source string to see if any tokens within the source string start with the prefix.
        /// </summary>
        /// <param name="source">The string to check.</param>
        /// <param name="prefix">The prefix to look for within the string.</param>
        /// <returns>Whether any token within the source string starts with the prefix.</returns>
        /// <remarks>This is meant to be a shadow method that runs within an expression, a working implementation is
        /// provided here for completeness.</remarks>
        public static bool MatchStartsWith(this string source, string prefix)
        {
            var terms = source.Split(SplitChars);
            return terms.Any(t => t.StartsWith(prefix));
        }

        /// <summary>
        /// Checks the source string to see if any tokens within the source string ends with the suffix.
        /// </summary>
        /// <param name="source">The string to check.</param>
        /// <param name="suffix">The suffix to look for within the string.</param>
        /// <returns>Whether any token within the source string ends with the suffix.</returns>
        /// <remarks>This is meant to be a shadow method that runs within an expression, a working implementation is
        /// provided here for completeness.</remarks>
        public static bool MatchEndsWith(this string source, string suffix)
        {
            var terms = source.Split(SplitChars);
            return terms.Any(t => t.EndsWith(suffix));
        }

        /// <summary>
        /// Checks the source string to see if any tokens within the source contains the infix.
        /// </summary>
        /// <param name="source">The string to check.</param>
        /// <param name="infix">The infix to look for within the string.</param>
        /// <returns>Whether any token within the source string contains the infix.</returns>
        /// <remarks>This is meant to be a shadow method that runs within an expression, a working implementation is
        /// provided here for completeness.</remarks>
        public static bool MatchContains(this string source, string infix)
        {
            var terms = source.Split(SplitChars);
            return terms.Any(t => t.EndsWith(infix));
        }

        /// <summary>
        /// Wagner-Fischer dynamic programming string distance algorithm.
        /// </summary>
        /// <param name="source">The source string to check the distance from.</param>
        /// <param name="term">The destination string to check the distance to.</param>
        /// <returns>The Levenshtein distance.</returns>
        /// <remarks>This is meant to be a shadow method that runs within an expression, a working implementation is
        /// provided here for completeness.</remarks>
        private static int LevenshteinDistance(this string source, string term)
        {
            var d = new int[source.Length, term.Length];
            for (var i = 1; i < source.Length; i++)
            {
                d[i, 0] = i;
            }

            for (var j = 1; j < term.Length; j++)
            {
                d[0, j] = j;
            }

            for (var j = 1; j < term.Length; j++)
            {
                for (var i = 1; i < source.Length; i++)
                {
                    var substitutionCost = source[i] == term[j] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + substitutionCost);
                }
            }

            return d[source.Length - 1, term.Length - 1];
        }
    }
}
