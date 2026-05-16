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
    public class UnitTestIndexManagerStorageA : UnitTestIndexManager<UnitTestIndexFixtureIndexA>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The log.</param>
        /// <param name="output">The test context.</param>
        public UnitTestIndexManagerStorageA(UnitTestIndexFixtureIndexA fixture, ITestOutputHelper output)
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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            Assert.NotNull(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>());

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            IndexManager.ReIndex(Fixture.TestData);

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            Assert.Equal(4, item.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);

            // act
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            Assert.Equal(4, item.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            Assert.Equal(4, item.Count());

            // act
            IndexManager.Delete(Fixture.TestData[0]);

            // validation
            item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            Assert.Equal(3, item.Count());

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Insert(new UnitTestIndexTestDocumentA()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Text = "Hello Aurora!"
            });

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Aurora'");
            Assert.NotNull(item);
            Assert.Single(item);

            Postconditions();
        }

        /// <summary>
        /// Tests the register wql function of the index manager.
        /// </summary>
        [Fact]
        public void RegisterWqlFunction()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Insert(new UnitTestIndexTestDocumentA()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Text = "abc"
            });

            // act
            IndexManager.RegisterWqlFunction<TestWqlExpressionNodeFilterFunctionConstant<UnitTestIndexTestDocumentA>>();

            // validation
            var functions = IndexManager.WqlFunctions;
            Assert.NotEmpty(functions);

            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'abc'");
            Assert.NotNull(item);
            Assert.Equal("abc", item.FirstOrDefault()?.Text);

            Postconditions();
        }

        /// <summary>
        /// Tests the remove wql function of the index manager.
        /// </summary>
        [Fact]
        public void RemoveWqlFunction()
        {
            // arrange
            Preconditions();
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Insert(new UnitTestIndexTestDocumentA()
            {
                Id = Guid.Parse("ED242C79-E41B-4214-BFBC-C4673E87433B"),
                Text = "abc"
            });

            IndexManager.RegisterWqlFunction<TestWqlExpressionNodeFilterFunctionConstant<UnitTestIndexTestDocumentA>>();
            Assert.NotEmpty(IndexManager.WqlFunctions);

            // act
            IndexManager.RemoveWqlFunction<TestWqlExpressionNodeFilterFunctionConstant<UnitTestIndexTestDocumentA>>();
            Assert.Empty(IndexManager.WqlFunctions);

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            IndexManager.Update(new UnitTestIndexTestDocumentA()
            {
                Id = Fixture.TestData[1].Id,
                Text = "Hello Helena, hello Aurora!"
            });

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Aurora'");
            Assert.NotNull(item);
            Assert.Single(item);

            item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helge'");
            Assert.NotNull(item);
            Assert.Single(item);

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            await IndexManager.ReIndexAsync(Fixture.TestData, token: TestContext.Current.CancellationToken);

            // act
            await IndexManager.UpdateAsync(new UnitTestIndexTestDocumentA()
            {
                Id = new Guid("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f"),
                Text = "Hello Helena, hello Aurora!"
            });

            // validation
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Aurora'");
            Assert.NotNull(item);
            Assert.Single(item);

            item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helge'");
            Assert.NotNull(item);
            Assert.Single(item);

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            var documents = IndexManager.All<UnitTestIndexTestDocumentA>();

            Assert.NotNull(documents);
            Assert.True(documents.Any());

            // act
            IndexManager.Clear<UnitTestIndexTestDocumentA>();

            // validation
            documents = IndexManager.All<UnitTestIndexTestDocumentA>();

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);

            // act
            var all = IndexManager.All<UnitTestIndexTestDocumentA>();

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
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // validation
            Assert.NotNull(document);
            Assert.True(document.GetType() == typeof(IndexDocument<UnitTestIndexTestDocumentA>));

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
            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentC>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentD>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            IndexManager.Create<UnitTestIndexTestDocumentE>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            // act
            var document = IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

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
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            IndexManager.ReIndex(Fixture.TestData);
            var item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            var count = item.Count();

            // act
            IndexManager.Close<UnitTestIndexTestDocumentA>();

            // validation
            IndexManager.Create<UnitTestIndexTestDocumentA>(CultureInfo.GetCultureInfo(culture), IndexType.Storage);
            item = IndexManager.Retrieve<UnitTestIndexTestDocumentA>("text ~ 'Helena'");
            Assert.NotNull(item);
            Assert.Equal(count, item.Count());

            Postconditions();
        }
    }
}
