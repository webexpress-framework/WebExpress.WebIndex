using System.Globalization;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

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
    [Collection("NonParallelTests")]
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

            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentB>());

            // postconditions
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

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.NotEmpty(item);

            // postconditions
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
            await IndexManager.ReIndexAsync(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.NotEmpty(item);
            Assert.Equal(item.FirstOrDefault().Name, randomItem.Name);

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests the reindex function from the index manager.
        /// </summary>
        [Fact]
        public async Task ReIndexAsyncCancel()
        {
            // arrange
            Preconditions();
            var lastItem = Fixture.TestData.LastOrDefault();
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            var cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<int>(percent =>
            {
                if (percent > 30)
                {
                    // cancel task
                    cancellationTokenSource.Cancel();
                }
            });

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData, progress, cancellationTokenSource.Token);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{lastItem.Name}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.Empty(item);

            // postconditions
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

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var before = wql.Apply().ToList();
            Assert.NotEmpty(before);

            // act
            IndexManager.Delete(randomItem);

            wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var after = wql.Apply().ToList();
            Assert.True(before.Count - 1 == after.Count);

            // postconditions
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

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            var item = wql.Apply();

            Assert.NotNull(wql);
            Assert.Equal(1, item.Count());

            // postconditions
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

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.Equal(1, item.Count());

            // postconditions
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
            await IndexManager.ReIndexAsync(Fixture.TestData);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentB()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>("name = 'Aurora'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.Equal(1, item.Count());

            // postconditions
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

            documents = IndexManager.All<UnitTestIndexTestDocumentB>();

            Assert.NotNull(documents);
            Assert.False(documents.Any());

            // postconditions
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

            Assert.True(all.Select(x => x.Id).OrderBy(x => x).SequenceEqual(Fixture.TestData.Select(x => x.Id).OrderBy(x => x)));

            // postconditions
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
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentB>));

            // postconditions
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
            Assert.Null(document);

            // postconditions
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
            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>(wqlString);
            Assert.NotNull(wql);

            // act
            var items = wql.Apply();
            Assert.Contains(expected, items.Select(x => x.Name.ToString()));

            // postconditions
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
            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            var count = item.Count();

            // act
            IndexManager.Close<UnitTestIndexTestDocumentB>();

            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            wql = IndexManager.Retrieve<UnitTestIndexTestDocumentB>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            item = wql.Apply();
            Assert.Equal(count, item.Count());

            // postconditions
            Postconditions();
        }
    }
}
