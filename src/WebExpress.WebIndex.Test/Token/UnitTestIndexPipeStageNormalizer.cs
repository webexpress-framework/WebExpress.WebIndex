using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageNormalizer : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageNormalizer(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the Normalize method. This function is part of the stemming process and normalize terms.
        /// </summary>
        [Theory]
        [InlineData("résumé", "resume", "en")]
        [InlineData("Mëtàl", "Metal", "en")]
        [InlineData("élégant", "elegant", "en")]
        [InlineData("cliché", "cliche", "en")]
        [InlineData("naïve", "naive", "en")]
        [InlineData("soufflé", "souffle", "en")]
        [InlineData("déjà-vu", "deja vu", "en")]
        [InlineData("tête-à-tête", "tete a tete", "en")]
        [InlineData("São-Paulo", "Sao Paulo", "en")]
        [InlineData("Björk", "Bjork", "en")]
        public void Normalize(string term, string expected, string cultureString)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureString);
            var pipeStage = new IndexPipeStageConverterNormalizer(Fixture.Context);

            // act
            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(term, culture), culture)
                .Select(x => x.Value)
                .ToList();

            Assert.Equal(expected, string.Join(" ", res));
        }
    }
}
