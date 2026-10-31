using WebExpress.WebIndex.Test.Fixture;
using Xunit;
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
        /// Tests that the similarity threshold widens and narrows the result set.
        /// A medium threshold must include similar terms (helena is 50% similar to
        /// helge), while a high threshold must degrade to an exact word search.
        /// </summary>
        [Fact]
        public void FuzzySimilarityThreshold()
        {
            // act
            var fuzzy = Fixture.ExecuteWql("text~'Helge' ~50");
            var fuzzyRes = Fixture.IndexManager.Retrieve(fuzzy);

            var strict = Fixture.ExecuteWql("text~'Helge' ~90");
            var strictRes = Fixture.IndexManager.Retrieve(strict);

            var exact = Fixture.ExecuteWql("text~'Helge'");
            var exactRes = Fixture.IndexManager.Retrieve(exact);

            // validation
            Assert.False(fuzzy.HasErrors);
            Assert.False(strict.HasErrors);
            Assert.Equal(2, exactRes.Count()); // helge only
            Assert.Equal(4, fuzzyRes.Count()); // helge plus the similar helena documents
            Assert.Equal(2, strictRes.Count()); // high threshold behaves like exact search
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

            // validation - the query matches the words hello, helena and helge by similarity
            // instead of the text as a substring; the index finds two documents less, as
            // hello is one of its stop words, which the query does not reproduce
            Assert.NotNull(res); // ensure the result set is not null
            Assert.NotNull(item); // ensure there is at least one result
            Assert.Equal(6, res.Count()); // check if 6 results are returned
            Assert.Equal(
            [
                "0d740124-7707-4b1a-a9f1-3a365a15f60b",
                "3f3d7066-a925-42ac-90f7-ef100afb8460",
                "77251abf-437e-4408-97a7-55545d1622b5",
                "a8901fac-aaef-483b-aba8-dba74e36e7fc",
                "b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a",
                "c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f"
            ], res.Select(x => x.Id.ToString()).Order()); // the documents with a word similar to hel
            Assert.Equal("Text ~ 'Hel' ~50", wql.ToString()); // validate the WQL query string representation
            Assert.NotNull(wql.Filter); // verify that a filter is applied
            Assert.Null(wql.Order); // ensure no explicit ordering is applied
            Assert.Null(wql.Partitioning); // ensure no partitioning is applied
        }
    }
}
