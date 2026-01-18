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
        public void SingleWordFromQueryable()
        {
            // arrange
            var term = Fixture.Term;

            // act
            var wql = Fixture.ExecuteWql($"text~'{term}'");
            var res = wql?.Apply(Fixture.TestData.AsQueryable());
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
        public void SingleWord()
        {
            // arrange
            var term = Fixture.Term;

            // act
            var wql = Fixture.ExecuteWql($"text~'{term}'");
            var res = wql?.Apply();
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
            var item = res?.FirstOrDefault();

            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.NotEmpty(res);
            Assert.Contains(term, item.Text);
            Assert.Contains(secondTerm, item.Text);
        }
    }
}
