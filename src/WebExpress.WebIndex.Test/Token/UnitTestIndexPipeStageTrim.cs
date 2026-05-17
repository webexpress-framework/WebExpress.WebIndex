using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    public class UnitTestIndexPipeStageTrim : IClassFixture<UnitTestIndexFixtureToken>
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
        public UnitTestIndexPipeStageTrim(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the trim method. This function is part of the stemming process and removes superfluous characters.
        /// </summary>
        [Fact]
        public void Trim()
        {
            var culture = CultureInfo.GetCultureInfo("en");
            var pipeStage = new IndexPipeStageConverterTrim(Fixture.Context);

            (string, string)[] words =
            [
                ("babies_", "babies"),
                ("_cities", "cities"),
                ("countries.", "countries"),
                (".families", "families")
            ];

            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(string.Join(" ", words.Select(x => x.Item1)), culture), culture)
                .Select(x => x.Value)
                .ToList();

            Assert.True(res.Intersect(words.Select(x => x.Item2)).Count() == res.Count);
        }
    }
}
