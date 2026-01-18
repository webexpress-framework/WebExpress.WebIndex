using System.Globalization;
using WebExpress.WebIndex.Memory;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Test class for testing the memory-based reverse index for unicode.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestReverseIndexMemoryF(UnitTestIndexFixtureIndexF fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixtureIndexF>(fixture, output)
    {
        /// <summary>
        /// Returns the field.
        /// </summary>
        protected static IndexFieldData Field => new()
        {
            Name = "Name",
            PropertyInfo = typeof(UnitTestIndexTestDocumentF).GetProperty("Name"),
            Type = typeof(UnitTestIndexTestDocumentF)
        };

        /// <summary>
        /// Creates a reverse index.
        /// </summary>
        [Fact]
        public void Create()
        {
            // arrange
            Preconditions();

            // act
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Adds items to a reverse index.
        /// </summary>
        [Fact]
        public void Add()
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            Assert.NotNull(reverseIndex);

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Adds a token to an existing entry in the reverse index.
        /// </summary>
        [Theory]
        [InlineData("Aurora", true)]
        [InlineData("😊🌸🐼", false)]
        [InlineData("张伟", true)]
        public void AddToken(string str, bool valid)
        {
            // arrange
            Preconditions();
            var doc = new UnitTestIndexTestDocumentF() { Id = Guid.Parse("9A274C29-E210-49C9-A673-238F79636CD9"), Name = str };
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));
            var token = Context.TokenAnalyzer.Analyze(str, CultureInfo.GetCultureInfo("en"));

            // act
            reverseIndex.Add(doc, token);
            var item = reverseIndex.Retrieve(str, new IndexRetrieveOptions());

            if (valid)
            {
                Assert.Contains(doc.Id, item);
            }
            else
            {
                Assert.Empty(item);
            }

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Removes a token without deleting the entire entry.
        /// </summary>
        [Fact]
        public void Remove()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            var token = Context.TokenAnalyzer.Analyze("Aurora", CultureInfo.GetCultureInfo("en"));
            reverseIndex.Add(randomItem, token.TakeLast(1));

            // act
            reverseIndex.Delete(randomItem, token.TakeLast(1));

            var items = reverseIndex.Retrieve("aurora", new IndexRetrieveOptions());
            Assert.Empty(items);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Retrieve a entry of the reverse index.
        /// </summary>
        [Fact]
        public void Retrieve()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(randomItem.Name, new IndexRetrieveOptions());

            if (randomItem.Name != "😊🌸🐼")
            {
                Assert.True(items.Any());
            }
            else
            {
                Assert.Empty(items);
            }

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Return all entries of the reverse index.
        /// </summary>
        [Fact]
        public void All()
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var all = reverseIndex.All;

            Assert.Equal(all.OrderBy(x => x), Fixture.TestData.Where(x => x.Name != "😊🌸🐼").Select(x => x.Id).OrderBy(x => x));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }
    }
}
