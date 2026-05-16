using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.DocumentStore
{
    /// <summary>
    /// Test class for testing the storage-based document store.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestDocumentStoreStorageC : UnitTestDocumentStore<UnitTestIndexFixtureIndexC>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestDocumentStoreStorageC(UnitTestIndexFixtureIndexC fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Creates a document store.
        /// </summary>
        [Fact]
        public void Create()
        {
            // arrange
            Preconditions();

            // act
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);

            // postconditions
            documentStore.Dispose();
        }

        /// <summary>
        /// Adds items to a document store.
        /// </summary>
        [Fact]
        public void Add()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);

            documentStore.Clear();

            // act
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            var i = documentStore.GetItem(Fixture.TestData[0].Id);

            Assert.True(i is not null && i.Id == Fixture.TestData[0].Id);

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Update an entry in the reverse index where the item has a first name change.
        /// </summary>
        [Fact]
        public void UpdateWithChange()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);
            var randomItem = Fixture.RandomItem;

            documentStore.Clear();
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            var text = "Update_" + randomItem.Text;
            var changed = new UnitTestIndexTestDocumentC
            {
                Id = randomItem.Id,
                Text = text,
            };

            // act
            documentStore.Update(changed);

            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Select(x => x.Id).OrderBy(x => x));
            Assert.True(all.Where(x => x.Text == text).Any());

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Changes an entry in the reverse index without the element to be changed having any changes.
        /// </summary>
        [Fact]
        public void UpdateWithoutChanges()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);
            var randomItem = Fixture.RandomItem;

            documentStore.Clear();
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            // act
            documentStore.Update(randomItem);
            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Select(x => x.Id).OrderBy(x => x));
            Assert.True(all.Where(x => x.Text == randomItem.Text).Any());

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Removes an entry from the document store.
        /// </summary>
        [Fact]
        public void Delete()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);

            documentStore.Clear();
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            // act
            documentStore.Delete(Fixture.TestData[0]);
            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Where(x => x.Id != Fixture.TestData[0].Id).Select(x => x.Id).OrderBy(x => x));

            // postconditions
            documentStore.Dispose();
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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);

            documentStore.Clear();
            foreach (var document in Fixture.TestData)
            {
                documentStore.Add(document);
            }

            // act
            var item = documentStore.GetItem(Fixture.TestData[0].Id);

            Assert.NotNull(documentStore);
            Assert.NotNull(item);

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Return all entries of the document store.
        /// </summary>
        [Fact]
        public void All()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentC>(Context, (uint)Fixture.TestData.Count);

            documentStore.Clear();
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            // act
            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Select(x => x.Id).OrderBy(x => x));

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }
    }
}
