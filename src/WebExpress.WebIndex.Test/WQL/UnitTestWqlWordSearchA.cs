using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Word search
    /// </summary>
    public class UnitTestWqlWordSearchA(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
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
            var wql = Fixture.ExecuteWql("text~'Helena'");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~\"Helena\"");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql3()
        {
            // act
            var wql = Fixture.ExecuteWql("text~Helena");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql4()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge'");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql1()
        {
            // act
            var wql = Fixture.ExecuteWql("text~Helena Helge");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql3()
        {
            // act
            var wql = Fixture.ExecuteWql("text~'Helena Helge\" order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql4()
        {
            // act
            var wql = Fixture.ExecuteWql("~'Helena Helge\" order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests word search, which searches for terms in a document regardless of their case or position.
        /// </summary>
        [Fact]
        public void SingleWordQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text~'Helena'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();

            // validation
            var res = query.Apply(data.AsQueryable());
            var item = res?.FirstOrDefault();

            Assert.NotNull(res); // ensure the result set is not null
            Assert.NotNull(item); // ensure there is at least one item in the result
            Assert.Equal(4, res.Count()); // verify the total number of matched results
            Assert.Equal("Text ~ 'Helena'", wql.ToString()); // check the string representation of the WQL query
            Assert.Contains("Helena", item.Text); // ensure the matched item contains "Helena" in its text
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
            var wql = Fixture.ExecuteWql("text~'Helena'");

            // act
            var res = wql?.Apply();

            // validation
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Equal(4, res.Count());
            Assert.Equal("Text ~ 'Helena'", wql.ToString());
            Assert.Contains("Helena", item.Text);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests word search, which searches for terms in a document regardless of their case or position.
        /// </summary>
        [Fact]
        public void MultipleWords()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text~'Helena Helge'");

            // act
            var res = wql?.Apply();

            // validation
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Equal(2, res.Count());
            Assert.Equal("Text ~ 'Helena Helge'", wql.ToString());
            Assert.Contains("Helena", item.Text);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }
    }
}
