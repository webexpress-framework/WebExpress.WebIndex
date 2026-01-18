using System.Globalization;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.IndexManager
{
    /// <summary>
    /// Test class for testing the memory-based index manager for unicode.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestIndexManagerMemoryF : UnitTestIndexManager<UnitTestIndexFixtureIndexF>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestIndexManagerMemoryF(UnitTestIndexFixtureIndexF fixture, ITestOutputHelper output)
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
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);

            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentF>());

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
            var randomItem = Fixture.TestData.LastOrDefault();
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo(culture), IndexType.Memory);

            // act
            IndexManager.ReIndex(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{randomItem.Name}'");
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
            var randomItem = Fixture.TestData.LastOrDefault();
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo(culture), IndexType.Memory);

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{randomItem.Name}'");
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
            // arrange
            Preconditions();
            var randomItem = Fixture.TestData.LastOrDefault();
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var before = wql.Apply().ToList();
            Assert.True(before.Any());

            // act
            IndexManager.Delete(randomItem);

            wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{randomItem.Name}'");
            Assert.NotNull(wql);

            var after = wql.Apply().ToList();
            Assert.True(before.Count - 1 == after.Count);

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests the add function of the index manager.
        /// </summary>
        [Theory]
        [InlineData("ED242C79-E41B-4214-BFBC-C4673E87433B", "Aurora")]
        [InlineData("A20BC371-10F9-4F43-9DA8-F4B4F0BE26AB", "李明")]
        [InlineData("80A78EBB-9819-45AF-BC0F-68E68D0C8C1A", "Sun Leaf Lion 🌞🌿🦁")]
        [InlineData("29F34DFD-432D-4315-88C2-CE41F293AC71", "🦋🌼🌙 Butterfly Flower Moon")]
        public void Add(string id, string name)
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentF()
            {
                Id = Guid.Parse(id),
                Name = name
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{name}'");
            var item = wql.Apply();

            Assert.NotNull(wql);
            Assert.Equal(1, item.Count());

            // postconditions
            Postconditions();
        }

        /// <summary>
        /// Tests the add function of the index manager.
        /// </summary>
        [Theory]
        [InlineData("9733A649-1E5E-4B1F-8C6E-9A4B6AB54292", "🌟🍀🐉")]
        public void NotAdd(string id, string name)
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentF()
            {
                Id = Guid.Parse(id),
                Name = name
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>($"name = '{name}'");
            var item = wql.Apply();

            Assert.NotNull(wql);
            Assert.Empty(item);

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
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Update(new UnitTestIndexTestDocumentF()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>("name = 'Aurora'");
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
        public async Task UpdateAsync()
        {
            // arrange
            Preconditions();
            var randomItem = Fixture.RandomItem;
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            await IndexManager.ReIndexAsync(Fixture.TestData);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentF()
            {
                Id = randomItem.Id,
                Name = "Aurora"
            });

            var wql = IndexManager.Retrieve<UnitTestIndexTestDocumentF>("name = 'Aurora'");
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
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentF>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // act
            IndexManager.Clear<UnitTestIndexTestDocumentF>();

            documents = IndexManager.All<UnitTestIndexTestDocumentF>();

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
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var all = IndexManager.All<UnitTestIndexTestDocumentF>();

            Assert.NotEmpty(all);

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentF>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentF>();
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentF>));

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentF>();
            Assert.Null(document);

            // postconditions
            Postconditions();
        }
    }
}
