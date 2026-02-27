using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlWildcardSearchE(UnitTestIndexFixtureWqlE fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlE>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlE Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterFirst()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'?livia'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("Olivia", item.Name);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterMiddle()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Oli?ia'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("Olivia", item.Name);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterEnd()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Olivi?'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Single(res);
            Assert.Equal("Olivia", item.Name);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharacters()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'Olivi*'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Single(res);
            Assert.Equal("Olivia", item.Name);
        }
    }
}
