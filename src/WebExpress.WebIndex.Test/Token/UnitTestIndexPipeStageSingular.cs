using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageSingular : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageSingular(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the singular method. This function is part of the lemmatization process and the transformation of the token into the singular.
        /// </summary>
        [Theory]
        // regular nouns (ies, ses  xes, s)
        [InlineData("babies", "baby", "en")]
        [InlineData("countries", "country", "en")]
        [InlineData("families", "family", "en")]
        [InlineData("parties", "party", "en")]
        [InlineData("pennies", "penny", "en")]
        [InlineData("studies", "study", "en")]
        [InlineData("stories", "story", "en")]
        [InlineData("autos", "auto", "de")]
        [InlineData("frauen", "frau", "de")]
        [InlineData("kinder", "kind", "de")]
        [InlineData("tische", "tisch", "de")]
        // irregular nouns
        [InlineData("axes", "axis", "en")]
        [InlineData("indices", "index", "en")]
        [InlineData("selves", "self", "en")]
        [InlineData("vortexes", "vortex", "en")]
        [InlineData("atlanten", "atlas", "de")]
        [InlineData("bücher", "buch", "de")]
        [InlineData("männer", "mann", "de")]
        [InlineData("stühle", "stuhl", "de")]
        public void PluralToSingular(string pluralWord, string singularWord, string cultureString)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureString);
            var pipeStage = new IndexPipeStageConverterSingular(Fixture.Context);

            // act
            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(pluralWord, culture), culture)
                .FirstOrDefault();

            Assert.Equal(singularWord, res?.Value);
        }
    }
}
