using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Condition;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the literals of the parser, its semantic check with source positions and its
    /// promise never to throw.
    /// </summary>
    public class UnitTestWqlSemantics
    {
        /// <summary>
        /// Tests that the unquoted literals null, true and false are typed values, while their
        /// quoted forms remain strings.
        /// </summary>
        [Fact]
        public void TypedLiterals()
        {
            Assert.True(Value("Text ~ null").IsNull);
            Assert.Equal("null", Value("Text ~ 'null'").StringValue);
            Assert.True(Value("Active = true").BoolValue);
            Assert.False(Value("Active = FALSE").BoolValue);
            Assert.Equal("true", Value("Active = 'true'").StringValue);
            Assert.Equal(5d, Value("Number = 5").NumberValue);
            Assert.Equal("", Value("Text = ''").StringValue);

            Assert.Equal("Text ~ null", Parse("Text ~ null").ToString());
            Assert.Equal("Active = true", Parse("Active = true").ToString());
            Assert.Equal("Text ~ 'null'", Parse("Text ~ 'null'").ToString());
        }

        /// <summary>
        /// Tests that a doubled quotation mark stands for the quotation mark itself.
        /// </summary>
        [Fact]
        public void EscapedQuotes()
        {
            Assert.Equal("O'Brien", Value("Text = 'O''Brien'").StringValue);
            Assert.Equal("'", Value("Text = ''''").StringValue);
            Assert.Equal("say \"hi\"", Value("Text = \"say \"\"hi\"\"\"").StringValue);
            Assert.Equal("a'b", Value("Text in ('a''b', 'c')").StringValue);
            Assert.Equal("Text = 'O''Brien'", Parse("Text = 'O''Brien'").ToString());
            Assert.False(Parse("Text = '' and Number = 1").HasErrors);
        }

        /// <summary>
        /// Tests that integral literals convert exactly and refuse values out of range.
        /// </summary>
        [Fact]
        public void NumberLiterals()
        {
            Assert.False(Parse("Number = 2147483647").HasErrors);
            Assert.True(Parse("Number = 2147483648").HasErrors);
            Assert.False(Parse("Price = 0.1").HasErrors);
            Assert.False(Parse("Number = 5.0").HasErrors);
            Assert.True(Parse("Number = 5.5").HasErrors);
        }

        /// <summary>
        /// Tests the semantic errors and their position in the statement.
        /// </summary>
        [Theory]
        [InlineData("Nonexistent = 1", "attribute_unknown", 0, 11)]
        [InlineData("Address.Nonexistent = 1", "attribute_unknown", 0, 19)]
        [InlineData("Text ~ 'a' order by Nonexistent", "attribute_unknown", 20, 11)]
        [InlineData("Number = 5.5", "literal_not_convertible", 9, 3)]
        [InlineData("Number = 'abc'", "literal_not_convertible", 10, 3)]
        [InlineData("Date = 'tomorrow'", "literal_not_convertible", 8, 8)]
        [InlineData("Reference = 'xyz'", "literal_not_convertible", 13, 3)]
        [InlineData("State = Unknown", "literal_not_convertible", 8, 7)]
        [InlineData("Active = 'yes'", "literal_not_convertible", 10, 3)]
        [InlineData("Number in (1, 'x')", "literal_not_convertible", 15, 1)]
        [InlineData("Number > 5 ~80", "option_not_applicable", 11, 1)]
        [InlineData("Number = 5 :2", "option_not_applicable", 11, 1)]
        [InlineData("Text is null :2", "option_not_applicable", 13, 1)]
        [InlineData("Active is 'yes'", "invalid_is_value", 11, 3)]
        [InlineData("Number is true", "invalid_is_value", 10, 4)]
        [InlineData("Text is 5", "invalid_is_value", 8, 1)]
        [InlineData("Number ~ 'a'", "text_required", 7, 1)]
        [InlineData("Date ends with '1'", "text_required", 5, 9)]
        [InlineData("len(Text) ~ 'a'", "text_required", 10, 1)]
        [InlineData("len() > 3", "function_arity", 0, 3)]
        [InlineData("Text = now(1)", "function_arity", 7, 3)]
        public void SemanticErrors(string wql, string message, int position, int length)
        {
            var statement = Parse(wql);

            Assert.True(statement.HasErrors);
            Assert.Equal($"webexpress.webindex:wql.{message}", statement.Error.Message);
            Assert.Equal(position, statement.Error.Position);
            Assert.Equal(length, statement.Error.Length);
        }

        /// <summary>
        /// Tests valid statements that resemble the erroneous ones.
        /// </summary>
        [Theory]
        [InlineData("Text = 'a' ~80 :2")]
        [InlineData("Text != 'a b' :1")]
        [InlineData("Text ~ 'a' ~80")]
        [InlineData("Active is true")]
        [InlineData("Optional is null")]
        [InlineData("Address.Zip = null")]
        [InlineData("Text is not null")]
        [InlineData("State = closed")]
        [InlineData("Reference = 10000000-0000-0000-0000-000000000001")]
        [InlineData("Date = 2024-01-15")]
        [InlineData("Number = '7'")]
        [InlineData("lower(Text) ~ 'a'")]
        [InlineData("coalesce(Text, Number) starts with '1'")]
        public void SemanticallyValid(string wql)
        {
            var statement = Parse(wql);

            Assert.False(statement.HasErrors, statement.Error?.ToString());
        }

        /// <summary>
        /// Tests that the first of several errors is reported.
        /// </summary>
        [Fact]
        public void FirstErrorIsReported()
        {
            var statement = Parse("Number = 'x' and Nonexistent = 1");

            Assert.Equal("webexpress.webindex:wql.literal_not_convertible", statement.Error.Message);
            Assert.Equal(10, statement.Error.Position);
        }

        /// <summary>
        /// Tests that a semantic error leaves the lookahead analysis untouched, since a
        /// statement being typed passes through unknown attributes.
        /// </summary>
        [Fact]
        public void SemanticErrorsDoNotStopTheAnalysis()
        {
            var parser = new WqlParser<UnitTestIndexTestDocumentG>();

            Assert.True(parser.Analyze("Nonexistent = 1").IsValidSoFar);
            Assert.True(parser.Analyze("Number = 5.5 order by Nonexistent").IsValidSoFar);
        }

        /// <summary>
        /// Tests that parsing never throws, whatever the input.
        /// </summary>
        [Theory]
        [InlineData("((((")]
        [InlineData("'")]
        [InlineData("\"")]
        [InlineData(")")]
        [InlineData("Number =")]
        [InlineData("Text = 'a' order by")]
        [InlineData("order by")]
        [InlineData("Number between")]
        [InlineData("len(")]
        [InlineData("not not")]
        [InlineData("Text ~ 'a' ~")]
        [InlineData("x.y.z = 1")]
        [InlineData("Address. = 1")]
        [InlineData("...")]
        [InlineData("Number = 99999999999999999999999999999999")]
        [InlineData("take 99999999999")]
        [InlineData("Text = 'a' and")]
        [InlineData("= = =")]
        [InlineData("Text in ()")]
        [InlineData("unknown(1) = 1")]
        [InlineData("Number between 1 and")]
        [InlineData("()")]
        public void ParseNeverThrows(string wql)
        {
            var parser = new WqlParser<UnitTestIndexTestDocumentG>();

            var statement = parser.Parse(wql);
            var lookahead = parser.Analyze(wql);

            Assert.NotNull(statement);
            Assert.True(statement.HasErrors);
            Assert.NotNull(lookahead);
        }

        /// <summary>
        /// Parses a statement over document G.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <returns>The parsed statement.</returns>
        private static IWqlStatement<UnitTestIndexTestDocumentG> Parse(string wql)
        {
            return new WqlParser<UnitTestIndexTestDocumentG>().Parse(wql);
        }

        /// <summary>
        /// Returns the first literal of a statement with a binary or set condition.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <returns>The literal.</returns>
        private static WqlExpressionNodeValue<UnitTestIndexTestDocumentG> Value(string wql)
        {
            return Parse(wql).Filter.Condition switch
            {
                WqlExpressionNodeFilterConditionBinary<UnitTestIndexTestDocumentG> binary => binary.Parameter.Value,
                WqlExpressionNodeFilterConditionSet<UnitTestIndexTestDocumentG> set => set.Parameters.First().Value,
                _ => null
            };
        }
    }
}
