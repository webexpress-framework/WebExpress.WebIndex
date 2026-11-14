using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Function;
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
        /// Tests that the statement prints the call as written - a function argument as the call
        /// and not its result - in a form the parser reads back to the same text.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="call">The call as the statement prints it.</param>
        [Theory]
        [InlineData("within('SD-12') and text ~ 'Bob'", "within('SD-12')")]
        [InlineData("echo(\"O'Brien\")", "echo('O''Brien')")]
        [InlineData("echo('a\"b', 12, true, null)", "echo('a\"b', 12, true, null)")]
        [InlineData("echo(now())", "echo(now())")]
        public void Text(string wql, string call)
        {
            // arrange
            var parser = Parser();
            parser.RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("echo", _ => x => true));

            // act
            var statement = parser.Parse(wql);
            var reparsed = parser.Parse(statement.Filter.ToString());

            // validation
            Assert.False(statement.HasErrors, statement.Error?.Message);
            Assert.False(reparsed.HasErrors, reparsed.Error?.Message);
            Assert.Contains(call, statement.Filter.ToString());
            Assert.Equal(statement.Filter.ToString(), reparsed.Filter.ToString());
        }

        /// <summary>
        /// Tests that a number reaches the predicate exactly as written: a whole number as a long,
        /// which a double would round beyond 2^53, and any other as a decimal.
        /// </summary>
        [Fact]
        public void NumbersArriveExactly()
        {
            // arrange
            IReadOnlyList<object> received = null;
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            parser.RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("sprint", arguments =>
            {
                received = arguments;

                return x => true;
            }));

            // act
            var statement = parser.Parse("sprint(9007199254740993, 12, 1.25)");

            // validation
            Assert.False(statement.HasErrors, statement.Error?.Message);
            Assert.Equal(9007199254740993L, received[0]);
            Assert.Equal(12, Convert.ToInt32(received[1]));
            Assert.Equal(1.25m, received[2]);
        }

        /// <summary>
        /// Tests that a predicate failing for a reason other than its arguments - here a timeout
        /// of a lookup - is passed on to the caller instead of turning into an empty result.
        /// </summary>
        [Fact]
        public void FailureOfThePredicateIsPassedOn()
        {
            // arrange
            var predicate = new WqlPredicate<UnitTestIndexTestDocumentA>("lookup", _ => throw new TimeoutException());
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            parser.RegisterPredicate(predicate);
            Fixture.IndexManager.RegisterWqlPredicate(predicate);

            try
            {
                // act
                var parsed = Assert.Throws<WqlPredicateException>(() => parser.Parse("lookup('SD-12')"));
                var retrieved = Assert.Throws<WqlPredicateException>(() => Fixture.IndexManager.Retrieve<UnitTestIndexTestDocumentA>("lookup('SD-12')").ToList());

                // validation
                Assert.IsType<TimeoutException>(parsed.InnerException);
                Assert.Equal("lookup", parsed.Predicate);
                Assert.IsType<TimeoutException>(retrieved.InnerException);
            }
            finally
            {
                Fixture.IndexManager.RemoveWqlPredicate<UnitTestIndexTestDocumentA>("lookup");
            }
        }

        /// <summary>
        /// Tests that a call that does not fit what the predicate reads from it - a missing
        /// argument, one of another type - is an error of the statement.
        /// </summary>
        /// <param name="wql">The statement.</param>
        [Theory]
        [InlineData("within()")]
        [InlineData("within(12)")]
        public void ArgumentFaultIsAnError(string wql)
        {
            // act
            var statement = Parser().Parse(wql);

            // validation
            Assert.True(statement.HasErrors);
            Assert.Equal("webexpress.webindex:wql.predicate_failed", statement.Error.Message);
        }

        /// <summary>
        /// Tests that an unclosed call reports the missing parenthesis.
        /// </summary>
        /// <param name="wql">The statement.</param>
        [Theory]
        [InlineData("within('SD-12'")]
        [InlineData("within('SD-12' 'SD-13')")]
        [InlineData("len(text")]
        public void UnclosedCall(string wql)
        {
            // act
            var statement = Parser().Parse(wql);

            // validation
            Assert.Equal("webexpress.webindex:wql.expected_close_parenthesis", statement.Error?.Message);
        }

        /// <summary>
        /// Tests that a predicate cannot take the name of a scalar function, which it would hide
        /// from every condition, nor a name no statement can call - neither on the parser nor
        /// in the index manager - and that a function cannot take the name of a predicate.
        /// </summary>
        [Fact]
        public void NamesAreChecked()
        {
            // arrange
            var parser = Parser();
            parser.RemoveFunction("len");
            parser.RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("len", _ => x => true));

            // validation
            Assert.Throws<ArgumentException>(() => Parser().RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("len", _ => x => true)));
            Assert.Throws<ArgumentException>(() => Parser().RegisterPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("in-sprint", _ => x => true)));
            Assert.Throws<ArgumentException>(() => Fixture.IndexManager.RegisterWqlPredicate(new WqlPredicate<UnitTestIndexTestDocumentA>("upper", _ => x => true)));
            Assert.ThrowsAny<Exception>(() => parser.RegisterFunction<WqlExpressionNodeFilterFunctionLen<UnitTestIndexTestDocumentA>>());
            Assert.False(Parser().Parse("len(text) > 3").HasErrors);
        }

        /// <summary>
        /// Tests that a predicate over a set of ids filters like one over an expression, in memory
        /// and against the index alike, and that an id no item carries matches nothing.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="count">The expected number of matches.</param>
        [Theory]
        [InlineData("within('SD-12')", 3)]
        [InlineData("within('SD-13')", 0)]
        [InlineData("within('SD-12') and text ~ 'Bob'", 1)]
        [InlineData("not within('SD-12')", 8)]
        public void IdPredicate(string wql, int count)
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            parser.RegisterPredicate(new WqlIdPredicate<UnitTestIndexTestDocumentA>("within", arguments => [.. Groups[(string)arguments[0]], Guid.NewGuid()]));
            var statement = parser.Parse(wql);
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
        /// Tests that the lookahead offers a predicate where a condition may start, and that a
        /// parser without predicates prompts as before.
        /// </summary>
        /// <param name="wql">The statement typed so far.</param>
        [Theory]
        [InlineData("")]
        [InlineData("not")]
        [InlineData("(")]
        [InlineData("text ~ 'Bob' and")]
        public void LookaheadOffersPredicates(string wql)
        {
            // act
            var with = Parser().Analyze(wql);
            var without = new WqlParser<UnitTestIndexTestDocumentA>().Analyze(wql);

            // validation
            Assert.Contains(WqlExpressionType.Function, with.ExpectedNextTokens);
            Assert.DoesNotContain(WqlExpressionType.Function, without.ExpectedNextTokens);
            Assert.Equal(["within"], Parser().PredicateNames);
        }
    }
}
