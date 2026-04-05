using System.Globalization;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Test class for testing the storage-based reverse index.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestReverseIndexStorageC(UnitTestIndexFixtureIndexC fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixtureIndexC>(fixture, output)
    {
        /// <summary>
        /// Returns the field.
        /// </summary>
        protected static IndexFieldData Field => new()
        {
            Name = "Text",
            PropertyInfo = typeof(UnitTestIndexTestDocumentC).GetProperty("Text"),
            Type = typeof(UnitTestIndexTestDocumentC)
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
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            Assert.NotNull(reverseIndex);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Adds a token to an existing entry in the reverse index.
        /// </summary>
        [Fact]
        public void AddToken()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            var token = Context.TokenAnalyzer.Analyze("Aurora", CultureInfo.GetCultureInfo("en"));

            // act
            reverseIndex.Add(randomItem, token.TakeLast(1));
            var all = reverseIndex.Retrieve("aurora", new IndexRetrieveOptions());

            Assert.Contains(randomItem.Id, all);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Removes an entry from the reverse index.
        /// </summary>
        [Fact]
        public void Remove()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            var before = reverseIndex.Retrieve(randomItem.Text, new IndexRetrieveOptions()).ToList();
            Assert.NotEmpty(before);

            // act
            reverseIndex.Delete(randomItem);

            var after = reverseIndex.Retrieve(randomItem.Text, new IndexRetrieveOptions()).ToList();
            Assert.True(before.Count - 1 == after.Count);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Removes a token without deleting the entire entry.
        /// </summary>
        [Fact]
        public void RemoveToken()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(randomItem.Text, new IndexRetrieveOptions());
            Assert.True(items.Any());

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
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentC>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var all = reverseIndex.All;

            Assert.Equal(all.OrderBy(x => x), Fixture.TestData.Select(x => x.Id).OrderBy(x => x));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }
    }
}
