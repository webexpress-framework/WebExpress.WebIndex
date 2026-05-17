using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Phrase search (exact word sequence)
    /// </summary>
    public class UnitTestWqlSearchPhraseD(UnitTestIndexFixtureWqlD fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlD>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlD Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void MultipleMatch1()
        {
            // arrange
            var wql = Fixture.ExecuteWql("description='lorem ipsum'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentD>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var description in res.Select(x => x.Description))
            {
                Assert.Contains("lorem ipsum", description);
            }
        }
    }
}
