using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageStopWord : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageStopWord(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the stop word method. This function is part of the stemming process and removes stop words.
        /// </summary>
        [InlineData
        (
            "en",
            "May the force be with you.",
            "may", "the", "be", "with", "you"
        )]
        [InlineData
        (
            "en",
            "Would you like tea or coffee?",
            "would", "you", "like", "or", "coffee"
        )]
        [InlineData
        (
            "en",
            "If it rains tomorrow, we will stay indoors.",
            "if", "it", "we", "will", "stay"
        )]
        [Theory]
        [InlineData
        (
            "de",
            "Als Gregor Samsa eines Morgens aus unruhigen Träumen erwachte, fand er sich in seinem Bett zu einem ungeheueren Ungeziefer verwandelt.",
            "als", "eines", "aus", "er", "sich", "in", "seinem", "zu", "einem"
        )]
        [InlineData
        (
            "en",
            "😊🌸🐼",
            null
        )]
        public void StopWord(string cultureStr, string str, params string[] tokenStr)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureStr);
            var pipeStage = new IndexPipeStageFilterStopWord(Fixture.Context);

            var token = IndexTermTokenizer.Tokenize(str.ToLower(), culture);

            // act
            var res = pipeStage.Process(token, culture)
                .Select(x => x.Value)
                .ToList();

            Assert.DoesNotContain(tokenStr, res);
        }
    }
}
