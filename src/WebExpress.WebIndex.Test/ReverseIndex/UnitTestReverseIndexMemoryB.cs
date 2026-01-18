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
    public class UnitTestReverseIndexMemoryB(UnitTestIndexFixtureIndexB fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixtureIndexB>(fixture, output)
    {
        /// <summary>
        /// Returns the field for the mane.
        /// </summary>
        protected static IndexFieldData FieldName => new()
        {
            Name = "Name",
            PropertyInfo = typeof(UnitTestIndexTestDocumentB).GetProperty("Name"),
            Type = typeof(UnitTestIndexTestDocumentB)
        };

        /// <summary>
        /// Returns the field for the price.
        /// </summary>
        protected static IndexFieldData FieldPrice => new IndexFieldData()
        {
            Name = "Price",
            PropertyInfo = typeof(UnitTestIndexTestDocumentA).GetProperty("Price"),
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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            var before = reverseIndex.Retrieve(randomItem.Name, new IndexRetrieveOptions()).ToList();
            Assert.NotEmpty(before);

            // act
            reverseIndex.Delete(randomItem);

            var after = reverseIndex.Retrieve(randomItem.Name, new IndexRetrieveOptions()).ToList();
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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

            reverseIndex.Clear();
            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(randomItem.Name, new IndexRetrieveOptions());
            Assert.True(items.Any());

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Tests numeric equals.
        /// </summary>
        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 1)]
        [InlineData(10, 1)]
        [InlineData(50, 1)]
        [InlineData(50.5, 0)]
        [InlineData(90, 1)]
        [InlineData(100, 0)]
        public void NumericEquals(decimal number, int expected)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseNumeric<UnitTestIndexTestDocumentB>(Context, FieldPrice, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(number, new IndexRetrieveOptions() { Method = IndexRetrieveMethod.Phrase });
            var prices = Fixture.TestData.Where(x => items.Contains(x.Id)).Select(x => x.Price).ToList();

            Assert.NotNull(items);
            Assert.Equal(Fixture.TestData.Where(x => x.Price == (double)number).Select(x => x.Price).ToList(), prices);
            Assert.Equal(expected, items.Count());

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Tests numeric greater than.
        /// </summary>
        [Theory]
        [InlineData(-10, 100)]
        [InlineData(0, 99)]
        [InlineData(10, 89)]
        [InlineData(50, 49)]
        [InlineData(50.5, 49)]
        [InlineData(90, 9)]
        [InlineData(100, 0)]
        public void NumericGreaterThan(decimal number, int expected)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseNumeric<UnitTestIndexTestDocumentB>(Context, FieldPrice, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(number, new IndexRetrieveOptions() { Method = IndexRetrieveMethod.GratherThan });
            var prices = Fixture.TestData.Where(x => items.Contains(x.Id)).Select(x => x.Price).ToList();

            Assert.NotNull(items);
            Assert.Equal(Fixture.TestData.Where(x => x.Price > (double)number).Select(x => x.Price).ToList(), prices);
            Assert.Equal(expected, items.Count());

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Tests numeric greater than or equals.
        /// </summary>
        [Theory]
        [InlineData(-10, 100)]
        [InlineData(0, 100)]
        [InlineData(10, 90)]
        [InlineData(50, 50)]
        [InlineData(50.5, 49)]
        [InlineData(90, 10)]
        [InlineData(100, 0)]
        public void NumericGreaterThanOrEquals(decimal number, int expected)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseNumeric<UnitTestIndexTestDocumentB>(Context, FieldPrice, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(number, new IndexRetrieveOptions() { Method = IndexRetrieveMethod.GratherThanOrEqual });
            var prices = Fixture.TestData.Where(x => items.Contains(x.Id)).Select(x => x.Price).ToList();

            Assert.NotNull(items);
            Assert.Equal(Fixture.TestData.Where(x => x.Price >= (double)number).Select(x => x.Price).ToList(), prices);
            Assert.Equal(expected, items.Count());

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Tests numeric less than or equals.
        /// </summary>
        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 0)]
        [InlineData(10, 10)]
        [InlineData(50, 50)]
        [InlineData(50.5, 51)]
        [InlineData(90, 90)]
        [InlineData(100, 100)]
        public void NumericLessThan(decimal number, int expected)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseNumeric<UnitTestIndexTestDocumentB>(Context, FieldPrice, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(number, new IndexRetrieveOptions() { Method = IndexRetrieveMethod.LessThan });
            var prices = Fixture.TestData.Where(x => items.Contains(x.Id)).Select(x => x.Price).ToList();

            Assert.NotNull(items);
            Assert.Equal(Fixture.TestData.Where(x => x.Price < (double)number).Select(x => x.Price).ToList(), prices);
            Assert.Equal(expected, items.Count());

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Tests numeric less than or equals.
        /// </summary>
        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 1)]
        [InlineData(10, 11)]
        [InlineData(50, 51)]
        [InlineData(50.5, 51)]
        [InlineData(90, 91)]
        [InlineData(100, 100)]
        public void NumericLessThanOrEquals(decimal number, int expected)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexMemoryReverseNumeric<UnitTestIndexTestDocumentB>(Context, FieldPrice, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(number, new IndexRetrieveOptions() { Method = IndexRetrieveMethod.LessThanOrEqual });
            var prices = Fixture.TestData.Where(x => items.Contains(x.Id)).Select(x => x.Price).ToList();

            Assert.NotNull(items);
            Assert.Equal(Fixture.TestData.Where(x => x.Price <= (double)number).Select(x => x.Price).ToList(), prices);
            Assert.Equal(expected, items.Count());

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
            var reverseIndex = new IndexMemoryReverseTerm<UnitTestIndexTestDocumentB>(Context, FieldName, CultureInfo.GetCultureInfo("en"));

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

