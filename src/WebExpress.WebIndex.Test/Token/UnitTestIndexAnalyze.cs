using System.Globalization;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

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
            // test execution
            var tokens = Fixture.TokenAnalyzer.Analyze(input, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(expected, tokens.Select(x => x.Value));
        }

        /// <summary>
        /// Tests the analysis function of an supported language.
        /// </summary>
        [Theory]
        [InlineData("en", "JourneyThroughTheUniverse.en", 250)]
        [InlineData("en", "InterstellarConversations.en", 169)]
        [InlineData("de", "BotanischeBindungenMicrosReiseZuVerdantia.de", 392)]
        [InlineData("de-DE", "BotanischeBindungenMicrosReiseZuVerdantia.de", 392)]
        [InlineData("fr", "BotanischeBindungenMicrosReiseZuVerdantia.de", 716)]
        public void Ressource(string culture, string ressource, int count)
        {
            // preconditions
            var input = Fixture.GetRessource(ressource);

            // test execution
            var tokens = Fixture.TokenAnalyzer.Analyze(input, CultureInfo.GetCultureInfo(culture));

            Assert.Equal(count, tokens.Count());
            Assert.DoesNotContain(tokens.Select(x => x.Value), new object[] { "it", "or" });
        }
    }
}
