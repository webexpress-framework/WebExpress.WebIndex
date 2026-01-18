using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

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
            var term = Fixture.RandomItem.Text.Split(' ').Skip(5).FirstOrDefault();
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(10).FirstOrDefault();

            // act
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}':12");
            var res = wql?.Apply();

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

            // act
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}':3");
            var res = wql?.Apply();

            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
            Assert.True(res.Count() <= Fixture.ExecuteWql($"text~'{secondTerm} {term}':12").Apply().Count());

        }
    }
}
