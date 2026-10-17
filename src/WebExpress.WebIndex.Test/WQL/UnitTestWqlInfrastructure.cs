using System.Globalization;
using System.Linq.Expressions;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Wql;
using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentG;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests the shared infrastructure of both execution paths: the value converter, the
    /// text matcher and the expression builder.
    /// </summary>
    public class UnitTestWqlInfrastructure
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>
        /// Tests conversions into numeric types, which refuse every loss of information.
        /// </summary>
        [Fact]
        public void ConvertNumbers()
        {
            AssertConverts(5d, typeof(int), 5);
            AssertConverts(5d, typeof(int?), 5);
            AssertConverts(5.5d, typeof(decimal), 5.5m);
            AssertConverts(0.1d, typeof(decimal), 0.1m);
            AssertConverts("42", typeof(long), 42L);
            AssertConverts("4,2", typeof(decimal), 4.2m, CultureInfo.GetCultureInfo("de-DE"));
            AssertConverts(12m, typeof(byte), (byte)12);
            AssertConverts(StateKind.Closed, typeof(int), 2);
            AssertConverts(2.5m, typeof(double), 2.5d);
            AssertConverts(9007199254740993m, typeof(long), 9007199254740993L);

            AssertRefuses(5.5d, typeof(int));
            AssertRefuses(3000000000d, typeof(int));
            AssertRefuses(-1d, typeof(uint));
            AssertRefuses(1e300, typeof(float));
            AssertRefuses(double.NaN, typeof(decimal));
            AssertRefuses("abc", typeof(int));
            AssertRefuses(true, typeof(int));
            AssertRefuses(null, typeof(int));
        }

        /// <summary>
        /// Tests conversions into booleans, dates, Guids, enumerations, text and nullable types.
        /// </summary>
        [Fact]
        public void ConvertOtherTypes()
        {
            AssertConverts("true", typeof(bool), true);
            AssertConverts(" FALSE ", typeof(bool?), false);
            AssertConverts("2024-02-29", typeof(DateTime), new DateTime(2024, 2, 29));
            AssertConverts("29.02.2024", typeof(DateTime), new DateTime(2024, 2, 29), CultureInfo.GetCultureInfo("de-DE"));
            AssertConverts(new DateTime(2024, 2, 29), typeof(DateOnly), new DateOnly(2024, 2, 29));
            AssertConverts("2024-02-29", typeof(DateOnly), new DateOnly(2024, 2, 29));
            AssertConverts("12:30", typeof(TimeOnly), new TimeOnly(12, 30));
            AssertConverts("2024-01-01T10:00:00+02:00", typeof(DateTimeOffset), new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(2)));
            AssertConverts("01:30:00", typeof(TimeSpan), TimeSpan.FromMinutes(90));
            AssertConverts("10000000-0000-0000-0000-000000000001", typeof(Guid), new Guid("10000000-0000-0000-0000-000000000001"));
            AssertConverts("Closed", typeof(StateKind), StateKind.Closed);
            AssertConverts("closed", typeof(StateKind?), StateKind.Closed);
            AssertConverts(1d, typeof(StateKind), StateKind.Pending);
            AssertConverts("2", typeof(StateKind), StateKind.Closed);
            AssertConverts(5d, typeof(string), "5");
            AssertConverts(true, typeof(string), "true");
            AssertConverts("x", typeof(char), 'x');
            AssertConverts(null, typeof(int?), null);
            AssertConverts(null, typeof(string), null);

            AssertRefuses("yes", typeof(bool));
            AssertRefuses(1d, typeof(bool));
            AssertRefuses("2024-02-30", typeof(DateTime));
            AssertRefuses(new DateTime(2024, 2, 29, 12, 0, 0), typeof(DateOnly));
            AssertRefuses("xyz", typeof(Guid));
            AssertRefuses("Unknown", typeof(StateKind));
            AssertRefuses(1.5d, typeof(StateKind));
            AssertRefuses(DayOfWeek.Monday, typeof(StateKind));
            AssertRefuses("xy", typeof(char));
            Assert.Throws<InvalidCastException>(() => WqlValueConverter.Convert("abc", typeof(int), Invariant));
        }

        /// <summary>
        /// Tests the phrase search of the text matcher, including the spacing of the words.
        /// </summary>
        [Fact]
        public void MatchPhrase()
        {
            Assert.True(Match("Alpha Bravo charlie", "alpha bravo", WqlTextMatchMode.Phrase));
            Assert.False(Match("bravo alpha", "alpha bravo", WqlTextMatchMode.Phrase));
            Assert.False(Match("alpha x bravo", "alpha bravo", WqlTextMatchMode.Phrase));
            Assert.True(Match("alpha x bravo", "alpha bravo", WqlTextMatchMode.Phrase, 1));
            // as in the index, a punctuation mark followed by a space takes a position of its own
            Assert.False(Match("alpha, bravo!", "alpha bravo", WqlTextMatchMode.Phrase));
            Assert.True(Match("alpha, bravo!", "alpha bravo", WqlTextMatchMode.Phrase, 1));
            Assert.True(Match("alpha,bravo", "alpha bravo", WqlTextMatchMode.Phrase));
            Assert.True(Match("Crème brûlée", "creme brulee", WqlTextMatchMode.Phrase));
            Assert.False(Match(null, "alpha", WqlTextMatchMode.Phrase));
            Assert.False(Match("alpha", "", WqlTextMatchMode.Phrase));
            Assert.False(Match("alpha", null, WqlTextMatchMode.Phrase));
        }

        /// <summary>
        /// Tests the word search of the text matcher with distance, wildcards and similarity.
        /// </summary>
        [Fact]
        public void MatchWords()
        {
            Assert.True(Match("bravo alpha", "alpha bravo", WqlTextMatchMode.Words));
            Assert.False(Match("alpha charlie", "alpha bravo", WqlTextMatchMode.Words));
            Assert.True(Match("alpha x y bravo", "bravo alpha", WqlTextMatchMode.Words, 3));
            Assert.False(Match("alpha x y bravo", "bravo alpha", WqlTextMatchMode.Words, 2));
            Assert.True(Match("alphabet", "alpha*", WqlTextMatchMode.Words));
            Assert.True(Match("alpha", "alpha*", WqlTextMatchMode.Words));
            Assert.True(Match("bravo", "br?vo", WqlTextMatchMode.Words));
            Assert.False(Match("brvo", "br?vo", WqlTextMatchMode.Words));
            Assert.True(Match("helena", "helge", WqlTextMatchMode.Words, 0, 50));
            Assert.False(Match("helena", "helge", WqlTextMatchMode.Words, 0, 90));
            Assert.True(Match("price 5.5 eur", "5.5", WqlTextMatchMode.Words));
        }

        /// <summary>
        /// Tests the comparisons of the expression builder, including null and the fallback
        /// for types without an operator.
        /// </summary>
        [Fact]
        public void Compare()
        {
            Assert.True(Evaluate<string>("abc", WqlComparisonOperator.Equal, "ABC"));
            Assert.True(Evaluate<string>("abc", WqlComparisonOperator.LessThan, "ABD"));
            Assert.False(Evaluate<string>(null, WqlComparisonOperator.LessThan, "a"));
            Assert.True(Evaluate<string>(null, WqlComparisonOperator.Equal, null));
            Assert.True(Evaluate<int?>(null, WqlComparisonOperator.NotEqual, 5));
            Assert.False(Evaluate<int?>(null, WqlComparisonOperator.GreaterThan, 5));
            Assert.True(Evaluate<Guid>(new Guid("00000000-0000-0000-0000-000000000002"), WqlComparisonOperator.GreaterThan, new Guid("00000000-0000-0000-0000-000000000001")));
            Assert.True(Evaluate<bool>(true, WqlComparisonOperator.GreaterThan, false));
            Assert.True(Evaluate<StateKind>(StateKind.Closed, WqlComparisonOperator.GreaterThanOrEqual, StateKind.Pending));
            Assert.True(WqlExpressionBuilder.CompareValues(5, 5d, WqlComparisonOperator.Equal, Invariant));
            Assert.True(WqlExpressionBuilder.CompareValues("abc", "ABC", WqlComparisonOperator.Equal, Invariant));
            Assert.False(WqlExpressionBuilder.CompareValues(new DateTime(2024, 1, 1), "xyz", WqlComparisonOperator.Equal, Invariant));
            Assert.True(WqlExpressionBuilder.CompareValues(new DateTime(2024, 1, 1), "xyz", WqlComparisonOperator.NotEqual, Invariant));
        }

        /// <summary>
        /// Asserts that a value converts into the expected value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The target type.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="culture">The culture, invariant if omitted.</param>
        private static void AssertConverts(object value, Type type, object expected, CultureInfo culture = null)
        {
            Assert.True(WqlValueConverter.TryConvert(value, type, culture ?? Invariant, out var result), $"'{value}' does not convert to {type.Name}");
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Asserts that a value is refused for a type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The target type.</param>
        private static void AssertRefuses(object value, Type type)
        {
            Assert.False(WqlValueConverter.TryConvert(value, type, Invariant, out _), $"'{value}' converts to {type.Name}");
        }

        /// <summary>
        /// Matches a text against a query.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="query">The query.</param>
        /// <param name="mode">The match mode.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="similarity">The similarity.</param>
        /// <returns>True on a match.</returns>
        private static bool Match(string text, string query, WqlTextMatchMode mode, uint distance = 0, uint similarity = 0)
        {
            return WqlTextMatcher.IsMatch(text, query, mode, distance, similarity, Invariant);
        }

        /// <summary>
        /// Builds and evaluates a comparison of two constants of a type.
        /// </summary>
        /// <typeparam name="T">The type of the operands.</typeparam>
        /// <param name="left">The left value.</param>
        /// <param name="op">The comparison.</param>
        /// <param name="right">The right value.</param>
        /// <returns>The result of the comparison.</returns>
        private static bool Evaluate<T>(T left, WqlComparisonOperator op, T right)
        {
            var expression = WqlExpressionBuilder.Compare(Expression.Constant(left, typeof(T)), op, Expression.Constant(right, typeof(T)), Invariant);

            return Expression.Lambda<Func<bool>>(expression).Compile()();
        }
    }
}
