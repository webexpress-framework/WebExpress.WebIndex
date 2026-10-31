using System.Globalization;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// A unit test class for analyzing tokens.
    /// </summary>
    public class UnitTestIndexAnalyze : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexAnalyze(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the analysis function of an supported language.
        /// </summary>
        [Theory]
        [InlineData("en", "abc def, ghi jkl mno-pip.", "abc", "def", "ghi", "jkl", "mno", "pip")]
        [InlineData("en", "Be the change that you wish to see in the world. 😊🌸🐼", "alter", "hope", "observe", "world")]
        [InlineData("en", "??? ??")]
        [InlineData("en", "???...&nbsp;")]
        [InlineData("en", "theya??r", "theya")]
        [InlineData("en", "Life is like riding a bicycle. To keep your balance, you must keep moving.", "existence", "enjoy", "riding", "bicycle", "retain", "balance", "retain", "moving")]
        [InlineData("en", "≾≿⊀⊁⊂⊃⊄⊅⊆⊇⊈⊉")]
        [InlineData("en", "★*€¢£¥©░▒▓│┤├")]
        [InlineData("en", "Hello Helena, hello Helge!", "helena", "helge")]
        [InlineData("en", "http://example.com/abc", "http", "example", "com", "abc")]
        public void Token(string culture, string input, params string[] expected)
        {
            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(input, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => x.Value));
        }

        /// <summary>
        /// Tests the analysis function of an supported language.
        /// </summary>
        /// <param name="culture">The language or fallback culture selecting the analysis rules.</param>
        /// <param name="ressource">The embedded sample text used to verify the complete pipeline.</param>
        /// <param name="count">The expected token count after applying the shipped language resources.</param>
        /// <param name="stopWords">The function words that must not survive analysis.</param>
        [Theory]
        [InlineData("en", "JourneyThroughTheUniverse.en", 247, "it", "or", "didn", "also")]
        [InlineData("en", "InterstellarConversations.en", 169, "it", "or")]
        [InlineData("de", "BotanischeBindungenMicrosReiseZuVerdantia.de", 379, "ab", "trotz", "bei", "da", "wenn", "obwohl")]
        [InlineData("de-DE", "BotanischeBindungenMicrosReiseZuVerdantia.de", 379, "ab", "trotz", "bei", "da", "wenn", "obwohl")]
        [InlineData("fr", "BotanischeBindungenMicrosReiseZuVerdantia.de", 715, "also")]
        public void Ressource(string culture, string ressource, int count, params string[] stopWords)
        {
            // arrange
            var input = Fixture.GetRessource(ressource);

            // act
            var tokens = Fixture.TokenAnalyzer.Analyze(input, CultureInfo.GetCultureInfo(culture)).ToArray();

            // validation
            Assert.Equal(count, tokens.Length);
            Assert.DoesNotContain(tokens, token => token.Value is string value && stopWords.Contains(value));
        }
    }
}
