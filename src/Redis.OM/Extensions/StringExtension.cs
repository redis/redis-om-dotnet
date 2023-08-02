using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Redis.OM.Extensions
{
    /// <summary>
    /// String Search Exention.
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Fuzzy matches are performed based on Levenshtein distance (LD) 1.
        /// </summary>
        /// <param name="str">string.</param>
        /// <param name="term">Search term.</param>
        /// <returns>true if found.</returns>
        public static bool ConstainsFuzzy1(this string str, string term)
        {
            return false;
        }

        /// <summary>
        /// Fuzzy matches are performed based on Levenshtein distance (LD) 2.
        /// </summary>
        /// <param name="str">string.</param>
        /// <param name="term">Search term.</param>
        /// <returns>true if found.</returns>
        public static bool ConstainsFuzzy2(this string str, string term)
        {
            return false;
        }
    }
}
