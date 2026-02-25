using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Proximity search
    /// </summary>
    public class UnitTestWqlSearchProximityE(UnitTestIndexFixtureWqlE fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlE>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlE Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch1()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Olivia':6");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.NotNull(res);
            Assert.Equal(1, res.Count());
            Assert.Contains("d50774b3-5d95-4fb4-97fb-d107dd6fb9a0", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch2()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Olivia':0");
            var res = Fixture.IndexManager.Retrieve(wql);

            // validation
            Assert.NotNull(res);
            Assert.NotNull(res);
            Assert.Equal(1, res.Count());
            Assert.Contains("d50774b3-5d95-4fb4-97fb-d107dd6fb9a0", res.Select(x => x.Id.ToString()));
        }
    }
}
