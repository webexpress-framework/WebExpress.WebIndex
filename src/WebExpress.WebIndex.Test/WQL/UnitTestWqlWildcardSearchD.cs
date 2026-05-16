using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlWildcardSearchD(UnitTestIndexFixtureWqlD fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlD>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlD Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterFirst()
        {
            // act
            var wql = Fixture.ExecuteWql("firstname~'?livia'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("FirstName ~ '?livia'", wql.ToString());
            Assert.Equal("Olivia", item.FirstName);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterMiddle()
        {
            // act
            var wql = Fixture.ExecuteWql("firstname~'Ol?via'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("FirstName ~ 'Ol?via'", wql.ToString());
            Assert.Equal("Olivia", item.FirstName);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterEnd()
        {
            // act
            var wql = Fixture.ExecuteWql("firstname~'Olivi?'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("FirstName ~ 'Olivi?'", wql.ToString());
            Assert.Equal("Olivia", item.FirstName);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharacters()
        {
            // act
            var wql = Fixture.ExecuteWql("firstname~'Olivi*'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("FirstName ~ 'Olivi*'", wql.ToString());
            Assert.Equal("Olivia", item.FirstName);
            Assert.NotNull(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }
    }
}
