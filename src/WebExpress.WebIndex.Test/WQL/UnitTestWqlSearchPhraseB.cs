using WebExpress.WebIndex.Test.Fixture;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Phrase search (exact word sequence)
    /// </summary>
    public class UnitTestWqlSearchPhraseB(UnitTestIndexFixtureWqlB fixture) : IClassFixture<UnitTestIndexFixtureWqlB>
    {
        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlB Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Theory]
        [InlineData("Description='lorem ipsum'")]
        [InlineData("description='lorem ipsum'")]
        [InlineData("description=\"lorem ipsum\"")]
        [InlineData("description=lorem")]
        [InlineData("Address.Street  =  lorem")]
        [InlineData("address.street=lorem")]
        public void ParseValidWql(string wqlString)
        {
            // act
            var wql = Fixture.ExecuteWql(wqlString);

            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Theory]
        [InlineData("description=lorem ipsum")]
        [InlineData("description='lorem ipsum")]
        [InlineData("description='lorem ipsum\"")]
        [InlineData("='lorem ipsum'")]
        [InlineData("Address,Street=lorem")]
        public void ParseInvalidWql(string wqlString)
        {
            // act
            var wql = Fixture.ExecuteWql(wqlString);
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Theory]
        [InlineData("Description='lorem'", "lorem")]
        public void SingleMatch(string wqlString, string expected)
        {
            // act
            var wql = Fixture.ExecuteWql(wqlString);
            var res = wql?.Apply();

            Assert.NotNull(res);
            foreach (var description in res.Select(x => x.Description))
            {
                Assert.Contains(expected, description);
            }
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void MultipleMatch()
        {
            // act
            var wql = Fixture.ExecuteWql("Description='lorem ipsum'");
            var res = wql?.Apply();

            Assert.NotNull(res);
            foreach (var description in res.Select(x => x.Description))
            {
                Assert.Contains("lorem ipsum", description);
            }
        }
    }
}
