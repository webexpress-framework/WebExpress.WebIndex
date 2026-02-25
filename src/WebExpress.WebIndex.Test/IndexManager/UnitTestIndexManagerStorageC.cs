using System.Globalization;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.IndexManager
{
    /// <summary>
    /// Test class for testing the storage-based index manager.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestIndexManagerStorageC(UnitTestIndexFixtureIndexC fixture, ITestOutputHelper output) : UnitTestIndexManager<UnitTestIndexFixtureIndexC>(fixture, output)
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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // validation
            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>());

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            IndexManager.ReIndex(Fixture.TestData);

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(items);
            Assert.NotEmpty(items);

            Postconditions();
        }

        /// <summary>
        /// Tests the reindex function in a series of tests from the index manager.
        /// </summary>
        [Theory]
        [InlineData(100, 100, 100, 15, "en")]
        [InlineData(1000, 100, 2000, 15, "en")]
        public async Task ReIndexAsync(int itemCount, int wordCount, int vocabulary, int wordLength, string culture)
        {
            var w = wordCount;
            var i = itemCount;
            var v = vocabulary;
            var l = wordLength;
            var maxCachedSegmentsRange = 50000u;
            var bufferSizeRange = Math.Pow(2, 12);
            var path = Path.Combine(Environment.CurrentDirectory, "storage-reindexasync_series.csv");
            var exists = File.Exists(path);

            var data = UnitTestIndexTestDocumentFactoryC.GenerateTestData(i, w, v, l);
            var randomItem = default(UnitTestIndexTestDocumentC);
            var mem = Fixture.GetUsedMemory();
            var tasks = new List<Task>();

            // arrange
            IndexStorageBuffer.MaxCachedSegments = maxCachedSegmentsRange;
            IndexStorageFile.BufferSize = (uint)bufferSizeRange;

            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            await IndexManager.ReIndexAsync(data);

            // validation
            randomItem ??= IndexManager.All<UnitTestIndexTestDocumentC>().Skip(new Random().Next() % data.Count()).FirstOrDefault();
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");
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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var before = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(before);
            Assert.NotEmpty(before);

            // act
            IndexManager.Delete(randomItem);

            // validation
            var after = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentC()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Text = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
            Assert.NotNull(items);
            Assert.Equal(1, items.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Update(new UnitTestIndexTestDocumentC()
            {
                Id = randomItem.Id,
                Text = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
            Assert.NotNull(items);
            Assert.Equal(1, items.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            await IndexManager.ReIndexAsync(Fixture.TestData);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentC()
            {
                Id = randomItem.Id,
                Text = "Aurora"
            });

            // validation
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
            Assert.NotNull(items);
            Assert.Equal(1, items.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentC>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // act
            IndexManager.Clear<UnitTestIndexTestDocumentC>();

            // validation
            documents = IndexManager.All<UnitTestIndexTestDocumentC>();

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var all = IndexManager.All<UnitTestIndexTestDocumentC>();

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
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

            // validation
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentC>));

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
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();

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
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);
            var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(items);
            var count = items.Count();

            // act
            IndexManager.Close<UnitTestIndexTestDocumentC>();

            // validation
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(items);
            Assert.Equal(count, items.Count());

            Postconditions();
        }

        /// <summary>
        /// Tests the retrieve function in a series of tests from the index manager.
        /// </summary>
        [Theory]
        [InlineData(100, 100, 100, 15, "en")]
        [InlineData(1000, 100, 2000, 15, "en")]
        public async Task Retrieve(int itemCount, int wordCount, int vocabulary, int wordLength, string culture)
        {
            var w = wordCount;
            var i = itemCount;
            var v = vocabulary;
            var l = wordLength;
            var maxCachedSegmentsRange = 50000u;
            var bufferSizeRange = Math.Pow(2, 12);
            var path = Path.Combine(Environment.CurrentDirectory, "storage-reindexasync_series.csv");
            var exists = File.Exists(path);

            var data = UnitTestIndexTestDocumentFactoryC.GenerateTestData(i, w, v, l);
            var randomItem = default(UnitTestIndexTestDocumentC);
            var mem = Fixture.GetUsedMemory();
            var tasks = new List<Task>();

            // arrange
            IndexStorageBuffer.MaxCachedSegments = maxCachedSegmentsRange;
            IndexStorageFile.BufferSize = (uint)bufferSizeRange;

            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(data);
            randomItem ??= IndexManager.All<UnitTestIndexTestDocumentC>().Skip(new Random().Next() % data.Count()).FirstOrDefault();

            for (int t = 0; t < 25; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    // act
                    var items = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");

                    // validation
                    Assert.NotNull(items);
                    Assert.NotEmpty(items);

                    return Task.CompletedTask;
                }));
            }

            await Task.WhenAll(tasks);

            Postconditions();
        }

        /// <summary>
        /// Tests the retrieve function in a series of tests from the index manager.
        /// </summary>
        [Theory]
        [InlineData(100, 100, 100, 15, "en")]
        [InlineData(1000, 100, 2000, 15, "en")]
        public async Task RetrieveAsync(int itemCount, int wordCount, int vocabulary, int wordLength, string culture)
        {
            var w = wordCount;
            var i = itemCount;
            var v = vocabulary;
            var l = wordLength;
            var maxCachedSegmentsRange = 50000u;
            var bufferSizeRange = Math.Pow(2, 12);
            var path = Path.Combine(Environment.CurrentDirectory, "storage-reindexasync_series.csv");
            var exists = File.Exists(path);

            var data = UnitTestIndexTestDocumentFactoryC.GenerateTestData(i, w, v, l);
            var randomItem = default(UnitTestIndexTestDocumentC);
            var mem = Fixture.GetUsedMemory();
            var tasks = new List<Task>();

            // arrange
            IndexStorageBuffer.MaxCachedSegments = maxCachedSegmentsRange;
            IndexStorageFile.BufferSize = (uint)bufferSizeRange;

            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            await IndexManager.ReIndexAsync(data);
            randomItem ??= IndexManager.All<UnitTestIndexTestDocumentC>().Skip(new Random().Next() % data.Count()).FirstOrDefault();

            for (int t = 0; t < 25; t++)
            {
                tasks.Add(await Task.Run(async () =>
                {
                    // act
                    var items = await IndexManager.RetrieveAsync<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");

                    // validation
                    Assert.NotNull(items);
                    Assert.NotEmpty(items);

                    return Task.CompletedTask;
                }));
            }

            await Task.WhenAll(tasks);

            Postconditions();
        }
    }
}
