using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Predicate;
using Xunit;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the predicate functions of WQL: a function that stands for a whole condition and
    /// binds state computed ahead, here a set of ids by name.
    /// </summary>
    /// <param name="fixture">The index with the test documents.</param>
    public class UnitTestWqlPredicates(UnitTestIndexFixtureWqlA fixture) : IClassFixture<UnitTestIndexFixtureWqlA>
    {
        private static readonly Guid HelenaA = new("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a");
        private static readonly Guid HelenaB = new("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f");
        private static readonly Guid Bob = new("0d740124-7707-4b1a-a9f1-3a365a15f60b");

        /// <summary>
        /// The id sets the predicate knows.
        /// </summary>
        private static readonly Dictionary<string, Guid[]> Groups = new()
        {
            ["SD-12"] = [HelenaA, HelenaB, Bob],
            ["SD-13"] = []
        };

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlA Fixture { get; } = fixture;

        /// <summary>
        /// Builds a parser that knows within(&lt;group&gt;).
        /// </summary>
        /// <returns>The parser.</returns>
        private static WqlParser<UnitTestIndexTestDocumentA> Parser()
        {
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            parser.RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("within", arguments =>
            {
                // the set is computed when the statement is bound, not once per item
                var ids = Groups[(string)arguments[0]].ToList();

                return x => ids.Contains(x.Id);
            }));

            return parser;
        }

        /// <summary>
        /// Tests that a predicate filters by its bound set, in memory and against the index alike.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="count">The expected number of matches.</param>
        [Theory]
        [InlineData("within('SD-12')", 3)]
        [InlineData("WITHIN(\"SD-12\")", 3)]
        [InlineData("within('SD-13')", 0)]
        [InlineData("within('SD-12') and text ~ 'Bob'", 1)]
        [InlineData("text ~ 'Bob' or within('SD-12')", 8)]
        [InlineData("not within('SD-12')", 8)]
        [InlineData("(within('SD-12'))", 3)]
        public void Filter(string wql, int count)
        {
            // arrange
            var statement = Parser().Parse(wql);
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentA>();

            // act
            var inMemory = statement.ToQuery().Apply(Fixture.TestData.AsQueryable()).ToList();
            var translatable = statement.ToQuery(WqlQueryMode.Translatable).Apply(Fixture.TestData.AsQueryable()).ToList();
            var indexed = statement.Apply(document).ToList();

            // validation
            Assert.False(statement.HasErrors, statement.Error?.Message);
            Assert.Equal(count, inMemory.Count);
            Assert.Equal(count, translatable.Count);
            Assert.Equal(inMemory.Select(x => x.Id).OrderBy(x => x), indexed.Select(x => x.Id).OrderBy(x => x));
        }

        /// <summary>
        /// Tests that a call the predicate cannot bind, or one with an attribute as argument, is an error.
        /// </summary>
        /// <param name="wql">The statement.</param>
        [Theory]
        [InlineData("within('SD-99')")]
        [InlineData("within(text)")]
        [InlineData("within('SD-12'")]
        public void Refused(string wql)
        {
            // act
            var statement = Parser().Parse(wql);

            // validation
            Assert.True(statement.HasErrors);
        }

        /// <summary>
        /// Tests that a parser without the predicate does not know the call, and that a removed
        /// predicate is unknown again.
        /// </summary>
        [Fact]
        public void Unregistered()
        {
            // arrange
            var parser = Parser();
            parser.RemovePredicate("within");

            // act
            var statement = parser.Parse("within('SD-12')");

            // validation
            Assert.True(statement.HasErrors);
            Assert.True(new WqlParser<UnitTestIndexTestDocumentA>().Parse("within('SD-12')").HasErrors);
        }

        /// <summary>
        /// Tests that a predicate registered in the index manager is known to the statements the
        /// manager reads as text, and unknown again once it is removed.
        /// </summary>
        [Fact]
        public void IndexManagerKnowsRegisteredPredicates()
        {
            // arrange
            var ids = Groups["SD-12"].ToList();
            Fixture.IndexManager.RegisterWqlPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("within", _ => x => ids.Contains(x.Id)));

            // act
            var registered = Fixture.IndexManager.Retrieve<UnitTestIndexTestDocumentA>("within('SD-12')").ToList();
            Fixture.IndexManager.RemoveWqlPredicate<UnitTestIndexTestDocumentA>("within");
            var removed = Fixture.IndexManager.Retrieve<UnitTestIndexTestDocumentA>("within('SD-12')").ToList();

            // validation
            Assert.Equal(3, registered.Count);
            Assert.Empty(removed);
        }

        /// <summary>
        /// Tests that the statement prints the call as written.
        /// </summary>
        [Fact]
        public void Text()
        {
            // act
            var statement = Parser().Parse("within('SD-12') and text ~ 'Bob'");

            // validation
            Assert.Contains("within(\"SD-12\") and", statement.Filter.ToString());
        }
    }
}
