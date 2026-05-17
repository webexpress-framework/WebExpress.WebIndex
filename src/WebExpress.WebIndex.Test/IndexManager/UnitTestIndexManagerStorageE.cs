using System.Globalization;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.IndexManager
{
    /// <summary>
    /// Test class for testing the storage-based index manager.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestIndexManagerStorageE : UnitTestIndexManager<UnitTestIndexFixtureIndexE>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestIndexManagerStorageE(UnitTestIndexFixtureIndexE fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests registering a document in the index manager.
        /// </summary>
        [Fact]
        public void Create()
        {
            // arrange
            Preconditions();

            // act
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // validation
            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentE>());

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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            IndexManager.ReIndex(Fixture.TestData);

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            Assert.NotEmpty(items);

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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var before = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
            Assert.NotNull(before);
            Assert.True(before.Any());

            // act
            IndexManager.Delete(randomItem);

            // validation
            var after = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentE()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Name = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>("name = 'Aurora'");
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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Update(new UnitTestIndexTestDocumentE()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>("name = 'Aurora'");
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
            var randomItem = Fixture.TestData.LastOrDefault();
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentE()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>("name = 'Aurora'");
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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentE>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // act
            IndexManager.Clear<UnitTestIndexTestDocumentE>();

            // validation
            documents = IndexManager.All<UnitTestIndexTestDocumentE>();

            Assert.NotNull(documents);
            Assert.False(documents.Any());

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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var all = IndexManager.All<UnitTestIndexTestDocumentE>();

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
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentE>();

            // validation
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentE>));

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
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentE>();

            // validation
            Assert.Null(document);

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
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            var count = items.Count();

            // act
            IndexManager.Close<UnitTestIndexTestDocumentE>();

            // validation
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            items = IndexManager.Retrieve<UnitTestIndexTestDocumentE>($"name = '{randomItem.Name}'");
            Assert.NotNull(items);
            Assert.Equal(count, items.Count());

            Postconditions();
        }
    }
}
