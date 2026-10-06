using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Proximity search
    /// </summary>
    public class UnitTestWqlSearchProximityC(UnitTestIndexFixtureWqlC fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlC>
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
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch1()
        {
            // arrange
            var randomItem = Fixture.RandomItem;
            var term = randomItem.Text.Split(' ').Skip(5).FirstOrDefault();
            var secondTerm = randomItem.Text.Split(' ').Skip(6).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}':1");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation 
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch2()
        {
            // arrange
            // both terms must lie within the document length (each test document has ten words),
            // otherwise the second term is null and the query degenerates into a single-term search
            var words = Fixture.RandomItem.Text.Split(' ');
            var term = words.Skip(5).FirstOrDefault();
            var secondTerm = words.Skip(8).FirstOrDefault();
            const int distance = 3;
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}':{distance}");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                // proximity matching is order-independent and uses a symmetric +/- distance window,
                // so the two terms must co-occur within that distance rather than as an adjacent phrase
                var tokens = item.Text.Split(' ');
                var termPositions = tokens
                    .Select((word, index) => (word, index))
                    .Where(x => x.word == term)
                    .Select(x => x.index)
                    .ToList();
                var secondTermPositions = tokens
                    .Select((word, index) => (word, index))
                    .Where(x => x.word == secondTerm)
                    .Select(x => x.index)
                    .ToList();

                Assert.Contains(termPositions, p => secondTermPositions.Any(q => Math.Abs(p - q) <= distance));
            }

            // a wider distance window can only ever match the same or more documents
            Assert.True(res.Count() <= Fixture
                .ExecuteWql($"text~'{secondTerm} {term}':12")
                .Apply(document)
                .Count());
        }
    }
}
