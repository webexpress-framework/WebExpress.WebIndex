using System.Globalization;
using System.Linq.Expressions;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Condition;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the extended expression model: functions on the left-hand side of a condition,
    /// attributes as parameters and the scalar functions, whose value must be the same in
    /// the reverse index and in a query.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestWqlExpressions(UnitTestIndexFixtureWqlG fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlG>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlG Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests conditions that compare the value of a function with a value.
        /// </summary>
        [Fact]
        public void FunctionOnTheLeft()
        {
            Fixture.AssertPaths("len(Text) > 10", x => (x.Text?.Length ?? 0) > 10);
            Fixture.AssertPaths("len(trim(Text)) = 4", x => (x.Text?.Trim().Length ?? 0) == 4);
            Fixture.AssertPaths("coalesce(Text, '') = ''", x => string.IsNullOrEmpty(x.Text));
            Fixture.AssertPaths("coalesce(Optional, 0) = 0", x => (x.Optional ?? 0) == 0);
            Fixture.AssertPaths("upper(Text) = 'alpha'", [5]);
            Fixture.AssertPaths("lower(Text) starts with 'char'", [4]);
            Fixture.AssertPaths("abs(Number) = 4", [4, 11]);
            Fixture.AssertPaths("floor(Number) = 3", [3]);
            Fixture.AssertPaths("ceil(Optional) >= 13", x => x.Optional >= 13);
            Fixture.AssertPaths("round(Price, 0) = 12", [2, 11]);
            Fixture.AssertPaths("yearof(Date) = 2023", [11]);
            Fixture.AssertPaths("monthof(Date) = 2", x => x.Date.Month == 2);
            Fixture.AssertPaths("dayof(Date) = 15", x => x.Date.Day == 15);
            Fixture.AssertPaths("datediff('day', Date, date('2024-03-01')) between 0 and 30", x => (new DateTime(2024, 3, 1) - x.Date.Date).Days is >= 0 and <= 30);
            Fixture.AssertPaths("dateadd('month', 1, Date) < date('2024-03-01')", x => x.Date.AddMonths(1) < new DateTime(2024, 3, 1));
            Fixture.AssertPaths("concat(Text, '!') ends with 'o!'", [12, 16]);
            Fixture.AssertPaths("replace(Text, 'alpha', 'omega') ~ 'omega'", [1, 2, 3, 5, 11, 15]);
            Fixture.AssertPaths("substring(Text, 1, 5) = 'alpha'", x => x.Text?.Length >= 5 && x.Text[..5].Equals("alpha", StringComparison.OrdinalIgnoreCase));
            Fixture.AssertPaths("nullif(Number, 3) is null", [3]);
            Fixture.AssertPaths("len(Text) > 3 and Number < 100", x => (x.Text?.Length ?? 0) > 3 && x.Number < 100);
            Fixture.AssertPaths("not len(Text) > 3", x => (x.Text?.Length ?? 0) <= 3);
        }

        /// <summary>
        /// Tests conditions that compare two attributes of the same item.
        /// </summary>
        [Fact]
        public void AttributeParameters()
        {
            Fixture.AssertPaths("Number = Optional", x => x.Number == x.Optional);
            Fixture.AssertPaths("Number > Optional", x => x.Number > x.Optional);
            Fixture.AssertPaths("Number != Optional", x => x.Number != x.Optional);
            Fixture.AssertPaths("Price = Number", x => x.Price == x.Number);
            Fixture.AssertPaths("Number between Optional and 10", x => x.Number >= x.Optional && x.Number <= 10);
            Fixture.AssertPaths("Optional in (Number, 0)", x => x.Optional == x.Number || x.Optional == 0);
            Fixture.AssertPaths("abs(Number) = Optional", x => Math.Abs(x.Number) == x.Optional);

            Assert.Equal("Number = Optional", Fixture.ExecuteWql("Number = optional").ToString());
            Assert.Equal("len(Text) > Number", Fixture.ExecuteWql("len(text) > number").ToString());
        }

        /// <summary>
        /// Tests the values of the functions, computed once by executing the function and once
        /// by the expression tree, which must agree.
        /// </summary>
        [Theory]
        [InlineData("substring('abcdef', 2, 3)", "bcd")]
        [InlineData("substring('abcdef', 0, 3)", "ab")]
        [InlineData("substring('abc', 2)", "bc")]
        [InlineData("substring('abc', 5)", "")]
        [InlineData("substring('abc', 1, -1)", "null")]
        [InlineData("substring(null, 1)", "null")]
        [InlineData("concat('a', null, 'b', 1)", "ab1")]
        [InlineData("replace('Alpha alpha', 'ALPHA', 'x')", "x x")]
        [InlineData("replace('abc', '', 'x')", "abc")]
        [InlineData("coalesce(null, 'b', 'c')", "b")]
        [InlineData("coalesce(null, null)", "null")]
        [InlineData("nullif('a', 'A')", "null")]
        [InlineData("nullif(1, 2)", "1")]
        [InlineData("abs(-2.5)", "2.5")]
        [InlineData("abs('x')", "null")]
        [InlineData("round(2.5)", "3")]
        [InlineData("round(-2.5)", "-3")]
        [InlineData("round(1.25, 1)", "1.3")]
        [InlineData("floor(-1.5)", "-2")]
        [InlineData("ceil(1.2)", "2")]
        [InlineData("len('hello')", "5")]
        [InlineData("len(null)", "0")]
        [InlineData("upper('abc')", "ABC")]
        [InlineData("trim('  a  ')", "a")]
        [InlineData("date('2024-02-29')", "2024-02-29 00:00:00")]
        [InlineData("date('2024-02-29T13:45')", "2024-02-29 13:45:00")]
        [InlineData("date('2024-02-30')", "null")]
        [InlineData("date('29.02.2024')", "null")]
        [InlineData("dateadd('month', 1, '2024-01-31')", "2024-02-29 00:00:00")]
        [InlineData("dateadd('day', -1.9, '2024-03-01')", "2024-02-29 00:00:00")]
        [InlineData("dateadd('YEAR', 1, date('2024-02-29'))", "2025-02-28 00:00:00")]
        [InlineData("dateadd('second', 30, '2024-01-01 23:59:45')", "2024-01-02 00:00:15")]
        [InlineData("dateadd('week', 1, '2024-01-01')", "null")]
        [InlineData("dateadd('1', 1, '2024-01-01')", "null")]
        [InlineData("datediff('year', '2023-12-31', '2024-01-01')", "1")]
        [InlineData("datediff('month', '2024-01-31', '2024-02-01')", "1")]
        [InlineData("datediff('day', '2024-03-01', '2024-02-28')", "-2")]
        [InlineData("datediff('hour', '2024-01-01 10:59', '2024-01-01 11:00')", "1")]
        [InlineData("datediff('minute', '2024-01-01 10:59:59', '2024-01-01 11:00:00')", "1")]
        [InlineData("yearof('2024-05-06')", "2024")]
        [InlineData("monthof('2024-05-06')", "5")]
        [InlineData("dayof('2024-05-06')", "6")]
        [InlineData("dayof(null)", "null")]
        public void FunctionValue(string function, string expected)
        {
            var node = ParseFunction(function);
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentG), "x");
            var compiled = Expression.Lambda<Func<UnitTestIndexTestDocumentG, object>>(Expression.Convert(node.ToExpression(param), typeof(object)), param).Compile();

            Assert.Equal(expected, Format(node.Execute()));
            Assert.Equal(expected, Format(compiled(new UnitTestIndexTestDocumentG())));
        }

        /// <summary>
        /// Tests that the existing functions keep their meaning on the new base class.
        /// </summary>
        [Fact]
        public void ExistingFunctions()
        {
            Assert.Equal(DateTime.Today.AddDays(-3), ParseFunction("day(-3)").Execute());
            Assert.Equal(DateTime.Today, ParseFunction("day()").Execute());
            Assert.Equal((double)DateTime.Now.Year + 1, ParseFunction("year(1)").Execute());
            Assert.Equal((double)DateTime.Now.AddMonths(-1).Month, ParseFunction("month(-1)").Execute());
            Assert.Equal("ab", ParseFunction("lower('AB')").Execute());

            foreach (var name in new[] { "day", "now", "month", "year", "len", "upper", "lower", "trim" })
            {
                Assert.IsAssignableFrom<WqlExpressionNodeFilterFunctionScalar<UnitTestIndexTestDocumentG>>(ParseFunction($"{name}('1')", false));
            }
        }

        /// <summary>
        /// Tests that a time-dependent function is evaluated when the query runs and not when
        /// it is built.
        /// </summary>
        [Fact]
        public void FunctionsEvaluateAtRunTime()
        {
            var node = ParseFunction("now()");
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentG), "x");
            var compiled = Expression.Lambda<Func<UnitTestIndexTestDocumentG, DateTime>>(node.ToExpression(param), param).Compile();

            var first = compiled(new UnitTestIndexTestDocumentG());
            Thread.Sleep(20);
            var second = compiled(new UnitTestIndexTestDocumentG());

            Assert.True(second > first);
        }

        /// <summary>
        /// Tests that the number of arguments is checked against the range a function declares.
        /// </summary>
        [Theory]
        [InlineData("len() > 3", 0)]
        [InlineData("Date < now(1)", 7)]
        [InlineData("Text = substring('a')", 7)]
        [InlineData("coalesce(Text) = 'a'", 0)]
        [InlineData("Text = replace('a', 'b')", 7)]
        [InlineData("Text = upper(lower())", 13)]
        [InlineData("Number in (1, round())", 14)]
        public void Arity(string wql, int position)
        {
            var statement = Fixture.ExecuteWql(wql);

            Assert.True(statement.HasErrors);
            Assert.Equal("webexpress.webindex:wql.function_arity", statement.Error.Message);
            Assert.Equal(position, statement.Error.Position);
        }

        /// <summary>
        /// Tests that a function written against the original contract keeps working: it
        /// declares no range of arguments and therefore accepts any number.
        /// </summary>
        [Fact]
        public void CustomFunction()
        {
            var parser = new WqlParser<UnitTestIndexTestDocumentG>();
            parser.RegisterFunction<LegacyFunction>();

            var none = parser.Parse("Text ~ legacy()");
            var many = parser.Parse("Text ~ legacy(1, 2, 3, 4)");

            Assert.False(none.HasErrors);
            Assert.False(many.HasErrors);
            Assert.Equal(Fixture.RetrieveIndexed(many).Order(), Fixture.RetrieveQueried(many).Order());
            Assert.NotEmpty(Fixture.RetrieveIndexed(many));
        }

        /// <summary>
        /// Parses a function from a statement that compares it with the text.
        /// </summary>
        /// <param name="function">The function call.</param>
        /// <param name="valid">True to assert that the statement is free of errors.</param>
        /// <returns>The function node.</returns>
        private WqlExpressionNodeFilterFunction<UnitTestIndexTestDocumentG> ParseFunction(string function, bool valid = true)
        {
            var statement = Fixture.ExecuteWql($"Text = {function}");

            Assert.True(!valid || !statement.HasErrors, $"'{function}' reports {statement.Error}");

            return ((WqlExpressionNodeFilterConditionBinary<UnitTestIndexTestDocumentG>)statement.Filter.Condition).Parameter.Function;
        }

        /// <summary>
        /// Formats a function value for comparison.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The text of the value.</returns>
        private static string Format(object value)
        {
            return value switch
            {
                null => "null",
                DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// A function written against the original contract, without the scalar base class.
        /// </summary>
        public sealed class LegacyFunction : WqlExpressionNodeFilterFunction<UnitTestIndexTestDocumentG>
        {
            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            public LegacyFunction()
                : base("legacy")
            {
            }

            /// <summary>
            /// Executes the function.
            /// </summary>
            /// <returns>A constant word.</returns>
            public override object Execute()
            {
                return "alpha";
            }

            /// <summary>
            /// Builds the expression of the function.
            /// </summary>
            /// <param name="param">The parameter expression representing the index item.</param>
            /// <returns>A constant word.</returns>
            public override Expression ToExpression(ParameterExpression param)
            {
                return Expression.Constant("alpha");
            }
        }
    }
}
