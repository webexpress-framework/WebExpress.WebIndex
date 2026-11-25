using System.Diagnostics;
using WebExpress.WebIndex.Utility;
using Xunit;

namespace WebExpress.WebIndex.Test.Utility
{
    /// <summary>
    /// Tests that the bounded similarity check decides like the full edit distance, and that a
    /// long query term no longer costs the product of both lengths per vocabulary word.
    /// </summary>
    public class UnitTestIndexFuzzyBounds
    {
        /// <summary>
        /// Verifies that the bounded check agrees with the similarity it replaces.
        /// </summary>
        [Fact]
        public void IsSimilarAgreesWithTheFullDistance()
        {
            // arrange
            var random = new Random(4711);
            string Word() => new([.. Enumerable.Range(0, random.Next(0, 12)).Select(_ => (char)('a' + random.Next(0, 4)))]);

            for (var i = 0; i < 5000; i++)
            {
                var a = Word();
                var b = Word();
                var percent = random.Next(1, 100);
                var threshold = percent / 100.0;
                var max = Math.Max(a.Length, b.Length);
                var distance = (int)Math.Round((1 - IndexFuzzy.CalculateLevenshteinSimilarity(a, b)) * max);

                // act: decided in whole numbers, free of the rounding of 1 - distance / max
                var expected = max == 0 || 100 * (max - distance) >= percent * max;

                // validation
                Assert.True(expected == IndexFuzzy.IsSimilar(a, b, threshold), $"'{a}' ~ '{b}' at {threshold}");
            }
        }

        /// <summary>
        /// Verifies that a query term far longer than any word is decided at once.
        /// </summary>
        [Fact]
        public void LongTermsAreDecidedAtOnce()
        {
            // arrange
            var term = new string('a', 16_000);
            var vocabulary = Enumerable.Range(0, 2000).Select(x => new string('a', 10 + x % 50)).ToList();
            var watch = Stopwatch.StartNew();

            // act
            var matches = vocabulary.Count(x => IndexFuzzy.IsSimilar(term, x, 0.01));

            // validation
            Assert.Equal(0, matches);
            Assert.True(watch.ElapsedMilliseconds < 1000, $"took {watch.ElapsedMilliseconds} ms");
        }
    }
}
