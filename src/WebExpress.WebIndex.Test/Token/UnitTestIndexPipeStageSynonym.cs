using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageSynonym : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageSynonym(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the synonym method. This function is part of the lemmatization process and reduced sysnonyms.
        /// </summary>
        [Theory]
        [InlineData("happy", "joyful", "en")]
        [InlineData("kfz", "auto", "de")]
        public void Synonym(string synonymWord, string normalWord, string cultureString)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureString);
            var pipeStage = new IndexPipeStageConverterSynonym(Fixture.Context);

            // act
            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(synonymWord, culture), culture)
                .FirstOrDefault();

            Assert.Equal(normalWord, res?.Value);
        }
    }
}