using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlSearchFuzzyA(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlA Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~ 80");

            // validation
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWqlOrderBy()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~ 80 Order by text");

            // validation
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWqlAnd1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~ 80 And text = 'Helge' Order by text skip 1");

            // validation
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);

            // act
            wql = Fixture.ExecuteWql("text~'Helena' ~ 80 & text = 'Helge' Order by text take 10");

            // validation
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWqlAnd2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~ 80 & text = 'Helge' Order by text take 10");

            // validation
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~a0");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWqlIn()
        {
            // act
            var wql = Fixture.ExecuteWql("text in ('Helena' ~ 80)");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void Fuzzy()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena' ~50");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Equal(4, res.Count());
            Assert.Equal("Text ~ 'Helena' ~50", wql.ToString());
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void FuzzyQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text~'Hel' ~50");
            var data = Fixture.TestData.AsQueryable();

            // act
            var query = wql.ToQuery();

            // validation
            var res = query.Apply(data);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res); // ensure the result set is not null
            Assert.NotNull(item); // ensure there is at least one result
            Assert.Equal(6, res.Count()); // check if 6 results are returned
            Assert.Equal("Text ~ 'Hel' ~50", wql.ToString()); // validate the WQL query string representation
            Assert.NotNull(wql.Filter); // verify that a filter is applied
            Assert.Null(wql.Order); // ensure no explicit ordering is applied
            Assert.Null(wql.Partitioning); // ensure no partitioning is applied
        }
    }
}
