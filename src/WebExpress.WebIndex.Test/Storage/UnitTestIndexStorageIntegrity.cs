using System.Globalization;
using System.Reflection;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Test.Storage
{
    /// <summary>
    /// Pins the integrity of the on-disk index: free lists that never hand out an address twice,
    /// one record per document id, balanced value trees and counters that match the postings.
    /// Where the memory index shares the behavior, both index types are checked.
    /// </summary>
    public class UnitTestIndexStorageIntegrity : IDisposable
    {
        private readonly IndexContext _context = new();

        /// <summary>
        /// Initializes a new instance of the class with an index directory of its own.
        /// </summary>
        public UnitTestIndexStorageIntegrity()
        {
            _context.IndexDirectory = Path.Combine(_context.IndexDirectory, Guid.NewGuid().ToString());
        }

        /// <summary>
        /// A deleted item puts its segment on the free list once. Freed twice, the list would
        /// point at itself and hand the same address to the next two items, which then overwrite
        /// each other.
        /// </summary>
        [Fact]
        public void DeleteFreesTheItemSegmentOnce()
        {
            // arrange
            using var store = new IndexStorageDocumentStore<IntegrityDocument>(_context, 5);
            var allocator = (IndexStorageSegmentAllocatorDocumentStore)store.Allocator;
            var alpha = Document("alpha");
            var beta = Document("beta");
            var gamma = Document("gamma");

            store.Add(alpha);

            // act
            store.Delete(alpha);
            var freed = allocator.FreeItemSegments.Count();

            store.Add(beta);
            store.Add(gamma);

            // validation
            Assert.Equal(1, freed);
            Assert.Empty(allocator.FreeItemSegments);
            Assert.Equal("beta", store.GetItem(beta.Id).Text);
            Assert.Equal("gamma", store.GetItem(gamma.Id).Text);
            Assert.Equal(2u, store.Count());
        }

        /// <summary>
        /// An item whose id is already stored is not stored a second time; the document store
        /// keeps the first, as the memory store does.
        /// </summary>
        [Fact]
        public void AddOfAStoredIdKeepsOneRecord()
        {
            // arrange
            using var store = new IndexStorageDocumentStore<IntegrityDocument>(_context, 5);
            var alpha = Document("alpha");

            store.Add(alpha);

            // act
            store.Add(new IntegrityDocument() { Id = alpha.Id, Text = "changed" });

            // validation
            Assert.Equal(1u, store.Count());
            Assert.Equal("alpha", Assert.Single(store.All).Text);
            Assert.Equal("alpha", store.GetItem(alpha.Id).Text);
        }

        /// <summary>
        /// An update replaces the stored data and leaves exactly one record of the id, whose
        /// segments are taken from the free list the delete filled.
        /// </summary>
        [Fact]
        public void UpdateReplacesTheStoredItem()
        {
            // arrange
            using var store = new IndexStorageDocumentStore<IntegrityDocument>(_context, 5);
            var allocator = (IndexStorageSegmentAllocatorDocumentStore)store.Allocator;
            var alpha = Document("alpha");

            store.Add(alpha);

            // act
            store.Update(new IntegrityDocument() { Id = alpha.Id, Text = "changed" });

            // validation
            Assert.Equal(1u, store.Count());
            Assert.Equal("changed", Assert.Single(store.All).Text);
            Assert.Equal("changed", store.GetItem(alpha.Id).Text);
            Assert.Empty(allocator.FreeItemSegments);
        }

        /// <summary>
        /// Items written to reused addresses survive closing and opening the store again, so the
        /// free list and the hash map reach the disk consistent with each other.
        /// </summary>
        [Fact]
        public void ReusedSegmentsSurviveAReopen()
        {
            // arrange
            var alpha = Document("alpha");
            var beta = Document("beta");
            var gamma = Document("gamma");

            using (var store = new IndexStorageDocumentStore<IntegrityDocument>(_context, 5))
            {
                store.Add(alpha);
                store.Add(beta);
                store.Delete(alpha);
                store.Add(gamma);
            }

            // act
            using var reopened = new IndexStorageDocumentStore<IntegrityDocument>(_context, 5);

            // validation
            Assert.Null(reopened.GetItem(alpha.Id));
            Assert.Equal("beta", reopened.GetItem(beta.Id).Text);
            Assert.Equal("gamma", reopened.GetItem(gamma.Id).Text);
            Assert.Equal(2u, reopened.Count());
            Assert.Empty(((IndexStorageSegmentAllocatorDocumentStore)reopened.Allocator).FreeItemSegments);
        }

        /// <summary>
        /// The asynchronous update replaces the stored document as the synchronous one does.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public async Task UpdateAsyncReplacesTheStoredDocument(IndexType type)
        {
            // arrange
            using var manager = CreateManager<IntegrityDocument>(type);
            var alpha = Document("alpha");
            await manager.InsertAsync(alpha);

            // act
            await manager.UpdateAsync(new IntegrityDocument() { Id = alpha.Id, Text = "omega" });

            // validation
            Assert.Equal("omega", Assert.Single(manager.All<IntegrityDocument>()).Text);
            Assert.Equal(alpha.Id, Assert.Single(manager.Retrieve<IntegrityDocument>("Text = 'omega'")).Id);
        }

        /// <summary>
        /// A trailing wildcard finds the term that ends where the wildcard starts, also when
        /// longer terms share it as their prefix.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void TrailingWildcardFindsThePrefixTerm(IndexType type)
        {
            // arrange
            using var manager = CreateManager<IntegrityDocument>(type);
            var alpha = Document("alpha");
            var alphabet = Document("alphabet");
            manager.Insert(alpha);
            manager.Insert(alphabet);

            // act
            var items = manager.Retrieve<IntegrityDocument>("Text ~ 'alpha*'").Select(x => x.Id).ToList();

            // validation
            Assert.Equal(new[] { alpha.Id, alphabet.Id }.Order(), items.Order());
        }

        /// <summary>
        /// The characters before a wildcard are matched by the descent and are not available to
        /// the rest of the pattern again: "al*l*" needs a second l after "al".
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void WildcardDoesNotReuseTheMatchedPrefix(IndexType type)
        {
            // arrange
            using var manager = CreateManager<IntegrityDocument>(type);
            var alpha = Document("alpha");
            var allegro = Document("allegro");
            manager.Insert(alpha);
            manager.Insert(allegro);

            // act
            var items = manager.Retrieve<IntegrityDocument>("Text ~ 'al*l*'").Select(x => x.Id).ToList();

            // validation
            Assert.Equal(allegro.Id, Assert.Single(items));
        }

        /// <summary>
        /// Two properties of the same class type each contribute their nested fields and get a
        /// reverse index of their own, although both fields share one property of that type.
        /// Without the index a query still answers by scanning the document store, which is why
        /// the indexes are checked and not just the results.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void NestedFieldsOfASharedTypeAreIndexed(IndexType type)
        {
            // arrange
            using var manager = CreateManager<PersonDocument>(type);
            var person = new PersonDocument()
            {
                Id = Guid.NewGuid(),
                Home = new AddressPart() { City = "hamburg" },
                Work = new AddressPart() { City = "berlin" }
            };
            manager.Insert(person);

            // act
            var home = manager.Retrieve<PersonDocument>("Home.City = 'hamburg'").ToList();
            var work = manager.Retrieve<PersonDocument>("Work.City = 'berlin'").ToList();

            // validation
            var document = manager.GetIndexDocument<PersonDocument>();
            var homeIndex = document.GetReverseIndex(document.Fields.Single(x => x.Name == "Home.City"));
            var workIndex = document.GetReverseIndex(document.Fields.Single(x => x.Name == "Work.City"));

            Assert.NotNull(homeIndex);
            Assert.NotNull(workIndex);
            Assert.NotSame(homeIndex, workIndex);
            Assert.Equal(person.Id, Assert.Single(home).Id);
            Assert.Equal(person.Id, Assert.Single(work).Id);
        }

        /// <summary>
        /// Values added in ascending order, the worst case for an unbalanced tree, leave the
        /// value tree balanced, and its heights are still valid after a reopen.
        /// </summary>
        [Fact]
        public void NumericValueTreeStaysBalanced()
        {
            // arrange
            const int count = 64;

            using (var manager = CreateManager<IntegrityDocument>(IndexType.Storage))
            {
                for (var i = 1; i <= count; i++)
                {
                    manager.Insert(new IntegrityDocument() { Id = Guid.NewGuid(), Text = "x", Count = i });

                    // a flush between inserts is what the maintenance timer does; a height
                    // changed after it has to be written again
                    NumericIndex(manager).IndexFile.Flush();
                }

                AssertBalanced(NumericRoot(manager));
            }

            // act
            using var reopened = CreateManager<IntegrityDocument>(IndexType.Storage);
            AssertBalanced(NumericRoot(reopened));

            for (var i = count + 1; i <= 2 * count; i++)
            {
                reopened.Insert(new IntegrityDocument() { Id = Guid.NewGuid(), Text = "x", Count = i });
            }

            // validation
            var root = NumericRoot(reopened);
            var height = AssertBalanced(root);

            // an avl tree of n nodes is at most about 1.44 log2(n) high
            Assert.True(height <= 1.45 * Math.Log2(2 * count + 2), $"height {height}");
            Assert.Equal(2 * count, reopened.Retrieve<IntegrityDocument>($"Count > 0").Count());
        }

        /// <summary>
        /// The term index counts a posting once, however often the term occurs in the document,
        /// so deleting the document brings the count back to zero, and a further delete keeps it there.
        /// </summary>
        [Fact]
        public void TermCountMatchesThePostings()
        {
            // arrange
            using var manager = CreateManager<IntegrityDocument>(IndexType.Storage);
            var index = (IndexStorageReverseTerm<IntegrityDocument>)manager
                .GetIndexDocument<IntegrityDocument>()
                .GetReverseIndex(new IndexFieldData(typeof(IntegrityDocument).GetProperty(nameof(IntegrityDocument.Text))));
            var item = Document("gamma gamma");

            // act
            index.Add(item);
            var added = index.Statistic.Count;

            index.Delete(item);
            index.Delete(item);

            // validation
            Assert.Equal(1u, added);
            Assert.Equal(0u, index.Statistic.Count);
        }

        /// <summary>
        /// Walks the value tree and checks that every stored height matches the subtree and no
        /// node leans by more than one level.
        /// </summary>
        /// <param name="node">The subtree root, or null.</param>
        /// <returns>The height of the subtree.</returns>
        private static uint AssertBalanced(IndexStorageSegmentNumericNode node)
        {
            if (node is null)
            {
                return 0;
            }

            var left = AssertBalanced(node.Left);
            var right = AssertBalanced(node.Right);

            Assert.Equal(Math.Max(left, right) + 1, node.Height);
            Assert.InRange((int)left - (int)right, -1, 1);

            return node.Height;
        }

        /// <summary>
        /// Returns the reverse index of the numeric field.
        /// </summary>
        /// <param name="manager">The index manager.</param>
        /// <returns>The reverse index.</returns>
        private static IndexStorageReverseNumeric<IntegrityDocument> NumericIndex(WebIndex.IndexManager manager)
        {
            var field = new IndexFieldData(typeof(IntegrityDocument).GetProperty(nameof(IntegrityDocument.Count)));

            return (IndexStorageReverseNumeric<IntegrityDocument>)manager
                .GetIndexDocument<IntegrityDocument>()
                .GetReverseIndex(field);
        }

        /// <summary>
        /// Returns the root of the value tree of the numeric field.
        /// </summary>
        /// <param name="manager">The index manager.</param>
        /// <returns>The root node.</returns>
        private static IndexStorageSegmentNumericNode NumericRoot(WebIndex.IndexManager manager)
        {
            return NumericIndex(manager).Numeric;
        }

        /// <summary>
        /// Creates a document with a new id and the given text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The document.</returns>
        private static IntegrityDocument Document(string text)
        {
            return new IntegrityDocument() { Id = Guid.NewGuid(), Text = text };
        }

        /// <summary>
        /// Opens an index manager on the test directory with the given document type registered.
        /// </summary>
        /// <typeparam name="TIndexItem">The document type.</typeparam>
        /// <param name="type">The index type.</param>
        /// <returns>The index manager.</returns>
        private WebIndex.IndexManager CreateManager<TIndexItem>(IndexType type)
            where TIndexItem : IIndexItem
        {
            var manager = new IndexManagerTest();
            typeof(IndexManagerTest)
                .GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, [_context]);

            try
            {
                manager.Create<TIndexItem>(CultureInfo.GetCultureInfo("en"), type);
            }
            catch
            {
                manager.Dispose();
                throw;
            }

            return manager;
        }

        /// <summary>
        /// Removes the index directory.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(_context.IndexDirectory))
            {
                Directory.Delete(_context.IndexDirectory, true);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// A document with a text field and a numeric field.
        /// </summary>
        public class IntegrityDocument : IIndexItem
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            [IndexIgnore]
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the text.
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Gets or sets the count.
            /// </summary>
            public long Count { get; set; }
        }

        /// <summary>
        /// A document with two properties of the same class type.
        /// </summary>
        public class PersonDocument : IIndexItem
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            [IndexIgnore]
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the home address.
            /// </summary>
            public AddressPart Home { get; set; }

            /// <summary>
            /// Gets or sets the work address.
            /// </summary>
            public AddressPart Work { get; set; }
        }

        /// <summary>
        /// An address with a city.
        /// </summary>
        public class AddressPart
        {
            /// <summary>
            /// Gets or sets the city.
            /// </summary>
            public string City { get; set; }
        }
    }
}
