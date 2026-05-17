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

            // valdation 
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
            var term = Fixture.RandomItem.Text.Split(' ').Skip(5).FirstOrDefault();
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(20).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}':3");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // valdation 
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
            Assert.True(res.Count() <= Fixture
                .ExecuteWql($"text~'{secondTerm} {term}':12")
                .Apply(document)
                .Count());

        }
    }
}
