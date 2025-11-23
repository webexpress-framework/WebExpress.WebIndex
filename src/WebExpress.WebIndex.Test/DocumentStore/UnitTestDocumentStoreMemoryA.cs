using WebExpress.WebIndex.Memory;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.DocumentStore
{
    /// <summary>
    /// Test class for testing the memory-based document store.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestDocumentStoreMemoryA : UnitTestDocumentStore<UnitTestIndexFixtureIndexA>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestDocumentStoreMemoryA(UnitTestIndexFixtureIndexA fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Creates a document store.
        /// </summary>
        [Fact]
        public void Create()
        {
            // preconditions
            var context = new IndexContext();

            // test execution
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(context, 5);

            // postconditions
            documentStore.Dispose();
        }

        /// <summary>
        /// Adds items to a document store.
        /// </summary>
        [Fact]
        public void Add()
        {
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();

            // test execution
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            var i = documentStore.GetItem(Fixture.TestData[0].Id);

            // validation
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
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            var name = "Update_" + Fixture.TestData[0].Text;
            var changed = new UnitTestIndexTestDocumentA
            {
                Id = Fixture.TestData[0].Id,
                Text = name,
            };

            // test execution
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
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // test execution
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
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // test execution
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
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            documentStore.Add(Fixture.TestData[0]);
            documentStore.Add(Fixture.TestData[1]);

            // test execution
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
            // preconditions
            Preconditions();
            var documentStore = new IndexMemoryDocumentStore<UnitTestIndexTestDocumentA>(Context, 5);

            documentStore.Clear();
            foreach (var item in Fixture.TestData)
            {
                documentStore.Add(item);
            }

            // test execution
            var all = documentStore.All;

            Assert.True(all.Select(x => x.Id).OrderBy(x => x).SequenceEqual(Fixture.TestData.Select(x => x.Id).OrderBy(x => x)));

            // postconditions
            documentStore.Dispose();
            Postconditions();
        }
    }
}
