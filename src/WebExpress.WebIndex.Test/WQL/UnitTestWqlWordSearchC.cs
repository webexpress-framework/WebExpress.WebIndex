using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Word search
    /// </summary>
    public class UnitTestWqlWordSearchC(UnitTestIndexFixtureWqlC fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlC>
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
        /// Tests word search, which searches for terms in a document regardless of their case or position.
        /// </summary>
        [Fact]
        public void SingleWordQuery()
        {
            // arrange
            var term = Fixture.Term;
            var wql = Fixture.ExecuteWql($"text~'{term}'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();

            // validation
            var res = query.Apply(data.AsQueryable());
            var item = res?.FirstOrDefault();

            Assert.NotNull(res); // ensure the result set is not null
            Assert.NotNull(item); // ensure there is at least one item in the result
            Assert.True(res.Count() >= 1); // verify the total number of matched results
            Assert.Equal($"Text ~ '{term}'", wql.ToString()); // check the string representation of the WQL query
            Assert.Contains(term, item.Text); // ensure the matched item contains "Helena" in its text
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
            var term = Fixture.Term;

            // act
            var wql = Fixture.ExecuteWql($"text~'{term}'");
            var res = wql?.Apply();

            // validation
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.True(res.Count() >= 1);
            Assert.Equal($"Text ~ '{term}'", wql.ToString());
            Assert.Contains(term, item.Text);
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
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();

            // act
            var wql = Fixture.ExecuteWql($"text~'{secondTerm} {term}'");
            var res = wql?.Apply();

            // validation
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.NotEmpty(res);
            Assert.Contains(term, item.Text);
            Assert.Contains(secondTerm, item.Text);
        }
    }
}
