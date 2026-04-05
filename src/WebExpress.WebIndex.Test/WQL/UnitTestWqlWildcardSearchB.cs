using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlWildcardSearchB(UnitTestIndexFixtureWqlB fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlB>
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
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterFirst()
        {
            // act
            var wql = Fixture.ExecuteWql("name~'?ame_12'");
            var res = Fixture.IndexManager.Retrieve(wql);
            var item = res?.FirstOrDefault();

            // validation
            Assert.NotNull(res);
            Assert.NotNull(item);
            Assert.Single(res);
            Assert.Equal("Name_12", item.Name);
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterMiddle()
        {
            // arrange
            var wql = Fixture.ExecuteWql("name~'Name_?23'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);

            foreach (var item in res)
            {
                Assert.Matches("Name_.23", item.Name);
            }
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterEnd()
        {
            // arrange
            var wql = Fixture.ExecuteWql("name~'Name_12?'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Matches("Name_12.", item.Name);
            }
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharacters()
        {
            // arrange
            var wql = Fixture.ExecuteWql("name~'Name*'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Matches("Name.*", item.Name);
            }
        }
    }
}
