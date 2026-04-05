using System.Globalization;
using WebExpress.WebIndex.Memory;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Test class for testing the memory-based reverse index.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestReverseIndexMemoryA(UnitTestIndexFixtureIndexA fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixtureIndexA>(fixture, output)
    {
        /// <summary>
        /// Returns the field.
        /// </summary>
        protected static IndexFieldData Field => new()
        {
            Name = "Text",
            PropertyInfo = typeof(UnitTestIndexTestDocumentA).GetProperty("Text"),
            Type = typeof(UnitTestIndexTestDocumentA)
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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

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
        [Fact]
        public void AddToken()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            var token = Context.TokenAnalyzer.Analyze(randomItem.Text + ", hello Aurora", CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            _ = reverseIndex.Root.Terms;
            var items = reverseIndex.Retrieve("Helena", new IndexRetrieveOptions());

            Assert.NotNull(reverseIndex);
            Assert.Equal(4, items.Count());
            var randomItem = items.Skip(new Random().Next() % items.Count()).FirstOrDefault();

            // act
            reverseIndex.Delete(Fixture.TestData.Where(x => x.Id == randomItem).FirstOrDefault());

            items = reverseIndex.Retrieve("Helena", new IndexRetrieveOptions());
            Assert.Equal(3, items.Count());

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            var token = Context.TokenAnalyzer.Analyze(randomItem.Text + ", hello Aurora", CultureInfo.GetCultureInfo("en"));
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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve("Helena", new IndexRetrieveOptions());

            Assert.NotNull(reverseIndex);
            Assert.Equal(4, items.Count());

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentA>(Context, Field, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
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
