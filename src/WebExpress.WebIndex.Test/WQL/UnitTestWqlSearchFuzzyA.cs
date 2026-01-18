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
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);

            wql = Fixture.ExecuteWql("text~'Helena' ~ 80 & text = 'Helge' Order by text take 10");
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
            var res = wql?.Apply();
            var item = res?.FirstOrDefault();

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
        public void FuzzyFromQueryable()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Hel' ~50");
            var res = wql?.Apply(Fixture.TestData.AsQueryable());
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Equal(6, res.Count());
            Assert.Equal("Text ~ 'Hel' ~50", wql.ToString());
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }
    }
}
