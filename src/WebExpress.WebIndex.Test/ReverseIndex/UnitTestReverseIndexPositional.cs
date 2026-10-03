using System.Globalization;
using System.Reflection;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Tests phrase, proximity and fuzzy retrieval as well as the result limit, on the memory and
    /// the storage index alike, since both have to answer a query the same way.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestReverseIndexPositional(UnitTestIndexFixtureWqlA fixture) : IClassFixture<UnitTestIndexFixtureWqlA>, IDisposable
    {
        private readonly IndexContext _context = new() { IndexDirectory = Path.Combine(new IndexContext().IndexDirectory, Guid.NewGuid().ToString()) };
        private readonly List<WebIndex.IndexManager> _managers = [];

        /// <summary>
        /// Gets the fixture whose parser checks the query syntax.
        /// </summary>
        private UnitTestIndexFixtureWqlA Fixture { get; } = fixture;

        /// <summary>
        /// The second term occurs twice within reach of the first; only the later occurrence
        /// leads on to the third term. A proximity search has to try it rather than give up after
        /// the first one.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void ProximityTriesEveryContinuation(IndexType indexType)
        {
            // arrange
            var manager = CreateManager(indexType,
                "zeta kappa kappa omega sigma",
                "zeta omega omega omega sigma kappa");

            // act
            var items = manager.Retrieve<PositionalDocument>("Text ~ 'zeta kappa sigma' :2").ToList();

            // validation
            Assert.Equal("zeta kappa kappa omega sigma", Assert.Single(items).Text);
        }

        /// <summary>
        /// The same arrangement for a phrase with a distance: the first occurrence of the second
        /// term does not lead to the third, the second one does.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void PhraseTriesEveryContinuation(IndexType indexType)
        {
            // arrange
            var manager = CreateManager(indexType, "zeta kappa kappa omega sigma");

            // act
            var items = manager.Retrieve<PositionalDocument>("Text = 'zeta kappa sigma' :1").ToList();

            // validation
            Assert.Single(items);
        }

        /// <summary>
        /// A similarity applies to every term of a proximity search too, so adding a distance to a
        /// fuzzy query keeps it fuzzy instead of turning it into an exact one.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void SimilarityAppliesToProximity(IndexType indexType)
        {
            // arrange
            var manager = CreateManager(indexType, "zeta kappa sigma", "omega delta");

            // act
            var fuzzy = manager.Retrieve<PositionalDocument>("Text ~ 'zetta kapa' ~70").ToList();
            var fuzzyWithDistance = manager.Retrieve<PositionalDocument>("Text ~ 'zetta kapa' ~70 :1").ToList();

            // validation
            Assert.Single(fuzzy);
            Assert.Single(fuzzyWithDistance);
        }

        /// <summary>
        /// The result limit cuts the complete result, not the candidates of the first term: a
        /// document confirmed only by a later term must not be lost to the limit.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void LimitAppliesAfterAllTerms(IndexType indexType)
        {
            // arrange
            var texts = Enumerable.Range(0, 40).Select(i => $"common filler{i}").Append("common rare").ToArray();
            var manager = CreateManager(indexType, texts);
            var reverseIndex = manager.GetIndexDocument<PositionalDocument>()
                .GetReverseIndex(new IndexFieldData(typeof(PositionalDocument).GetProperty(nameof(PositionalDocument.Text))));

            // act
            var ids = reverseIndex.Retrieve("common rare", new IndexRetrieveOptions(1)).ToList();

            // validation
            Assert.Single(ids);
        }

        /// <summary>
        /// The result limit is the exact maximum on both index types.
        /// </summary>
        [Theory]
        [InlineData(IndexType.Memory)]
        [InlineData(IndexType.Storage)]
        public void LimitIsExact(IndexType indexType)
        {
            // arrange
            var manager = CreateManager(indexType, [.. Enumerable.Range(0, 10).Select(i => $"common filler{i}")]);
            var reverseIndex = manager.GetIndexDocument<PositionalDocument>()
                .GetReverseIndex(new IndexFieldData(typeof(PositionalDocument).GetProperty(nameof(PositionalDocument.Text))));

            // act
            var ids = reverseIndex.Retrieve("common", new IndexRetrieveOptions(3)).ToList();

            // validation
            Assert.Equal(3, ids.Count);
        }

        /// <summary>
        /// A similarity is a percentage; a value beyond 100 is refused instead of being read as an
        /// exact search.
        /// </summary>
        [Theory]
        [InlineData("text ~ 'Helena' ~0", false)]
        [InlineData("text ~ 'Helena' ~100", false)]
        [InlineData("text ~ 'Helena' ~101", true)]
        [InlineData("text ~ 'Helena' ~ 150", true)]
        [InlineData("text ~ 'Helena' ~99999999999", true)]
        public void SimilarityOutsideOfPercentIsRefused(string wql, bool error)
        {
            // act
            var statement = Fixture.ExecuteWql(wql);

            // validation
            Assert.Equal(error, statement.HasErrors);
        }

        /// <summary>
        /// A distance is a word count; a value beyond its range is refused as a syntax error
        /// rather than surfacing as an overflow. The same holds for the count of take and skip.
        /// </summary>
        [Theory]
        [InlineData("text ~ 'Helena' :3", false)]
        [InlineData("text ~ 'Helena' :99999999999", true)]
        [InlineData("text ~ 'Helena' : 99999999999", true)]
        [InlineData("text ~ 'Helena' take 99999999999", true)]
        public void DistanceBeyondRangeIsRefused(string wql, bool error)
        {
            // act
            var statement = Fixture.ExecuteWql(wql);

            // validation
            Assert.Equal(error, statement.HasErrors);
        }

        /// <summary>
        /// A negation subtracts the matches from all documents. The matches must not be cut to
        /// the result limit first, or every match beyond it would turn up as a non-match.
        /// </summary>
        [Theory]
        [InlineData("Text != 'lorem'")]
        [InlineData("Text not in ('lorem')")]
        public void NegationExcludesMatchesBeyondTheLimit(string wql)
        {
            // arrange
            var limit = (int)new IndexRetrieveOptions().MaxResults;
            var texts = Enumerable.Repeat("lorem", limit + 5).Append("ipsum").Append("dolor").ToArray();
            var manager = CreateManager(IndexType.Memory, texts);

            // act
            var items = manager.Retrieve<PositionalDocument>(wql).ToList();

            // validation
            Assert.Equal(["dolor", "ipsum"], items.Select(x => x.Text).Order());
        }

        /// <summary>
        /// Creates an index manager of the given type holding a document per text.
        /// </summary>
        /// <param name="indexType">The index type.</param>
        /// <param name="texts">The texts of the documents.</param>
        /// <returns>The index manager.</returns>
        private WebIndex.IndexManager CreateManager(IndexType indexType, params string[] texts)
        {
            var manager = new IndexManagerTest();
            typeof(IndexManagerTest)
                .GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, [_context]);
            _managers.Add(manager);

            manager.Create<PositionalDocument>(CultureInfo.GetCultureInfo("en"), indexType);

            foreach (var text in texts)
            {
                manager.Insert(new PositionalDocument() { Id = Guid.NewGuid(), Text = text });
            }

            return manager;
        }

        /// <summary>
        /// Releases the index managers and removes the index directory.
        /// </summary>
        public void Dispose()
        {
            foreach (var manager in _managers)
            {
                manager.Dispose();
            }

            if (Directory.Exists(_context.IndexDirectory))
            {
                Directory.Delete(_context.IndexDirectory, true);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// A document with a single text field.
        /// </summary>
        public class PositionalDocument : IIndexItem
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
        }
    }
}
