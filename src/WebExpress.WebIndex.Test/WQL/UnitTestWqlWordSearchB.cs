using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Word search
    /// </summary>
    public class UnitTestWqlWordSearchB(UnitTestIndexFixtureWqlB fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlB>
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
        /// Tests word search, which searches for terms in a document regardless of their case or position.
        /// </summary>
        [Fact]
        public void SingleWordQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("name~'Name_12'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();

            // validation
            var res = query.Apply(data.AsQueryable());
            var item = res?.FirstOrDefault();

            Assert.NotNull(res); // ensure the result set is not null
            Assert.NotNull(item); // ensure there is at least one item in the result
            Assert.Equal(1, res.Count()); // verify the total number of matched results
            Assert.Equal("Name ~ 'Name_12'", wql.ToString()); // check the string representation of the WQL query
            Assert.Contains("Name_12", item.Name); // ensure the matched item contains "Helena" in its text
            Assert.NotNull(wql.Filter); // validate that a filter exists in the WQL query
            Assert.Null(wql.Order); // ensure no explicit ordering is applied
            Assert.Null(wql.Partitioning); // ensure no partitioning is present
        }

        /// <summary>
        /// Tests word search, which searches for terms in a document regardless of their case or position.
        /// </summary>
        [Fact]
        public void SingleWord()
        {
            // arrange
            var wql = Fixture.ExecuteWql("name~'Name_12'");

            // act
            var res = wql?.Apply();

            // validation
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Equal(1, res.Count());
            Assert.Contains("Name_12", item.Name);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }
    }
}
