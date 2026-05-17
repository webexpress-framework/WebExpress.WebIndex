using System.Globalization;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Test class for testing the storage-based reverse index for unicode.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestReverseIndexStorageF(UnitTestIndexFixtureIndexF fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixtureIndexF>(fixture, output)
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
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Adds items to a reverse index.
        /// </summary>
        [Theory]
        [InlineData("ED242C79-E41B-4214-BFBC-C4673E87433B", "Aurora", true)]
        [InlineData("A20BC371-10F9-4F43-9DA8-F4B4F0BE26AB", "李明", true)]
        [InlineData("9733A649-1E5E-4B1F-8C6E-9A4B6AB54292", "🌟🍀🐉", false)]
        [InlineData("80A78EBB-9819-45AF-BC0F-68E68D0C8C1A", "Sun Leaf Lion 🌞🌿🦁", true)]
        [InlineData("29F34DFD-432D-4315-88C2-CE41F293AC71", "🦋🌼🌙 Butterfly Flower Moon", true)]
        public void Add(string id, string name, bool valid)
        {
            // arrange
            Preconditions();
            var doc = new UnitTestIndexTestDocumentF() { Id = Guid.Parse(id), Name = name };
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            // act
            reverseIndex.Add(new UnitTestIndexTestDocumentF() { Id = Guid.Parse(id), Name = name });

            Assert.NotNull(reverseIndex);

            var items = reverseIndex.Retrieve(name, new IndexRetrieveOptions());

            if (valid)
            {
                Assert.Contains(doc.Id, items);
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
        /// Removes a token without deleting the entire entry.
        /// </summary>
        [Theory]
        [InlineData("Aurora")]
        [InlineData("张伟")]
        public void Remove(string str)
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.TestData?.LastOrDefault();
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));

            foreach (var item in Fixture.TestData)
            {
                reverseIndex.Add(item);
            }

            var token = Context.TokenAnalyzer.Analyze(str, CultureInfo.GetCultureInfo("en"));
            reverseIndex.Add(randomItem, token.TakeLast(1));

            // act
            reverseIndex.Delete(randomItem, token.TakeLast(1));

            var items = reverseIndex.Retrieve(str, new IndexRetrieveOptions());
            Assert.Empty(items);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Retrieve a entry of the reverse index.
        /// </summary>
        [Theory]
        [InlineData("张伟", IndexRetrieveMethod.Phrase)]
        public void Retrieve(string str, IndexRetrieveMethod method)
        {
            // arrange
            Preconditions();
            var reverseIndex = new IndexStorageReverseTerm<UnitTestIndexTestDocumentF>(Context, Field, CultureInfo.GetCultureInfo("en"));
            var option = new IndexRetrieveOptions() { Method = method };

            foreach (var item in Fixture.TestData)
            {
                // act
                reverseIndex.Add(item);
            }

            // act
            var items = reverseIndex.Retrieve(str, option);

            Assert.NotEmpty(items);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }
    }
}
