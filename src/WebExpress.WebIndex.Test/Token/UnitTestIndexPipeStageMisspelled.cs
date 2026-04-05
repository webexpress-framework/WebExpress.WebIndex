using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="fixture">The test context.</param>
    /// <param name="output">The log.</param>
    public class UnitTestIndexPipeStageMisspelled(UnitTestIndexFixtureToken fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureToken>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureToken Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the misspelled correction method for individual word pairs.
        /// This function is part of the lemmatization process and corrects commonly misspelled terms.
        /// </summary>
        [Theory]
        [InlineData("febuary", "february")]
        [InlineData("finaly", "finally")]
        [InlineData("flourescent", "fluorescent")]
        [InlineData("foriegn", "foreign")]
        [InlineData("greatful", "grateful")]
        [InlineData("garentee", "guarantee")]
        [InlineData("happend", "happened")]
        [InlineData("independant", "independent")]
        [InlineData("inturrupt", "interrupt")]
        [InlineData("knowlege", "knowledge")]
        [InlineData("libary", "library")]
        [InlineData("noticable", "noticeable")]
        [InlineData("ocassion", "occasion")]
        [InlineData("occured", "occurred")]
        public void Misspelled_En(string input, string expected)
        {
            // precondition
            var culture = CultureInfo.GetCultureInfo("en");
            var pipeStage = new IndexPipeStageConverterMisspelled(Fixture.Context);

            // act
            var tokens = IndexTermTokenizer.Tokenize(input, culture);
            var result = pipeStage.Process(tokens, culture).Select(x => x.Value).ToList();

            // validation
            Assert.Contains(expected, result);
        }

        /// <summary>
        /// Tests the misspelled correction method for individual word pairs.
        /// This function is part of the lemmatization process and corrects commonly misspelled terms.
        /// </summary>
        [Theory]
        [InlineData("andauernd", "andauernd")]
        [InlineData("beschwerden", "beschwerden")]
        [InlineData("balett", "ballett")]
        [InlineData("denenach", "demnach")]
        [InlineData("fahhrad", "fahrrad")]
        [InlineData("managment", "management")]
        [InlineData("muzik", "musik")]
        [InlineData("niderlage", "niederlage")]
        [InlineData("parner", "partner")]
        [InlineData("probem", "problem")]
        [InlineData("quallität", "qualität")]
        [InlineData("rythmus", "rhythmus")]
        [InlineData("willkomenn", "willkommen")]
        [InlineData("zumindestens", "zumindest")]
        public void Misspelled_De(string input, string expected)
        {
            // precondition
            var culture = CultureInfo.GetCultureInfo("de");
            var pipeStage = new IndexPipeStageConverterMisspelled(Fixture.Context);

            // act
            var tokens = IndexTermTokenizer.Tokenize(input, culture);
            var result = pipeStage.Process(tokens, culture).Select(x => x.Value).ToList();

            // validation
            Assert.Contains(expected, result);
        }
    }
}
