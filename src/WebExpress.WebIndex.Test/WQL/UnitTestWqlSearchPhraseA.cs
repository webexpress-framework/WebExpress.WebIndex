using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Phrase search (exact word sequence)
    /// </summary>
    public class UnitTestWqlSearchPhraseA(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlA Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql1()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena'");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text=\"Helena\"");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql3()
        {
            // act
            var wql = Fixture.ExecuteWql("text=Helena");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseValidWql4()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena Helge'");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql1()
        {
            // act
            var wql = Fixture.ExecuteWql("text=Helena Helge order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql2()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena Helge order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql3()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena Helge\" order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests the parser.
        /// </summary>
        [Fact]
        public void ParseInvalidWql4()
        {
            // act
            var wql = Fixture.ExecuteWql("='Helena Helge\" order by text");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void SingleMatch1()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena'");
            var res = Fixture.IndexManager.Retrieve(wql);

            Assert.NotNull(res);
            Assert.Equal(4, res.Count());
            Assert.Contains("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a", res.Select(x => x.Id.ToString()));
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
            Assert.Contains("a8901fac-aaef-483b-aba8-dba74e36e7fc", res.Select(x => x.Id.ToString()));
            Assert.Contains("3f3d7066-a925-42ac-90f7-ef100afb8460", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void MultipleMatch1()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Helena and Helge!'");
            var res = Fixture.IndexManager.Retrieve(wql);

            Assert.NotNull(res);
            Assert.Single(res);
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void MultipleMatch2()
        {
            // act
            var wql = Fixture.ExecuteWql("text='Hello Helena and Helge!'");
            var res = Fixture.IndexManager.Retrieve(wql);

            Assert.NotNull(res);
            Assert.Single(res);
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// The test produces an unexpected positive result even though there is no exact match. This is because stop words are removed but are 
        /// important for the order of the following words.
        /// </summary>
        [Fact]
        public void UnexpectedMatch()
        {
            // act
            // Hello Helena and Helge!
            var wql = Fixture.ExecuteWql("text='Hello Helena hello Helge!'");
            var res = Fixture.IndexManager.Retrieve(wql);

            Assert.NotNull(res);
            Assert.Single(res);
            Assert.Contains("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f", res.Select(x => x.Id.ToString()));
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void EmptyMatch1()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text=''");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            Assert.Empty(res);
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void EmptyMatch2()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text='Sophie'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            Assert.Empty(res);
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void NotExactMatch1()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text='Hello Helena, Aurora and Helge!'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            Assert.Empty(res);
        }

        /// <summary>
        /// Tests phrase search, which retrieves content from documents that contain a specific order and combination of words defined by the phrase.
        /// </summary>
        [Fact]
        public void NotExactMatch2()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text='Helena Helge'");
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // act
            var res = wql?.Apply(document);

            // validation
            Assert.NotNull(res);
            Assert.Empty(res);
        }
    }
}
