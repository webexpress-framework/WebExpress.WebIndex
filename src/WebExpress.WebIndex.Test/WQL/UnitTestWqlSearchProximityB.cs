using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Proximity search
    /// </summary>
    public class UnitTestWqlSearchProximityB(UnitTestIndexFixtureWqlB fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlB>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlB Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Name_12':2");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.NotNull(res);
            Assert.Single(res);
            Assert.Contains("Name_12", res.Select(x => x.Name));
        }
    }
}
