using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Condition;
using WebExpress.WebIndex.Wql.Function;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests for additional WQL operators: !=, is, is not.
    /// </summary>
    public class UnitTestWqlOperators(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
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
        /// Verifies that a not-equal query parses without errors.
        /// </summary>
        [Fact]
        public void ParseNotEqual()
        {
            // act
            var wql = Fixture.ExecuteWql("text != 'Helena'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that a not-equal query generates a correct ToQuery expression.
        /// </summary>
        [Fact]
        public void NotEqualToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text != 'Helena'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
            Assert.True(res.All(x => !x.Text.Equals("Helena", System.StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Verifies that a not-equal query string representation is correct.
        /// </summary>
        [Fact]
        public void NotEqualToString()
        {
            // act
            var wql = Fixture.ExecuteWql("text != 'Helena'");

            // validation
            Assert.Equal("Text != 'Helena'", wql.ToString());
        }

        /// <summary>
        /// Verifies that "is" operator parses correctly.
        /// </summary>
        [Fact]
        public void ParseIs()
        {
            // act
            var wql = Fixture.ExecuteWql("text is null");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that "is" operator generates a correct ToQuery expression.
        /// </summary>
        [Fact]
        public void IsNullToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text is null");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
            Assert.True(res.All(x => x.Text == null));
        }

        /// <summary>
        /// Verifies that "is not" operator parses correctly.
        /// </summary>
        [Fact]
        public void ParseIsNot()
        {
            // act
            var wql = Fixture.ExecuteWql("text is not null");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that "is not" operator generates a correct ToQuery expression.
        /// </summary>
        [Fact]
        public void IsNotNullToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text is not null");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
            Assert.True(res.All(x => x.Text != null));
        }

        /// <summary>
        /// Verifies that "is not" operator string representation is correct.
        /// </summary>
        [Fact]
        public void IsNotNullToString()
        {
            // act
            var wql = Fixture.ExecuteWql("text is not null");

            // validation
            Assert.Equal("Text is not 'null'", wql.ToString());
        }

        /// <summary>
        /// Verifies that the != operator can be combined with AND.
        /// </summary>
        [Fact]
        public void NotEqualWithAnd()
        {
            // act
            var wql = Fixture.ExecuteWql("text != 'Helena' and text != 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the != operator can be combined with OR.
        /// </summary>
        [Fact]
        public void NotEqualWithOr()
        {
            // act
            var wql = Fixture.ExecuteWql("text != 'Helena' or text != 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies the incremental lookahead analysis works for != operator.
        /// </summary>
        [Theory]
        [InlineData("text !=", false, 2)]
        [InlineData("text != 'Helena'", true, 5)]
        public void AnalyzeNotEqual(string wql, bool valid, int count)
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();

            // act
            var ila = parser.Analyze(wql);

            // validation
            Assert.NotNull(ila);
            Assert.Equal(valid, ila.IsValidSoFar);
            Assert.Equal(count, ila.Items.Count());
        }

        /// <summary>
        /// Verifies that registering and removing conditions works.
        /// </summary>
        [Fact]
        public void RegisterAndRemoveCondition()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();

            // act - remove and re-register
            parser.RemoveCondition("!=");
            parser.RegisterCondition<WqlExpressionNodeFilterConditionBinaryNotEqual<UnitTestIndexTestDocumentA>>();

            // validation - should still parse
            var wql = parser.Parse("text != 'Helena'");
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that registering and removing functions works.
        /// </summary>
        [Fact]
        public void RegisterAndRemoveFunction()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();

            // act - remove and re-register
            parser.RemoveFunction("upper");
            parser.RegisterFunction<WqlExpressionNodeFilterFunctionUpper<UnitTestIndexTestDocumentA>>();

            // validation - should still parse
            var wql = parser.Parse("text ~ upper('test')");
            Assert.False(wql.HasErrors);
        }
    }
}
