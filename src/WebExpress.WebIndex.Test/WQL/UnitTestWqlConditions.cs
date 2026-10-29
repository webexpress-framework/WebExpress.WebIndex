using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentFactoryG;
using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentG;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the conditions between, not between, starts with, ends with, contains and !~,
    /// the unary not and the typed set conditions, each through the reverse index and as a query.
    /// </summary>
    public class UnitTestWqlConditions(UnitTestIndexFixtureWqlG fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlG>
    {
        private static readonly int[] Alpha = [1, 2, 3, 5, 11, 15];

        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlG Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests ranges with both bounds included, on numbers through the numeric index and
        /// on other types through a scan.
        /// </summary>
        [Fact]
        public void Between()
        {
            Fixture.AssertPaths("Number between 3 and 6", x => x.Number >= 3 && x.Number <= 6);
            Fixture.AssertPaths("Number not between 3 and 11000", x => !(x.Number >= 3 && x.Number <= 11000));
            Fixture.AssertPaths("Price between 10 and 20", x => x.Price >= 10 && x.Price <= 20);
            Fixture.AssertPaths("Optional between 2 and 4", x => x.Optional >= 2 && x.Optional <= 4);
            Fixture.AssertPaths("Optional not between 2 and 4", x => !(x.Optional >= 2 && x.Optional <= 4));
            Fixture.AssertPaths("Date between '2024-01-01' and '2024-02-01'", x => x.Date >= new DateTime(2024, 1, 1) && x.Date <= new DateTime(2024, 2, 1));
            Fixture.AssertPaths("Text between 'b' and 'd'", x => x.Text is not null && string.Compare(x.Text, "b", StringComparison.OrdinalIgnoreCase) >= 0 && string.Compare(x.Text, "d", StringComparison.OrdinalIgnoreCase) <= 0);
            Fixture.AssertPaths("State between Pending and Closed", x => x.State is StateKind.Pending or StateKind.Closed);
            Fixture.AssertPaths("Address.Zip between 10000 and 20000", x => x.Address?.Zip >= 10000 && x.Address?.Zip <= 20000);
            Fixture.AssertNone("Number between 6 and 3");
        }

        /// <summary>
        /// Tests that the and between the bounds belongs to the range and not to the logical
        /// operators.
        /// </summary>
        [Fact]
        public void BetweenWithinLogicalOperators()
        {
            Fixture.AssertPaths("Number between 3 and 6 and Active = true", x => x.Number >= 3 && x.Number <= 6 && x.Active);
            Fixture.AssertPaths("Number between 3 and 6 or Number between 10 and 12", x => x.Number is >= 3 and <= 6 or >= 10 and <= 12);
            Fixture.AssertPaths("Active = true and Number between 3 and 6 or Number < 0", x => x.Active && x.Number >= 3 && x.Number <= 6 || x.Number < 0);

            Assert.Equal("(Number between 3 and 6 and Active = true)", Fixture.ExecuteWql("Number between 3 and 6 and Active = true").ToString());
        }

        /// <summary>
        /// Tests the substring conditions, which match characters anywhere, also inside a
        /// word, and ignore case.
        /// </summary>
        [Fact]
        public void Substrings()
        {
            Fixture.AssertPaths("Text starts with 'alp'", x => x.Text?.StartsWith("alp", StringComparison.OrdinalIgnoreCase) == true);
            Fixture.AssertPaths("Text ends with 'VO'", x => x.Text?.EndsWith("vo", StringComparison.OrdinalIgnoreCase) == true);
            Fixture.AssertPaths("Text contains 'lph'", x => x.Text?.Contains("lph", StringComparison.OrdinalIgnoreCase) == true);
            Fixture.AssertPaths("Text contains 'é'", [14]);
            Fixture.AssertPaths("Text starts with ' '", [13]);
            Fixture.AssertPaths("Address.City starts with 'ber'", x => x.Address?.City?.StartsWith("ber", StringComparison.OrdinalIgnoreCase) == true);
            Fixture.AssertPaths("Text contains 'bulk 1000'", x => x.Text?.Contains("bulk 1000") == true);

            // the word search does not look inside words
            Fixture.AssertNone("Text ~ 'lph'");
        }

        /// <summary>
        /// Tests the negated word search, which keeps the items without a text.
        /// </summary>
        [Fact]
        public void NotLike()
        {
            Fixture.AssertPaths("Text !~ 'alpha'", x => !Alpha.Select(Id).Contains(x.Id));
            Fixture.AssertPaths("Text !~ 'al*'", x => !Alpha.Append(12).Select(Id).Contains(x.Id));
            Fixture.AssertPaths("Text !~ 'bulk'", x => x.Number < 1000);
            Fixture.AssertPaths("Text !~ 'alpha charlie' :2", x => !new[] { 1, 3, 15 }.Select(Id).Contains(x.Id));
        }

        /// <summary>
        /// Tests the unary not on conditions and groups.
        /// </summary>
        [Fact]
        public void Not()
        {
            Fixture.AssertPaths("not Active = true", x => !x.Active);
            Fixture.AssertPaths("not not Active = true", x => x.Active);
            Fixture.AssertPaths("not (Text ~ 'alpha' or Number > 5)", x => !Alpha.Select(Id).Contains(x.Id) && x.Number <= 5);
            Fixture.AssertPaths("Active = true and not Text ~ 'alpha'", x => x.Active && !Alpha.Select(Id).Contains(x.Id));
            Fixture.AssertPaths("not Number in (1, 2) and Number < 5", x => x.Number is not (1 or 2) && x.Number < 5);
            Fixture.AssertPaths("not Optional > 3", x => !(x.Optional > 3));
            Fixture.AssertPaths("not Text ~ 'bulk'", x => x.Number < 1000);

            Assert.Equal("not (Active = true)", Fixture.ExecuteWql("not Active = true").ToString());
            Assert.Equal("(not (Number = 1 or Number = 2) and Number = 3)", Fixture.ExecuteWql("not (Number = 1 or Number = 2) and Number = 3").ToString());
        }

        /// <summary>
        /// Tests set conditions on nullable, enumeration, Guid and date attributes and with
        /// functions among the values.
        /// </summary>
        [Fact]
        public void TypedSets()
        {
            Fixture.AssertPaths("Optional in (1, null)", x => x.Optional is 1 or null);
            Fixture.AssertPaths("Optional not in (1, null)", x => x.Optional is not (1 or null));
            Fixture.AssertPaths("State in (Open, 2)", x => x.State is StateKind.Open or StateKind.Closed);
            Fixture.AssertPaths($"Reference not in ('{Reference(1)}', '{Reference(2)}')", x => x.Reference != Reference(1) && x.Reference != Reference(2));
            Fixture.AssertPaths("Date in (2024-01-15, '2024-02-15 10:30')", [1, 4]);
            Fixture.AssertPaths("Number in (len('abc'), 7)", x => x.Number is 3 or 7);
            Fixture.AssertPaths("Number in (abs(-4), -4)", x => x.Number is 4 or -4);
        }

        /// <summary>
        /// Tests that the new conditions are checked like the existing ones.
        /// </summary>
        [Theory]
        [InlineData("Number starts with '1'", "webexpress.webindex:wql.text_required", 7)]
        [InlineData("Number !~ 'a'", "webexpress.webindex:wql.text_required", 7)]
        [InlineData("Text contains 'a' ~80", "webexpress.webindex:wql.option_not_applicable", 18)]
        [InlineData("Number between 'a' and 5", "webexpress.webindex:wql.literal_not_convertible", 16)]
        [InlineData("Number between 1 and 5.5", "webexpress.webindex:wql.literal_not_convertible", 21)]
        [InlineData("Number between 1", "webexpress.webindex:wql.expected_token", 0)]
        [InlineData("Number between 1 or 5", "webexpress.webindex:wql.expected_token", 17)]
        public void Errors(string wql, string message, int position)
        {
            var statement = Fixture.ExecuteWql(wql);

            Assert.True(statement.HasErrors);
            Assert.Equal(message, statement.Error.Message);
            Assert.Equal(position, statement.Error.Position);
        }

        /// <summary>
        /// Tests the lookahead analysis of the new syntax.
        /// </summary>
        [Theory]
        [InlineData("Number between", false, WqlExpressionType.Operator, WqlExpressionType.Parameter)]
        [InlineData("Number between 3", false, WqlExpressionType.Parameter, WqlExpressionType.LogicalOperator)]
        [InlineData("Number between 3 and", false, WqlExpressionType.LogicalOperator, WqlExpressionType.Parameter)]
        [InlineData("Number between 3 and 5", true, WqlExpressionType.Parameter, WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.PartitioningOperator)]
        [InlineData("not", false, WqlExpressionType.LogicalOperator, WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis)]
        [InlineData("Text starts with", false, WqlExpressionType.Operator, WqlExpressionType.Parameter)]
        [InlineData("len(Text)", false, WqlExpressionType.CloseParenthesis, WqlExpressionType.Operator)]
        public void Analyze(string wql, bool valid, WqlExpressionType type, params WqlExpressionType[] expectedNextTokens)
        {
            var ila = new WqlParser<UnitTestIndexTestDocumentG>().Analyze(wql);

            Assert.Equal(valid, ila.IsValidSoFar);
            Assert.Equal(type, ila.LastExpressionType);
            Assert.Equal(expectedNextTokens.Order(), ila.ExpectedNextTokens.Order());
        }
    }
}
