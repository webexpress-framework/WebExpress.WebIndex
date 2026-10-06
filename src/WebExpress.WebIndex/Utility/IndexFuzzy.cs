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
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // if first string is empty, the distance is the length of the second string
            if (n == 0) return m;
            // if second string is empty, the distance is the length of the first string
            if (m == 0) return n;

            // initialize the matrix
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            // compute the distances
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = t[j - 1] == s[i - 1] ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            // the distance is the value in the bottom right corner of the Levenshtein distance
            return d[n, m];
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
