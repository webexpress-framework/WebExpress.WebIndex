using System.Globalization;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.IndexManager
{
    /// <summary>
    /// Test class for testing the storage-based index manager.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the class.
    /// </remarks>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    public class UnitTestIndexManagerStorageB(UnitTestIndexFixtureIndexB fixture, ITestOutputHelper output)
        : UnitTestIndexManager<UnitTestIndexFixtureIndexB>(fixture, output)
    {
        /// <summary>
        /// Tests registering a document in the index manager.
        /// </summary>
        [Fact]
        public void Create()
        {
            // arrange
            Preconditions();

            // act
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // validation
            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>());

            Postconditions();
        }

        /// <summary>
        /// Tests the reindex function from the index manager.
        /// </summary>
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("de-DE")]
        [InlineData("fr")]
        public void ReIndex(string culture)
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            IndexManager.ReIndex(Fixture.TestData);

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            Assert.NotEmpty(items);

            Postconditions();
        }

        /// <summary>
        /// Tests the reindex function from the index manager.
        /// </summary>
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("de-DE")]
        [InlineData("fr")]
        public async Task ReIndexAsync(string culture)
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(item);
            Assert.NotEmpty(item);
            Assert.Equal(item.FirstOrDefault().Name, randomItem.Name);

            Postconditions();
        }

        /// <summary>
        /// Ensures cancellation stops reindexing while retaining completed items.
        /// </summary>
        [Fact]
        public async Task ReIndexAsyncCancel()
        {
            // arrange
            Preconditions();
            var firstItem = Fixture.TestData.First();
            var lastItem = Fixture.TestData.Last();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            using var cancellationTokenSource = new CancellationTokenSource();

            // cancellation must occur before the next item rather than in a queued callback
            var progress = new SynchronousProgress(percent =>
            {
                if (percent > 30)
                {
                    cancellationTokenSource.Cancel();
                }
            });

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData, progress, cancellationTokenSource.Token);

            // validation
            Assert.True(cancellationTokenSource.IsCancellationRequested);
            Assert.Single(IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{firstItem.Name}'"));
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{lastItem.Name}'");
            Assert.NotNull(items);
            Assert.Empty(items);

            Postconditions();
        }

        /// <summary>
        /// Tests the removal of a document from the index manager.
        /// </summary>
        [Fact]
        public void Delete()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var before = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(before);
            Assert.NotEmpty(before);

            // act
            IndexManager.Delete(randomItem);

            // validation
            var after = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(after);
            Assert.Equal(before.Count() - 1, after.Count());

            Postconditions();
        }

        /// <summary>
        /// Tests the add function of the index manager.
        /// </summary>
        [Fact]
        public void Add()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentB()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Name = "Hello Aurora!"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            Assert.NotNull(items);
            Assert.Single(items);

            Postconditions();
        }

        /// <summary>
        /// Tests the update function of the index manager.
        /// </summary>
        [Fact]
        public void Update()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Update(new UnitTestIndexTestDocumentB()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            Assert.NotNull(items);
            Assert.Single(items);

            Postconditions();
        }

        /// <summary>
        /// Tests the update function of the index manager.
        /// </summary>
        [Fact]
        public async Task UpdateAsync()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentB()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            Assert.NotNull(items);
            Assert.Single(items);

            Postconditions();
        }

        /// <summary>
        /// Tests removing a document on the index manager.
        /// </summary>
        [Fact]
        public void Clear()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentB>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // act
            IndexManager.Clear<UnitTestIndexTestDocumentB>();

            // validation
            documents = IndexManager.All<UnitTestIndexTestDocumentB>();

            Assert.NotNull(documents);
            Assert.Empty(documents);

            IndexManager.Close<UnitTestIndexTestDocumentB>();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            Assert.Empty(IndexManager.All<UnitTestIndexTestDocumentB>());

            var replacement = Fixture.TestData[0];
            IndexManager.Insert(replacement);
            Assert.Equal(replacement.Id, Assert.Single(IndexManager.All<UnitTestIndexTestDocumentB>()).Id);

            IndexManager.Close<UnitTestIndexTestDocumentB>();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            Assert.Equal(replacement.Id, Assert.Single(IndexManager.All<UnitTestIndexTestDocumentB>()).Id);

            Postconditions();
        }

        /// <summary>
        /// Return all entries of the index manager.
        /// </summary>
        [Fact]
        public void All()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var all = IndexManager.All<UnitTestIndexTestDocumentB>();

            // validation
            Assert.True(all.Select(x => x.Id).OrderBy(x => x).SequenceEqual(Fixture.TestData.Select(x => x.Id).OrderBy(x => x)));

            Postconditions();
        }

        /// <summary>
        /// Tests get a document from the index manager.
        /// </summary>
        [Fact]
        public void GetDocument()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>();

            // validation
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentB>));

            Postconditions();
        }

        /// <summary>
        /// Tests get a document from the index manager.
        /// </summary>
        [Fact]
        public void GetDocument_Not()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>();

            // validation
            Assert.Null(document);

            Postconditions();
        }

        /// <summary>
        /// Tests the retrieve function in a series of tests from the index manager.
        /// </summary>
        [Theory]
        [InlineData("name = 'Name_3'", "en", "Name_3")]
        [InlineData("Summary = 'Name_3'", "en", "Name_3")]
        [InlineData("Price = 3", "en", "Name_3")]
        [InlineData("Price = '3'", "en", "Name_3")]
        [InlineData("Address.Street = 3", "en", "Name_3")]
        [InlineData("Address.Country = usa", "en", "Name_3")]
        public void Retrieve(string wqlString, string cultureString, string expected)
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(cultureString), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>(wqlString);

            // validation
            Assert.NotNull(items);
            Assert.Contains(expected, items.Select(x => x.Name.ToString()));

            Postconditions();
        }

        /// <summary>
        /// Tests the close and open function from the index manager.
        /// </summary>
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("de-DE")]
        [InlineData("fr")]
        public void ReOpen(string culture)
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            var count = items.Count();

            // act
            IndexManager.Close<UnitTestIndexTestDocumentB>();

            // validation
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            items = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            Assert.Equal(count, items.Count());

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Keeps progress-triggered cancellation independent of synchronization context scheduling.
        /// </summary>
        /// <param name="handler">The callback to complete before indexing can continue.</param>
        private sealed class SynchronousProgress(Action<int> handler) : IProgress<int>
        {
            /// <summary>
            /// Runs the callback inline so cancellation is observed before the next item.
            /// </summary>
            /// <param name="value">The completed indexing percentage.</param>
            public void Report(int value)
            {
                handler(value);
            }
        }
    }
}
