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
            // preconditions
            Preconditions();

            // test execution
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>());

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
            // preconditions
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // test execution
            IndexManager.ReIndex(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.NotEmpty(item);

            // postconditions
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

            // preconditions
            IndexStorageBuffer.MaxCachedSegments = maxCachedSegmentsRange;
            IndexStorageFile.BufferSize = (uint)bufferSizeRange;

            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // test execution
            await IndexManager.ReIndexAsync(data);

            randomItem ??= IndexManager.All<UnitTestIndexTestDocumentC>().Skip(new Random().Next() % data.Count()).FirstOrDefault();
            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            Assert.NotEmpty(item);

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests the removal of a document from the index manager.
        /// </summary>
        [Fact]
        public void Delete()
        {
            // preconditions
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            var before = wql.Apply().ToList();
            Assert.NotEmpty(before);

            // test execution
            IndexManager.Delete(randomItem);

            wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            var after = wql.Apply().ToList();
            Assert.Equal(before.Count - 1, after.Count);

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests the add function of the index manager.
        /// </summary>
        [Fact]
        public void Add()
        {
            // preconditions
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // test execution
            IndexManager.Insert(new UnitTestIndexTestDocumentC()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Text = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
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
            // preconditions
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // test execution
            IndexManager.Update(new UnitTestIndexTestDocumentC()
            {
                Id = randomItem.Id,
                Text = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
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
            // preconditions
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            await IndexManager.ReIndexAsync(Fixture.TestData);

            // test execution
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentC()
            {
                Id = randomItem.Id,
                Text = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>("text = 'Aurora'");
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
            // preconditions
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentC>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // test execution
            IndexManager.Clear<UnitTestIndexTestDocumentC>();

            documents = IndexManager.All<UnitTestIndexTestDocumentC>();

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
            // preconditions
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // test execution
            var all = IndexManager.All<UnitTestIndexTestDocumentC>();

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
            // preconditions
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // test execution
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentC>));

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests get a document from the index manager.
        /// </summary>
        [Fact]
        public void GetDocument_Not()
        {
            // preconditions
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // test execution
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentC>();
            Assert.Null(document);

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
            // preconditions
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);
            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            var item = wql.Apply();
            var count = item.Count();

            // test execution
            IndexManager.Close<UnitTestIndexTestDocumentC>();

            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text = '{randomItem.Text.Split(' ').FirstOrDefault()}'");
            Assert.NotNull(wql);

            item = wql.Apply();
            Assert.Equal(count, item.Count());

            // postconditions
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

            // preconditions
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
                    // test execution
                    var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");
                    Assert.NotNull(wql);

                    var item = wql.Apply();
                    Assert.NotEmpty(item);

                    return Task.CompletedTask;
                }));
            }

            await Task.WhenAll(tasks);

            // postconditions
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

            // preconditions
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
                    // test execution
                    var wql = await IndexManager.RetrieveAsync<UnitTestIndexTestDocumentC>($"text ~ '{randomItem.Text.Split(' ').FirstOrDefault()}'");
                    Assert.NotNull(wql);

                    var item = wql.Apply();
                    Assert.NotEmpty(item);

                    return Task.CompletedTask;
                }));
            }

            await Task.WhenAll(tasks);

            // postconditions
            Postconditions();
        }
    }
}
