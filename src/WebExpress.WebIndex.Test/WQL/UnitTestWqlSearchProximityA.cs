using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Proximity search
    /// </summary>
    public class UnitTestWqlSearchProximityA(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
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
        public void ParseValidWql1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :2");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :0");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidEmptyWql()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :");
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidNegativeWql()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :-2");
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWqlOrderBy()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :2 Order by text");
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWqlAnd()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :2 And text = 'Helge' Order by text skip 1");
            Assert.False(wql.HasErrors);

            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);

            // act
            wql = Fixture.ExecuteWql("text~'Helena Helge' :2 & text = 'Helge' Order by text take 10");
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
            var wql = Fixture.ExecuteWql("text~'Helena Helge' :a");
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWqlIn()
        {
            // act
            var wql = Fixture.ExecuteWql("text in ('Helena Helge' :2)");
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Bob Dave':20");
            var res = wql?.Apply();

            Assert.NotNull(res);
            Assert.Equal(2, res.Count());
            Assert.Contains("d3ded093-69be-458b-abeb-7516c970371b", res.Select(x => x.Id.ToString()));
            Assert.Contains("614aac74-13d1-414b-a163-cccce949b3cb", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityMatch2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Dave Bob':20");
            var res = wql?.Apply();

            Assert.NotNull(res);
            Assert.Equal(2, res.Count());
            Assert.Contains("d3ded093-69be-458b-abeb-7516c970371b", res.Select(x => x.Id.ToString()));
            Assert.Contains("614aac74-13d1-414b-a163-cccce949b3cb", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityInvalidMatch1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Bob Dave':1");
            var res = wql?.Apply();

            Assert.NotNull(res);
            Assert.Empty(res);
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityInvalidMatch2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Dave Bob':1");
            var res = wql?.Apply();

            Assert.NotNull(res);
            Assert.Empty(res);
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityPhraseMatch()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Bob Dave':20");
            var res = wql?.Apply();

            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains("Bob", item.Text);
                Assert.Contains("Dave", item.Text);
            }
        }

        /// <summary>
        /// Tests proximity searches, in which two or more terms must appear at a certain distance from each other.
        /// </summary>
        [Fact]
        public void ProximityInvalidPhraseMatch()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Bob Dave':1");
            var res = wql?.Apply();

            Assert.NotNull(res);
            Assert.Empty(res);
        }
    }
}
