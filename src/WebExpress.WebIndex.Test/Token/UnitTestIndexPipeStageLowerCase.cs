using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// Unit tests for the IndexPipeStageLowerCase class.
    /// </summary>
    /// <remarks>
    /// This class tests the functionality of converting terms to lower case as part of the stemming process.
    /// </remarks>
    public class UnitTestIndexPipeStageLowerCase : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageLowerCase(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the lower case method. This function is part of the stemming process and convert characters in lower case.
        /// </summary>
        [Theory]
        [InlineData("Babies", "babies", "en")]
        [InlineData("Cities", "cities", "en")]
        [InlineData("Countries", "countries", "en")]
        [InlineData("Families", "families", "en")]
        public void LowerCase(string term, string normalizeTerm, string cultureString)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureString);
            var pipeStage = new IndexPipeStageConverterLowerCase(Fixture.Context);

            // act
            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(term, culture), culture)
                .FirstOrDefault();

            Assert.Equal(normalizeTerm, res.Value);
        }
    }
}
