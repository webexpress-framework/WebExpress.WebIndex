using System.Globalization;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// Unit tests for analyzing numbers in different cultures.
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{WebExpress.WebIndex.Test.Fixture.UnitTestIndexFixtureToken}" />
    public class UnitTestIndexAnalyzeNumber : IClassFixture<UnitTestIndexFixtureToken>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; }

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureToken Fixture { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The test context.</param>
        /// <param name="output">The log.</param>
        public UnitTestIndexAnalyzeNumber(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the number input.
        /// </summary>
        [Theory]
        [InlineData("1", 1, "en")]
        [InlineData("1", 1, "de")]
        [InlineData("-1", -1, "en")]
        [InlineData("-1", -1, "de")]
        public void Number(string term, double expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, (double)tokens.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Tests the invalid number input.
        /// </summary>
        [Theory]
        [InlineData("-st", "st", "en")]
        [InlineData("-st", "st", "de")]
        [InlineData("10b0", "10 b0", "en")]
        public void InvalidNumber(string term, string expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, string.Join(" ", tokens.Select(x => x.Value)));
        }

        /// <summary>
        /// Tests the double input.
        /// </summary>
        [Theory]
        [InlineData("10038.76", 10038.76, "en")]
        [InlineData("1,0038.76", 10038.76, "en")]
        [InlineData("10038,76", 10038.76, "de")]
        [InlineData("1.0038,76", 10038.76, "de")]
        [InlineData("-1,0038.76", -10038.76, "en")]
        [InlineData("-1.0038,76", -10038.76, "de")]
        public void Double(string term, double expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, (double)tokens.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Tests the number input.
        /// </summary>
        [Theory]
        [InlineData("1.0038,76", new double[] { 1.0038, 76 }, "en")]
        [InlineData("1,0038.76", new double[] { 1.0038, 76 }, "de")]
        public void InvalidDouble(string term, double[] expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => (double)x.Value));
        }

        /// <summary>
        /// Tests the number with exponent input.
        /// </summary>
        [Theory]
        [InlineData("1.0038e76", 1.0038e76, "en")]
        [InlineData("1.0038E76", 1.0038e76, "en")]
        [InlineData("1.0038e+76", 1.0038e76, "en")]
        [InlineData("1.0038E+76", 1.0038e76, "en")]
        [InlineData("1.0038e-76", 1.0038e-76, "en")]
        [InlineData("1.0038E-76", 1.0038e-76, "en")]
        [InlineData("1,0038e76", 1.0038e76, "de")]
        [InlineData("+1.0038e76", 1.0038e76, "en")]
        [InlineData("+1,0038e76", 1.0038e76, "de")]
        [InlineData("-1.0038e76", -1.0038e76, "en")]
        [InlineData("-1,0038e76", -1.0038e76, "de")]
        public void Exponent(string term, double expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, (double)tokens.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Tests the number with exponent input.
        /// </summary>
        [Theory]
        [InlineData("∞", double.PositiveInfinity, "en")]
        [InlineData("-∞", double.NegativeInfinity, "en")]
        [InlineData("∞", double.PositiveInfinity, "de")]
        [InlineData("-∞", double.NegativeInfinity, "de")]
        public void Infinity(string term, double expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, (double)tokens.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Tests the add with input.
        /// </summary>
        [Theory]
        [InlineData("2+3", new double[] { 2, 3 }, "en")]
        [InlineData("2 + 3", new double[] { 2, 3 }, "en")]
        [InlineData("2+3", new double[] { 2, 3 }, "de")]
        [InlineData("2 + 3", new double[] { 2, 3 }, "de")]
        public void Add(string term, double[] expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => (double)x.Value));
        }

        /// <summary>
        /// Tests the minus with input.
        /// </summary>
        [Theory]
        [InlineData("2-3", new double[] { 2, 3 }, "en")]
        [InlineData("2 - 3", new double[] { 2, 3 }, "en")]
        [InlineData("2-3", new double[] { 2, 3 }, "de")]
        [InlineData("2 - 3", new double[] { 2, 3 }, "de")]
        public void Minus(string term, double[] expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => (double)x.Value));
        }

        /// <summary>
        /// Tests the power with input.
        /// </summary>
        [Theory]
        [InlineData("2^3", new double[] { 2, 3 }, "en")]
        [InlineData("2 ^ 3", new double[] { 2, 3 }, "en")]
        [InlineData("2^3", new double[] { 2, 3 }, "de")]
        [InlineData("2 ^ 3", new double[] { 2, 3 }, "de")]
        public void Power(string term, double[] expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => (double)x.Value));
        }

        /// <summary>
        /// Tests the text_number input.
        /// </summary>
        [Theory]
        [InlineData("N1", "n1", "en")]
        [InlineData("N1", "n1", "de")]
        public void TextWithNumber(string term, string expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Tests the text_number with wildcatd input.
        /// </summary>
        [Theory]
        [InlineData("Name?23", "name?23", "en")]
        [InlineData("Name?23", "name?23", "de")]
        public void NumberWithWildcard(string term, string expected, string culture)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(term, CultureInfo.GetCultureInfo(culture), true);

            Assert.Equal(expected, tokens.FirstOrDefault()?.Value);
        }
    }
}
