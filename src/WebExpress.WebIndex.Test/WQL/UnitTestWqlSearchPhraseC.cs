using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Phrase search (exact word sequence)
    /// </summary>
    public class UnitTestWqlSearchPhraseC(UnitTestIndexFixtureWqlC fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlC>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlC Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void MultipleMatch1()
        {
            // arrange
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();
            var wql = Fixture.ExecuteWql($"text='{term} {secondTerm}'");

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var text in res.Select(x => x.Text))
            {
                Assert.Contains($"{term} {secondTerm}", text);
            }
        }
    }
}
