using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Wildcard search
    /// </summary>
    public class UnitTestWqlWildcardSearchC(UnitTestIndexFixtureWqlC fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlC>
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
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterFirst()
        {
            // arrange
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{term} {string.Concat("?", secondTerm.AsSpan(1))}'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterMiddle()
        {
            // arrange
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{term} {string.Concat(secondTerm.AsSpan(0, 2), "?", secondTerm.AsSpan(1))}'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void SingleCharacterEnd()
        {
            // arrange
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{term} {string.Concat(secondTerm[..^1], "?")}'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
        }

        /// <summary>
        /// Tests the wildcard search.
        /// </summary>
        [Fact]
        public void MultipleCharacters()
        {
            // arrange
            var term = Fixture.Term;
            var secondTerm = Fixture.RandomItem.Text.Split(' ').Skip(1).FirstOrDefault();
            var wql = Fixture.ExecuteWql($"text~'{term} {string.Concat(secondTerm[..^1], "*")}'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            foreach (var item in res)
            {
                Assert.Contains($"{term} {secondTerm}", item.Text);
            }
        }
    }
}
