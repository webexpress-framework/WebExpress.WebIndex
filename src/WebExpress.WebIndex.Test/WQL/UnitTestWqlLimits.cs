using System.Diagnostics;
using System.Globalization;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Wql;
using Xunit;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the limits that keep a statement from the network from exhausting the stack or the
    /// processor. A stack overflow cannot be caught in .NET and ends the whole server, so each of
    /// these statements has to come back as an ordinary parse error.
    /// </summary>
    public class UnitTestWqlLimits
    {
        /// <summary>
        /// Verifies that parentheses nested far beyond the limit are refused instead of recursing.
        /// </summary>
        [Fact]
        public void DeepParenthesesAreRefused()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>() { MaxInputLength = int.MaxValue };
            var input = new string('(', 100_000) + "text ~ 'a'" + new string(')', 100_000);

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.True(wql.HasErrors);
            Assert.Equal("webexpress.webindex:wql.too_deep", wql.Error.Message);
        }

        /// <summary>
        /// Verifies that function calls nested far beyond the limit are refused as well, since
        /// each call opens a parenthesis of its own.
        /// </summary>
        [Fact]
        public void DeepFunctionCallsAreRefused()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>() { MaxInputLength = int.MaxValue };
            var input = "text = " + string.Concat(Enumerable.Repeat("lower(", 50_000)) + "'a'" + new string(')', 50_000);

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.Equal("webexpress.webindex:wql.too_deep", wql.Error?.Message);
        }

        /// <summary>
        /// Verifies that nesting up to the limit still parses.
        /// </summary>
        [Fact]
        public void NestingAtTheLimitParses()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var input = new string('(', parser.MaxNestingDepth) + "text ~ 'a'" + new string(')', parser.MaxNestingDepth);

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that a chain of negations, which recurses once per "not", is refused.
        /// </summary>
        [Fact]
        public void LongNegationChainIsRefused()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>() { MaxInputLength = int.MaxValue };
            var input = string.Concat(Enumerable.Repeat("not ", 100_000)) + "text ~ 'a'";

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.Equal("webexpress.webindex:wql.too_complex", wql.Error?.Message);
        }

        /// <summary>
        /// Verifies that a chain of conditions longer than the limit is refused, because it
        /// becomes a tree as deep as the chain is long, and that a shorter one still parses.
        /// </summary>
        /// <param name="conditions">The number of conditions in the chain.</param>
        /// <param name="refused">Whether the chain is expected to be refused.</param>
        [Theory]
        [InlineData(200, false)]
        [InlineData(257, false)]
        [InlineData(258, true)]
        [InlineData(5_000, true)]
        public void LongConditionChain(int conditions, bool refused)
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>() { MaxInputLength = int.MaxValue };
            var input = string.Join(" or ", Enumerable.Repeat("text ~ 'a'", conditions));

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.Equal(refused, wql.HasErrors);
            if (refused)
            {
                Assert.Equal("webexpress.webindex:wql.too_complex", wql.Error.Message);
            }
        }

        /// <summary>
        /// Verifies that a statement longer than the limit is refused before it is tokenized, and
        /// that the limit can be raised.
        /// </summary>
        [Fact]
        public void LongStatementIsRefused()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var input = "text ~ '" + new string('a', parser.MaxInputLength) + "'";

            // act
            var refused = parser.Parse(input);
            parser.MaxInputLength = input.Length;
            var accepted = parser.Parse(input);

            // validation
            Assert.Equal("webexpress.webindex:wql.too_long", refused.Error?.Message);
            Assert.False(accepted.HasErrors);
        }

        /// <summary>
        /// Verifies that parentheses and keywords inside a quoted value are data and do not count
        /// against the limits.
        /// </summary>
        [Fact]
        public void QuotedContentIsNoSyntax()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();

            // act
            var parenthesis = parser.Parse("text = '('");
            var keyword = parser.Parse("text = 'not'");

            // validation
            Assert.False(parenthesis.HasErrors, parenthesis.Error?.Message);
            Assert.False(keyword.HasErrors, keyword.Error?.Message);
        }

        /// <summary>
        /// Verifies that the lookahead analysis a prompt runs on every key stroke refuses a deep
        /// statement as well instead of recursing.
        /// </summary>
        [Fact]
        public void AnalyzeRefusesDeepStatement()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var input = new string('(', 100_000);

            // act
            var lookahead = parser.Analyze(input);

            // validation
            Assert.False(lookahead.IsValidSoFar);
        }

        /// <summary>
        /// Verifies that a long value is tokenized in linear time; appending to a string copied
        /// the value once per character.
        /// </summary>
        [Fact]
        public void LongValueIsTokenizedInLinearTime()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>() { MaxInputLength = int.MaxValue };
            var input = "text = '" + new string('a', 2_000_000) + "'";
            var watch = Stopwatch.StartNew();

            // act
            var wql = parser.Parse(input);

            // validation
            Assert.False(wql.HasErrors);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
        }

        /// <summary>
        /// Verifies that keywords are recognized independently of the current culture; in Turkish
        /// the lower case of "IN" is "ın" with a dotless i.
        /// </summary>
        [Fact]
        public void KeywordsIgnoreTheCurrentCulture()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var culture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                // act
                var set = parser.Parse("Text IN ('a', 'b')");
                var compare = parser.Parse("Text IS NOT null");

                // validation
                Assert.False(set.HasErrors, set.Error?.Message);
                Assert.False(compare.HasErrors, compare.Error?.Message);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }
    }
}
