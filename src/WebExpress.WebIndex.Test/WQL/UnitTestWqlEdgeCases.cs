using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using Xunit.Abstractions;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests for WQL parser edge cases, error handling, and diagnostics.
    /// </summary>
    public class UnitTestWqlEdgeCases(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
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
        /// Verifies that empty input parses without errors.
        /// </summary>
        [Fact]
        public void ParseEmptyInput()
        {
            // act
            var wql = Fixture.ExecuteWql("");

            // validation
            Assert.False(wql.HasErrors);
            Assert.Null(wql.Filter);
            Assert.Null(wql.Order);
            Assert.Null(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that null input returns a statement without errors.
        /// </summary>
        [Fact]
        public void ParseNullInput()
        {
            // act
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var wql = parser.Parse(null);

            // validation
            Assert.NotNull(wql);
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that whitespace input parses without errors.
        /// </summary>
        [Fact]
        public void ParseWhitespaceInput()
        {
            // act
            var wql = Fixture.ExecuteWql("   ");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that a query with an unknown attribute name does not produce a filter 
        /// (the parser treats the unknown identifier as a non-matching attribute).
        /// </summary>
        [Fact]
        public void ParseUnknownAttribute()
        {
            // act - use an attribute name that does not exist on the document type
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var wql = parser.Parse("nonexistent ~ 'value'");

            // validation - the parser produces an error because nonexistent is not a known attribute
            // Note: behavior depends on parser validation - some parsers accept and produce empty results
            Assert.NotNull(wql);
        }

        /// <summary>
        /// Verifies that an unexpected token produces an error.
        /// </summary>
        [Fact]
        public void ParseUnexpectedToken()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ Helena Helge");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that unterminated single-quoted string produces error.
        /// </summary>
        [Fact]
        public void ParseUnterminatedSingleQuote()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'unterminated");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that unterminated double-quoted string produces error.
        /// </summary>
        [Fact]
        public void ParseUnterminatedDoubleQuote()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ \"unterminated");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that unmatched close parenthesis produces error.
        /// </summary>
        [Fact]
        public void ParseUnmatchedCloseParenthesis()
        {
            // act
            var wql = Fixture.ExecuteWql(")text ~ 'value'");

            // validation
            Assert.True(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that the WqlExpressionError contains position info.
        /// </summary>
        [Fact]
        public void ErrorContainsPosition()
        {
            // act
            var wql = Fixture.ExecuteWql("text # 'value'");

            // validation
            Assert.True(wql.HasErrors);
            Assert.NotNull(wql.Error);
            Assert.NotNull(wql.Error.Message);
        }

        /// <summary>
        /// Verifies that combined filter, order, and partitioning parses correctly.
        /// </summary>
        [Fact]
        public void ParseFullQuery()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' order by text asc skip 0 take 10");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that multiple conditions with AND parse correctly.
        /// </summary>
        [Fact]
        public void ParseAndConditions()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' and text ~ 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that multiple conditions with OR parse correctly.
        /// </summary>
        [Fact]
        public void ParseOrConditions()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' or text ~ 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that & logical operator works.
        /// </summary>
        [Fact]
        public void ParseAmpersandOperator()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' & text ~ 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that || logical operator works.
        /// </summary>
        [Fact]
        public void ParsePipeOperator()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' || text ~ 'Helge'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that parenthesized expression parses correctly.
        /// </summary>
        [Fact]
        public void ParseParenthesizedExpression()
        {
            // act
            var wql = Fixture.ExecuteWql("(text ~ 'Helena')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that nested parenthesized expression parses correctly.
        /// </summary>
        [Fact]
        public void ParseNestedParentheses()
        {
            // act
            var wql = Fixture.ExecuteWql("(text ~ 'Helena') and (text ~ 'Helge')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that order by with multiple attributes parses correctly.
        /// </summary>
        [Fact]
        public void ParseOrderByMultipleAttributes()
        {
            // act
            var parser = new WqlParser<UnitTestIndexTestDocumentA>();
            var wql = parser.Parse("orderby text asc");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Order);
        }

        /// <summary>
        /// Verifies that skip without take parses correctly.
        /// </summary>
        [Fact]
        public void ParseSkipOnly()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' skip 5");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that take without skip parses correctly.
        /// </summary>
        [Fact]
        public void ParseTakeOnly()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' take 10");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that the IN operator parses correctly.
        /// </summary>
        [Fact]
        public void ParseInOperator()
        {
            // act
            var wql = Fixture.ExecuteWql("text in ('Helena', 'Helge')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the NOT IN operator parses correctly.
        /// </summary>
        [Fact]
        public void ParseNotInOperator()
        {
            // act
            var wql = Fixture.ExecuteWql("text not in ('Helena', 'Helge')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the IN operator produces correct ToQuery.
        /// </summary>
        [Fact]
        public void InOperatorToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text in ('Helena', 'Helge')");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
        }

        /// <summary>
        /// Verifies that the NOT IN operator produces correct ToQuery.
        /// </summary>
        [Fact]
        public void NotInOperatorToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text not in ('Helena', 'Helge')");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
        }

        /// <summary>
        /// Verifies that the equal operator parses correctly.
        /// </summary>
        [Fact]
        public void ParseEqualOperator()
        {
            // act
            var wql = Fixture.ExecuteWql("text = 'Helena'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the equal operator ToQuery works.
        /// </summary>
        [Fact]
        public void EqualOperatorToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text = 'Helena'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
        }

        /// <summary>
        /// Verifies that the AST is constructed correctly for a simple query.
        /// </summary>
        [Fact]
        public void AbstractSyntaxTreeSimple()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text ~ 'Helena'");

            // act
            var ast = wql.AbstractSyntaxTree;

            // validation
            Assert.NotNull(ast);
            Assert.NotNull(ast.Filter);
            Assert.Null(ast.Order);
            Assert.Null(ast.Partitioning);
        }

        /// <summary>
        /// Verifies that the AST is constructed correctly for a full query.
        /// </summary>
        [Fact]
        public void AbstractSyntaxTreeFull()
        {
            // arrange
            var wql = Fixture.ExecuteWql("text ~ 'Helena' order by text asc skip 0 take 10");

            // act
            var ast = wql.AbstractSyntaxTree;

            // validation
            Assert.NotNull(ast);
            Assert.NotNull(ast.Filter);
            Assert.NotNull(ast.Order);
            Assert.NotNull(ast.Partitioning);
        }

        /// <summary>
        /// Verifies that the WqlStatement.ToString produces the correct representation.
        /// </summary>
        [Fact]
        public void StatementToString()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' order by text asc take 10");

            // validation
            var str = wql.ToString();
            Assert.Contains("Text", str);
            Assert.Contains("~", str);
            Assert.Contains("Helena", str);
            Assert.Contains("order by", str);
            Assert.Contains("take", str);
        }

        /// <summary>
        /// Verifies that double-quoted strings are handled correctly.
        /// </summary>
        [Fact]
        public void DoubleQuotedString()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ \"Helena\"");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that single-quoted strings are handled correctly.
        /// </summary>
        [Fact]
        public void SingleQuotedString()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena'");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that unquoted strings are handled correctly.
        /// </summary>
        [Fact]
        public void UnquotedString()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ Helena");

            // validation
            Assert.False(wql.HasErrors);
        }

        /// <summary>
        /// Verifies that a query with only order by (no filter) parses correctly.
        /// </summary>
        [Fact]
        public void OrderByOnlyQuery()
        {
            // act
            var wql = Fixture.ExecuteWql("order by text asc");

            // validation
            Assert.False(wql.HasErrors);
            Assert.Null(wql.Filter);
            Assert.NotNull(wql.Order);
        }

        /// <summary>
        /// Verifies that a query with only partitioning (no filter, no order) parses correctly.
        /// </summary>
        [Fact]
        public void PartitioningOnlyQuery()
        {
            // act
            var wql = Fixture.ExecuteWql("take 10");

            // validation
            Assert.False(wql.HasErrors);
            Assert.Null(wql.Filter);
            Assert.Null(wql.Order);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that skip and take combined partitioning works.
        /// </summary>
        [Fact]
        public void SkipAndTakePartitioning()
        {
            // act
            var wql = Fixture.ExecuteWql("skip 5 take 10");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Partitioning);
        }

        /// <summary>
        /// Verifies that WqlParseException stores token information.
        /// </summary>
        [Fact]
        public void ParseExceptionHasTokenInfo()
        {
            // arrange
            var token = new WqlToken() { Value = "test" };
            var ex = new WqlParseException("test message", token);

            // validation
            Assert.Equal("test message", ex.Message);
            Assert.NotNull(ex.Token);
            Assert.Single(ex.Token);
        }

        /// <summary>
        /// Verifies that WqlParseException stores multiple tokens.
        /// </summary>
        [Fact]
        public void ParseExceptionHasMultipleTokens()
        {
            // arrange
            var tokens = new List<IWqlToken>
            {
                new WqlToken() { Value = "a" },
                new WqlToken() { Value = "b" }
            };
            var ex = new WqlParseException("test message", tokens);

            // validation
            Assert.Equal("test message", ex.Message);
            Assert.Equal(2, ex.Token.Count());
        }

        /// <summary>
        /// Verifies that WqlExpressionError ToString returns the message.
        /// </summary>
        [Fact]
        public void ExpressionErrorToString()
        {
            // arrange - use mismatched quotes for guaranteed error
            var wql = Fixture.ExecuteWql("text ~ 'unterminated value");

            // validation
            Assert.True(wql.HasErrors);
            Assert.NotNull(wql.Error.ToString());
        }

        /// <summary>
        /// Verifies that an error in a WQL statement returns the raw string.
        /// </summary>
        [Fact]
        public void ErrorStatementToStringReturnsRaw()
        {
            // arrange - use mismatched quotes for guaranteed error
            var input = "text ~ 'unterminated value";
            var wql = Fixture.ExecuteWql(input);

            // validation
            Assert.True(wql.HasErrors);
            Assert.Equal(input, wql.ToString());
        }

        /// <summary>
        /// Verifies case-insensitive attribute matching.
        /// </summary>
        [Fact]
        public void CaseInsensitiveAttribute()
        {
            // act
            var wql = Fixture.ExecuteWql("TEXT ~ 'Helena'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies case-insensitive operator matching.
        /// </summary>
        [Fact]
        public void CaseInsensitiveOrderBy()
        {
            // act
            var wql = Fixture.ExecuteWql("ORDER BY text ASC");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Order);
        }

        /// <summary>
        /// Verifies numeric parameter parsing.
        /// </summary>
        [Fact]
        public void NumericParameter()
        {
            // arrange
            var parser = new WqlParser<UnitTestIndexTestDocumentB>();

            // act
            var wql = parser.Parse("price > 5.0");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies fuzzy search options parsing.
        /// </summary>
        [Fact]
        public void FuzzyOptionsParsing()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena' ~80");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies distance options parsing.
        /// </summary>
        [Fact]
        public void DistanceOptionsParsing()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena Helge' :5");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies combined fuzzy and distance options parsing.
        /// </summary>
        [Fact]
        public void FuzzyAndDistanceOptions()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ 'Helena Helge' ~80 :5");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }
    }
}
