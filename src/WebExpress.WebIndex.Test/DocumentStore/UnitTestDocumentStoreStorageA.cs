using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.DocumentStore
{
    /// <summary>
    /// Test class for testing the storage-based document store.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestDocumentStoreStorageA : UnitTestDocumentStore<UnitTestIndexFixtureIndexA>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestDocumentStoreStorageA(UnitTestIndexFixtureIndexA fixture, ITestOutputHelper output)
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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            // act
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            var name = "Update_" + Fixture.TestData[0].Text;
            var changed = new UnitTestIndexTestDocumentA
            {
                Id = Fixture.TestData[0].Id,
                Text = name,
            };

            // act
            documentStore.Update(changed);

            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Take(2).Select(x => x.Id).OrderBy(x => x));
            Assert.True(all.Where(x => x.Text == name).Any());

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // act
            documentStore.Update(Fixture.TestData[0]);
            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Take(2).Select(x => x.Id).OrderBy(x => x));
            Assert.True(all.Where(x => x.Text == Fixture.TestData[0].Text).Any());

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // act
            documentStore.Delete(Fixture.TestData[0]);
            var all = documentStore.All;

            Assert.Equal
            (
                all.Select(x => x.Id).OrderBy(x => x),
                Fixture.TestData
                    .Where(x => x.Id == Fixture.TestData[1].Id)
                    .Select(x => x.Id)
            );

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

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
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // act
            var all = documentStore.All;

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Take(2).Select(x => x.Id).OrderBy(x => x));

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Clear the document store.
        /// </summary>
        [Fact]
        public void Clear()
        {
            // arrange
            Preconditions();
            var documentStore = new IndexStorageDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);
            documentStore.Add(Fixture.TestData[2]);
            documentStore.Add(Fixture.TestData[3]);

            // act
            documentStore.Clear();
            Assert.Empty(documentStore.All);

            documentStore.Add(Fixture.TestData[0]);

            documentStore.Add(Fixture.TestData[1]);

            var all = documentStore.All.ToList();

            Assert.Equal(all.Select(x => x.Id).OrderBy(x => x), Fixture.TestData.Take(2).Select(x => x.Id).OrderBy(x => x));

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }
    }
}
