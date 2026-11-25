using System;

namespace WebExpress.WebIndex.Utility
{
    /// <summary>
    /// Represents a utility class for computing the Levenshtein distance between two strings.
    /// </summary>
    public class IndexFuzzy
    {
        /// <summary>
        /// Computes the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="s">The first string to compare.</param>
        /// <param name="t">The second string to compare.</param>
        /// <returns>The number of single-character edits required to change one string into the other.</returns>
        /// <summary>
        /// The longest word compared by edit distance; longer words match only exactly.
        /// </summary>
        /// <remarks>
        /// The comparison costs the product of both lengths and runs once per word of the
        /// vocabulary. A query term may be thousands of characters long, which made a single fuzzy
        /// query cost millions of steps per vocabulary word. No real word is this long.
        /// </remarks>
        public const int MaxFuzzyLength = 64;

        private static int LevenshteinDistance(string s, string t, int limit = int.MaxValue)
        {
            int n = s.Length;
            int m = t.Length;

            // if first string is empty, the distance is the length of the second string
            if (n == 0) return m;
            // if second string is empty, the distance is the length of the first string
            if (m == 0) return n;

            // two rows suffice; the full matrix allocated the product of both lengths per call
            var previous = new int[m + 1];
            var current = new int[m + 1];

            for (int j = 0; j <= m; j++) { previous[j] = j; }

            for (int i = 1; i <= n; i++)
            {
                current[0] = i;
                var rowMinimum = current[0];

                for (int j = 1; j <= m; j++)
                {
                    int cost = t[j - 1] == s[i - 1] ? 0 : 1;

                    current[j] = Math.Min(
                        Math.Min(previous[j] + 1, current[j - 1] + 1),
                        previous[j - 1] + cost);
                    rowMinimum = Math.Min(rowMinimum, current[j]);
                }

                // the distance never falls below the smallest value of a row
                if (rowMinimum > limit)
                {
                    return rowMinimum;
                }

                (previous, current) = (current, previous);
            }

            return previous[m];
        }

        /// <summary>
        /// Tells whether two words are at least as similar as the threshold, by edit distance,
        /// stopping as soon as the outcome is certain.
        /// </summary>
        /// <param name="word1">The first word.</param>
        /// <param name="word2">The second word.</param>
        /// <param name="threshold">The least similarity, between 0 and 1.</param>
        /// <returns>True when the words are similar enough.</returns>
        public static bool IsSimilar(string word1, string word2, double threshold)
        {
            word1 ??= string.Empty;
            word2 ??= string.Empty;

            var maxLength = Math.Max(word1.Length, word2.Length);

            if (maxLength == 0 || string.Equals(word1, word2, StringComparison.Ordinal))
            {
                return true;
            }

            if (maxLength > MaxFuzzyLength)
            {
                return false;
            }

            // the similarity is 1 - distance / maxLength, so the threshold bounds the distance;
            // the length difference alone is a lower bound of it
            var limit = (int)Math.Floor((1.0 - threshold) * maxLength + 1e-9);

            if (Math.Abs(word1.Length - word2.Length) > limit)
            {
                return false;
            }

            return LevenshteinDistance(word1, word2, limit) <= limit;
        }

        /// <summary>
        /// Calculates the similarity between two strings based on the Levenshtein
        /// distance, normalized to the longer word. Identical strings yield 1.0,
        /// completely different strings yield 0.0.
        /// </summary>
        /// <param name="word1">The first string to compare.</param>
        /// <param name="word2">The second string to compare.</param>
        /// <returns>A double value in [0, 1] representing the similarity.</returns>
        public static double CalculateLevenshteinSimilarity(string word1, string word2)
        {
            int distance = LevenshteinDistance(word1, word2);
            int maxLength = Math.Max(word1.Length, word2.Length);

            if (maxLength == 0)
            {
                return 1.0;
            }

            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Computes the length of the longest common subsequence (LCS) between two strings.
        /// </summary>
        /// <param name="s1">The first string to compare.</param>
        /// <param name="s2">The second string to compare.</param>
        /// <returns>The length of the longest common subsequence.</returns>
        private static int LongestCommonSubsequence(string s1, string s2)
        {
            int[,] dp = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            return dp[s1.Length, s2.Length];
        }

        /// <summary>
        /// Calculates the similarity between two strings based on the LCS length.
        /// </summary>
        /// <param name="word1">The first string to compare.</param>
        /// <param name="word2">The second string to compare.</param>
        /// <returns>A double value representing the similarity percentage.</returns>
        public static double CalculateLCSSimilarity(string word1, string word2)
        {
            int lcsLength = LongestCommonSubsequence(word1, word2);
            int maxLength = Math.Max(word1.Length, word2.Length);
            return (double)lcsLength / maxLength;
        }
    }
}
