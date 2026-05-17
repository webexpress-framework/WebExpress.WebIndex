using System.Linq.Expressions;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Function;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests for WQL functions: now(), day(), upper(), lower(), len(), trim(), year(), month().
    /// </summary>
    public class UnitTestWqlFunctions(UnitTestIndexFixtureWqlA fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlA>
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
        /// Verifies the upper() function parses and executes correctly.
        /// </summary>
        [Fact]
        public void UpperFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ upper('hello')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies the upper() function produces the correct value.
        /// </summary>
        [Fact]
        public void UpperFunctionExecute()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionUpper<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    StringValue = "hello world"
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal("HELLO WORLD", result);
        }

        /// <summary>
        /// Verifies the lower() function parses and executes correctly.
        /// </summary>
        [Fact]
        public void LowerFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ lower('HELLO')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies the lower() function produces the correct value.
        /// </summary>
        [Fact]
        public void LowerFunctionExecute()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLower<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    StringValue = "HELLO WORLD"
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal("hello world", result);
        }

        /// <summary>
        /// Verifies the len() function parses correctly.
        /// </summary>
        [Fact]
        public void LenFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ len('hello')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies the len() function produces the correct value.
        /// </summary>
        [Fact]
        public void LenFunctionExecute()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLen<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    StringValue = "hello"
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal(5.0, result);
        }

        /// <summary>
        /// Verifies the trim() function parses correctly.
        /// </summary>
        [Fact]
        public void TrimFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ trim('  hello  ')");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies the trim() function produces the correct value.
        /// </summary>
        [Fact]
        public void TrimFunctionExecute()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionTrim<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    StringValue = "  hello  "
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal("hello", result);
        }

        /// <summary>
        /// Verifies the now() function parses and returns a DateTime.
        /// </summary>
        [Fact]
        public void NowFunctionExecute()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionNow<UnitTestIndexTestDocumentA>();

            // act
            var result = func.Execute();

            // validation
            Assert.IsType<DateTime>(result);
            Assert.True(((DateTime)result - DateTime.Now).TotalSeconds < 1);
        }

        /// <summary>
        /// Verifies the day() function returns DateTime.Today without offset.
        /// </summary>
        [Fact]
        public void DayFunctionNoOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionDay<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal(DateTime.Now.Date, result);
        }

        /// <summary>
        /// Verifies the day() function returns DateTime.Today with a day offset.
        /// </summary>
        [Fact]
        public void DayFunctionWithOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionDay<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    NumberValue = -5
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal(DateTime.Now.Date.AddDays(-5), result);
        }

        /// <summary>
        /// Verifies the year() function returns the current year.
        /// </summary>
        [Fact]
        public void YearFunctionNoOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionYear<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal((double)DateTime.Now.Year, result);
        }

        /// <summary>
        /// Verifies the year() function returns offset year.
        /// </summary>
        [Fact]
        public void YearFunctionWithOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionYear<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    NumberValue = -1
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal((double)(DateTime.Now.Year - 1), result);
        }

        /// <summary>
        /// Verifies the month() function returns the current month.
        /// </summary>
        [Fact]
        public void MonthFunctionNoOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionMonth<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal((double)DateTime.Now.Month, result);
        }

        /// <summary>
        /// Verifies the month() function with offset.
        /// </summary>
        [Fact]
        public void MonthFunctionWithOffset()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionMonth<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    NumberValue = -1
                }
            }];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal((double)DateTime.Now.AddMonths(-1).Month, result);
        }

        /// <summary>
        /// Verifies the year() function parses correctly in a WQL query.
        /// </summary>
        [Fact]
        public void YearFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ year()");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the year() function expression is evaluated dynamically.
        /// </summary>
        [Fact]
        public void YearFunctionToExpressionIsDynamic()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionYear<UnitTestIndexTestDocumentA>();
            func.Parameters = [];
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentA), "x");

            // act
            var expr = func.ToExpression(param);

            // validation
            Assert.NotEqual(ExpressionType.Constant, expr.NodeType);
            Assert.Equal(typeof(double), expr.Type);
        }

        /// <summary>
        /// Verifies the month() function parses correctly in a WQL query.
        /// </summary>
        [Fact]
        public void MonthFunctionParse()
        {
            // act
            var wql = Fixture.ExecuteWql("text ~ month()");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that the month() function expression is evaluated dynamically.
        /// </summary>
        [Fact]
        public void MonthFunctionToExpressionIsDynamic()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionMonth<UnitTestIndexTestDocumentA>();
            func.Parameters = [];
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentA), "x");

            // act
            var expr = func.ToExpression(param);

            // validation
            Assert.NotEqual(ExpressionType.Constant, expr.NodeType);
            Assert.Equal(typeof(double), expr.Type);
        }

        /// <summary>
        /// Verifies that upper() evaluates its parameter expression at runtime.
        /// </summary>
        [Fact]
        public void UpperFunctionToExpressionUsesRuntimeValue()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionUpper<UnitTestIndexTestDocumentA>();
            func.Parameters = [CreateRuntimeTextParameter()];

            // act
            var compiled = CompileRuntimeFunction<string>(func);
            var result = compiled(new UnitTestIndexTestDocumentA() { Text = "hello runtime" });

            // validation
            Assert.Equal("HELLO RUNTIME", result);
        }

        /// <summary>
        /// Verifies that lower() evaluates its parameter expression at runtime.
        /// </summary>
        [Fact]
        public void LowerFunctionToExpressionUsesRuntimeValue()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLower<UnitTestIndexTestDocumentA>();
            func.Parameters = [CreateRuntimeTextParameter()];

            // act
            var compiled = CompileRuntimeFunction<string>(func);
            var result = compiled(new UnitTestIndexTestDocumentA() { Text = "HELLO RUNTIME" });

            // validation
            Assert.Equal("hello runtime", result);
        }

        /// <summary>
        /// Verifies that len() evaluates its parameter expression at runtime.
        /// </summary>
        [Fact]
        public void LenFunctionToExpressionUsesRuntimeValue()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLen<UnitTestIndexTestDocumentA>();
            func.Parameters = [CreateRuntimeTextParameter()];

            // act
            var compiled = CompileRuntimeFunction<double>(func);
            var result = compiled(new UnitTestIndexTestDocumentA() { Text = "hello" });

            // validation
            Assert.Equal(5.0, result);
        }

        /// <summary>
        /// Verifies that trim() evaluates its parameter expression at runtime.
        /// </summary>
        [Fact]
        public void TrimFunctionToExpressionUsesRuntimeValue()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionTrim<UnitTestIndexTestDocumentA>();
            func.Parameters = [CreateRuntimeTextParameter()];

            // act
            var compiled = CompileRuntimeFunction<string>(func);
            var result = compiled(new UnitTestIndexTestDocumentA() { Text = "  hello runtime  " });

            // validation
            Assert.Equal("hello runtime", result);
        }

        /// <summary>
        /// Verifies the upper() function string representation.
        /// </summary>
        [Fact]
        public void UpperFunctionToString()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionUpper<UnitTestIndexTestDocumentA>();
            func.Parameters = [new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>
            {
                Value = new WqlExpressionNodeValue<UnitTestIndexTestDocumentA>
                {
                    StringValue = "test"
                }
            }];

            // act
            var result = func.ToString();

            // validation
            Assert.Equal("upper('test')", result);
        }

        /// <summary>
        /// Verifies the now() function string representation.
        /// </summary>
        [Fact]
        public void NowFunctionToString()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionNow<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.ToString();

            // validation
            Assert.Equal("now()", result);
        }

        /// <summary>
        /// Verifies the upper function with null parameters returns null.
        /// </summary>
        [Fact]
        public void UpperFunctionNullInput()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionUpper<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies the lower function with null parameters returns null.
        /// </summary>
        [Fact]
        public void LowerFunctionNullInput()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLower<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies the len function with null parameters returns 0.
        /// </summary>
        [Fact]
        public void LenFunctionNullInput()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionLen<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Verifies the trim function with null parameters returns null.
        /// </summary>
        [Fact]
        public void TrimFunctionNullInput()
        {
            // arrange
            var func = new WqlExpressionNodeFilterFunctionTrim<UnitTestIndexTestDocumentA>();
            func.Parameters = [];

            // act
            var result = func.Execute();

            // validation
            Assert.Null(result);
        }

        private static Func<UnitTestIndexTestDocumentA, TResult> CompileRuntimeFunction<TResult>(WqlExpressionNodeFilterFunction<UnitTestIndexTestDocumentA> func)
        {
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentA), "x");
            return Expression.Lambda<Func<UnitTestIndexTestDocumentA, TResult>>(func.ToExpression(param), param).Compile();
        }

        private static WqlExpressionNodeParameter<UnitTestIndexTestDocumentA> CreateRuntimeTextParameter()
        {
            return new WqlExpressionNodeParameter<UnitTestIndexTestDocumentA>()
            {
                Function = new RuntimeTextFunction()
            };
        }

        private sealed class RuntimeTextFunction : WqlExpressionNodeFilterFunction<UnitTestIndexTestDocumentA>
        {
            public RuntimeTextFunction()
                : base("runtimeText")
            {
            }

            public override object Execute()
            {
                return "constant placeholder";
            }

            public override Expression ToExpression(ParameterExpression param)
            {
                return Expression.Property(param, nameof(UnitTestIndexTestDocumentA.Text));
            }
        }
    }
}
