using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageSurrogateCharacter : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageSurrogateCharacter(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the surrogate character method. This function is part of the stemming process and removes words with surrogate characters.
        /// </summary>
        [Fact]
        public void Surrogate()
        {
            var culture = CultureInfo.GetCultureInfo("en");
            var pipeStage = new IndexPipeStageFilterSurrogateCharacter(Fixture.Context);

            var chars = new char[] { '\uD800', '\uDC00' }; // this is a surrogate pair
            var token = IndexTermTokenizer.Tokenize($"a surrogate pair like this '{new string(chars)}' must be removed.", culture);

            var res = pipeStage.Process(token, culture)
                .Select(x => x.Value)
                .ToList();

            Assert.DoesNotContain(new string(chars), res);

            Assert.True(token.Count() - 1 == res.Count);
        }
    }
}
