using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Creates the comparisons of all WQL conditions in one place. Keeping them together
    /// ensures that every condition types its right-hand side against the attribute in the
    /// same way, compares text without regard to case and treats null alike, so that the
    /// expression tree of a query and the predicate of an index scan cannot drift apart.
    /// </summary>
    /// <remarks>
    /// Null follows the lifted operators of C#: two nulls are equal, null differs from every
    /// value, and an ordering comparison involving null is false. A comparison is built from
    /// the operator of the type wherever one exists; only types without an operator, such as
    /// <see cref="Guid"/>, <see cref="bool"/> or enumerations in an ordering comparison, fall
    /// back to a comparison at evaluation time.
    /// </remarks>
    public static class WqlExpressionBuilder
    {
        private static readonly MethodInfo StringEqualsMethod = typeof(string)
            .GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)]);

        private static readonly MethodInfo StringCompareMethod = typeof(string)
            .GetMethod(nameof(string.Compare), [typeof(string), typeof(string), typeof(StringComparison)]);

        private static readonly MethodInfo StringStartsWithMethod = typeof(string)
            .GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)]);

        private static readonly MethodInfo StringEndsWithMethod = typeof(string)
            .GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)]);

        private static readonly MethodInfo StringContainsMethod = typeof(string)
            .GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)]);

        private static readonly MethodInfo CompareValuesMethod = typeof(WqlExpressionBuilder)
            .GetMethod(nameof(CompareValues), BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo ToTextMethod = typeof(WqlValueConverter)
            .GetMethod(nameof(WqlValueConverter.ToText), BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo MatcherIsMatchMethod = typeof(WqlTextMatcher)
            .GetMethod(nameof(WqlTextMatcher.IsMatch), BindingFlags.Public | BindingFlags.Instance, [typeof(string)]);

        private static readonly MethodInfo IsMatchMethod = typeof(WqlTextMatcher)
            .GetMethod(nameof(WqlTextMatcher.IsMatch), BindingFlags.Public | BindingFlags.Static,
            [typeof(string), typeof(string), typeof(WqlTextMatchMode), typeof(uint), typeof(uint), typeof(CultureInfo)]);

        private static readonly MethodInfo StringToLowerMethod = typeof(string)
            .GetMethod(nameof(string.ToLower), Type.EmptyTypes);

        private static readonly MethodInfo StringCompareTranslatableMethod = typeof(string)
            .GetMethod(nameof(string.Compare), [typeof(string), typeof(string)]);

        private static readonly MethodInfo StringStartsWithTranslatableMethod = typeof(string)
            .GetMethod(nameof(string.StartsWith), [typeof(string)]);

        private static readonly MethodInfo StringEndsWithTranslatableMethod = typeof(string)
            .GetMethod(nameof(string.EndsWith), [typeof(string)]);

        private static readonly MethodInfo StringContainsTranslatableMethod = typeof(string)
            .GetMethod(nameof(string.Contains), [typeof(string)]);

        /// <summary>
        /// Creates the constant of a literal, converted into the type of the operand it is
        /// compared with.
        /// </summary>
        /// <param name="value">The literal value. May be null.</param>
        /// <param name="targetType">The type of the other operand.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <returns>
        /// A constant of the target type; a null literal against a value type is typed as its
        /// nullable form, so that the comparison is lifted.
        /// </returns>
        /// <exception cref="InvalidCastException">The literal cannot be converted without loss.</exception>
        public static Expression Literal(object value, Type targetType, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            if (value is null)
            {
                return Expression.Constant(null, MakeNullable(targetType));
            }

            return Expression.Constant(WqlValueConverter.Convert(value, targetType, culture), targetType);
        }

        /// <summary>
        /// Creates the comparison of two operands.
        /// </summary>
        /// <param name="left">The left operand, usually the attribute.</param>
        /// <param name="op">The comparison.</param>
        /// <param name="right">The right operand, typed against the left one where it is a literal.</param>
        /// <param name="culture">The culture used to align values at evaluation time.</param>
        /// <returns>A boolean expression.</returns>
        public static Expression Compare(Expression left, WqlComparisonOperator op, Expression right, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            culture ??= CultureInfo.InvariantCulture;

            var leftType = Underlying(left.Type);
            var rightType = Underlying(right.Type);

            if (leftType == typeof(string) && rightType == typeof(string))
            {
                return CompareText(left, op, right);
            }

            if (leftType != rightType && WqlValueConverter.IsNumeric(leftType) && WqlValueConverter.IsNumeric(rightType))
            {
                // operands of different numeric types meet in the type that holds both; decimal
                // keeps integers and decimals exact, double is needed once a side is floating
                var common = IsFloating(leftType) || IsFloating(rightType) ? typeof(double) : typeof(decimal);

                left = ConvertNumber(left, common);
                right = ConvertNumber(right, common);
                leftType = common;
                rightType = common;
            }

            if (leftType == rightType && leftType != typeof(object) && TryMakeBinary(left, op, right, out var binary))
            {
                return binary;
            }

            return Expression.Call
            (
                CompareValuesMethod,
                Box(left),
                Box(right),
                Expression.Constant(op),
                Expression.Constant(culture)
            );
        }

        /// <summary>
        /// Creates the evaluation of the WQL text contract, a phrase or word search.
        /// </summary>
        /// <param name="text">The text searched in.</param>
        /// <param name="query">The words searched for.</param>
        /// <param name="mode">The match mode.</param>
        /// <param name="distance">The distance by which a word may stand further from the previous one.</param>
        /// <param name="similarity">The minimum similarity in percent.</param>
        /// <param name="culture">The culture used to split numbers within the text.</param>
        /// <returns>A boolean expression.</returns>
        public static Expression MatchText(Expression text, Expression query, WqlTextMatchMode mode, uint distance, uint similarity, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(query);

            culture ??= CultureInfo.InvariantCulture;
            text = AsText(text);

            if (query is ConstantExpression constant)
            {
                // a literal query is analyzed once instead of once per item
                var matcher = new WqlTextMatcher(WqlValueConverter.ToText(constant.Value), mode, distance, similarity, culture);

                return Expression.Call(Expression.Constant(matcher), MatcherIsMatchMethod, text);
            }

            return Expression.Call
            (
                IsMatchMethod,
                text,
                AsText(query),
                Expression.Constant(mode),
                Expression.Constant(distance),
                Expression.Constant(similarity),
                Expression.Constant(culture)
            );
        }

        /// <summary>
        /// Rewrites the conditions of an expression tree into forms that a query provider such
        /// as a database can translate, because neither the <see cref="WqlTextMatcher"/> nor
        /// the overloads of string methods taking a <see cref="StringComparison"/> exist there.
        /// A text search on a literal becomes a substring test per word, see
        /// <see cref="WqlTextMatcher.ToTranslatableExpression"/>; comparisons, prefixes,
        /// suffixes and substrings compare both sides in lower case. A text search whose
        /// words are only known at evaluation time and the comparisons of functions are left
        /// as they are, so a provider rejects them rather than returning a wrong result.
        /// </summary>
        /// <param name="expression">An expression built by this class.</param>
        /// <returns>The rewritten expression.</returns>
        public static Expression ToTranslatable(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            return new TranslatableRewriter().Visit(expression);
        }

        /// <summary>
        /// Creates the test whether a text starts with a value, without regard to case.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="value">The beginning searched for.</param>
        /// <returns>A boolean expression that is false if either side is null.</returns>
        public static Expression StartsWith(Expression text, Expression value)
        {
            return Substring(text, value, StringStartsWithMethod);
        }

        /// <summary>
        /// Creates the test whether a text ends with a value, without regard to case.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="value">The ending searched for.</param>
        /// <returns>A boolean expression that is false if either side is null.</returns>
        public static Expression EndsWith(Expression text, Expression value)
        {
            return Substring(text, value, StringEndsWithMethod);
        }

        /// <summary>
        /// Creates the test whether a text contains a value, without regard to case.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="value">The part searched for.</param>
        /// <returns>A boolean expression that is false if either side is null.</returns>
        public static Expression Contains(Expression text, Expression value)
        {
            return Substring(text, value, StringContainsMethod);
        }

        /// <summary>
        /// Compares two values at evaluation time. The expression trees fall back to this
        /// method where the operand types offer no operator, such as for the values of
        /// functions whose type is only known once they ran. The right value is aligned with
        /// the type of the left one by the <see cref="WqlValueConverter"/>; values that cannot
        /// be aligned are unequal and unordered.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <param name="op">The comparison.</param>
        /// <param name="culture">The culture used to align the values.</param>
        /// <returns>The result of the comparison.</returns>
        public static bool CompareValues(object left, object right, WqlComparisonOperator op, CultureInfo culture)
        {
            if (left is null || right is null)
            {
                var bothNull = left is null && right is null;

                return op switch
                {
                    WqlComparisonOperator.Equal => bothNull,
                    WqlComparisonOperator.NotEqual => !bothNull,
                    _ => false
                };
            }

            if (WqlValueConverter.TryConvert(right, left.GetType(), culture, out var alignedRight))
            {
                right = alignedRight;
            }
            else if (WqlValueConverter.TryConvert(left, right.GetType(), culture, out var alignedLeft))
            {
                left = alignedLeft;
            }
            else
            {
                return op == WqlComparisonOperator.NotEqual;
            }

            int? order = left switch
            {
                string text => string.Compare(text, (string)right, StringComparison.OrdinalIgnoreCase),
                IComparable comparable => comparable.CompareTo(right),
                _ => null
            };

            return op switch
            {
                WqlComparisonOperator.Equal => order.HasValue ? order == 0 : Equals(left, right),
                WqlComparisonOperator.NotEqual => order.HasValue ? order != 0 : !Equals(left, right),
                WqlComparisonOperator.LessThan => order < 0,
                WqlComparisonOperator.LessThanOrEqual => order <= 0,
                WqlComparisonOperator.GreaterThan => order > 0,
                WqlComparisonOperator.GreaterThanOrEqual => order >= 0,
                _ => false
            };
        }

        /// <summary>
        /// Returns the nullable form of a value type, or the type itself if it already holds null.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A type that can hold null.</returns>
        internal static Type MakeNullable(Type type)
        {
            return type.IsValueType && Nullable.GetUnderlyingType(type) is null
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;
        }

        /// <summary>
        /// Compares two texts without regard to case.
        /// </summary>
        /// <param name="left">The left text.</param>
        /// <param name="op">The comparison.</param>
        /// <param name="right">The right text.</param>
        /// <returns>A boolean expression.</returns>
        private static Expression CompareText(Expression left, WqlComparisonOperator op, Expression right)
        {
            var comparison = Expression.Constant(StringComparison.OrdinalIgnoreCase);

            if (op is WqlComparisonOperator.Equal or WqlComparisonOperator.NotEqual)
            {
                Expression equal = Expression.Call(StringEqualsMethod, left, right, comparison);

                return op == WqlComparisonOperator.Equal ? equal : Expression.Not(equal);
            }

            var order = Expression.Call(StringCompareMethod, left, right, comparison);
            var zero = Expression.Constant(0);

            Expression relation = op switch
            {
                WqlComparisonOperator.LessThan => Expression.LessThan(order, zero),
                WqlComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(order, zero),
                WqlComparisonOperator.GreaterThan => Expression.GreaterThan(order, zero),
                _ => Expression.GreaterThanOrEqual(order, zero)
            };

            // string.Compare orders null before every text, but an ordering involving null is false
            return Expression.AndAlso
            (
                Expression.AndAlso(IsNotNull(left), IsNotNull(right)),
                relation
            );
        }

        /// <summary>
        /// Attempts to create the comparison from the operator of the operand type.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="op">The comparison.</param>
        /// <param name="right">The right operand of the same underlying type.</param>
        /// <param name="binary">The comparison.</param>
        /// <returns>True if the type defines the operator, otherwise false.</returns>
        private static bool TryMakeBinary(Expression left, WqlComparisonOperator op, Expression right, out Expression binary)
        {
            if (left.Type != right.Type)
            {
                var lifted = MakeNullable(Underlying(left.Type));

                left = left.Type == lifted ? left : Expression.Convert(left, lifted);
                right = right.Type == lifted ? right : Expression.Convert(right, lifted);
            }

            try
            {
                binary = op switch
                {
                    WqlComparisonOperator.Equal => Expression.Equal(left, right),
                    WqlComparisonOperator.NotEqual => Expression.NotEqual(left, right),
                    WqlComparisonOperator.LessThan => Expression.LessThan(left, right),
                    WqlComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                    WqlComparisonOperator.GreaterThan => Expression.GreaterThan(left, right),
                    _ => Expression.GreaterThanOrEqual(left, right)
                };

                return true;
            }
            catch (InvalidOperationException)
            {
                binary = null;

                return false;
            }
        }

        /// <summary>
        /// Creates a substring test that ignores case and is false if either side is null.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="value">The value searched for.</param>
        /// <param name="method">The string method performing the test.</param>
        /// <returns>A boolean expression.</returns>
        private static Expression Substring(Expression text, Expression value, MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(value);

            text = AsText(text);
            value = AsText(value);

            return Expression.AndAlso
            (
                Expression.AndAlso(IsNotNull(text), IsNotNull(value)),
                Expression.Call(text, method, value, Expression.Constant(StringComparison.OrdinalIgnoreCase))
            );
        }

        /// <summary>
        /// Converts an operand into a numeric type while keeping its nullability.
        /// </summary>
        /// <param name="operand">The operand.</param>
        /// <param name="type">The numeric type.</param>
        /// <returns>The converted operand.</returns>
        private static Expression ConvertNumber(Expression operand, Type type)
        {
            var target = Nullable.GetUnderlyingType(operand.Type) is not null ? MakeNullable(type) : type;

            return operand.Type == target ? operand : Expression.Convert(operand, target);
        }

        /// <summary>
        /// Returns an operand as text, converting other types the way the converter writes them.
        /// </summary>
        /// <param name="operand">The operand.</param>
        /// <returns>An expression of type string.</returns>
        private static Expression AsText(Expression operand)
        {
            return operand.Type == typeof(string)
                ? operand
                : Expression.Call(ToTextMethod, Box(operand));
        }

        /// <summary>
        /// Converts an operand into an object.
        /// </summary>
        /// <param name="operand">The operand.</param>
        /// <returns>An expression of type object.</returns>
        private static Expression Box(Expression operand)
        {
            return operand.Type == typeof(object) ? operand : Expression.Convert(operand, typeof(object));
        }

        /// <summary>
        /// Creates the test whether an operand is not null.
        /// </summary>
        /// <param name="operand">The operand of a reference type.</param>
        /// <returns>A boolean expression.</returns>
        private static Expression IsNotNull(Expression operand)
        {
            return Expression.NotEqual(operand, Expression.Constant(null, operand.Type));
        }

        /// <summary>
        /// Returns the underlying type of a nullable type, or the type itself.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The underlying type.</returns>
        private static Type Underlying(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Determines whether a numeric type is a floating point type.
        /// </summary>
        /// <param name="type">The numeric type.</param>
        /// <returns>True for float and double, otherwise false.</returns>
        private static bool IsFloating(Type type)
        {
            return type == typeof(double) || type == typeof(float);
        }

        /// <summary>
        /// Replaces the calls of a tree that only run in memory by their translatable forms.
        /// </summary>
        private sealed class TranslatableRewriter : ExpressionVisitor
        {
            /// <summary>
            /// Visits a method call and replaces it if a translatable form exists.
            /// </summary>
            /// <param name="node">The method call.</param>
            /// <returns>The replacement or the visited call.</returns>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (base.VisitMethodCall(node) is not MethodCallExpression call)
                {
                    return node;
                }

                if (call.Method == MatcherIsMatchMethod && call.Object is ConstantExpression { Value: WqlTextMatcher matcher })
                {
                    return matcher.ToTranslatableExpression(call.Arguments[0]);
                }

                if (call.Method == StringEqualsMethod && IsIgnoreCase(call.Arguments[2]))
                {
                    var left = call.Arguments[0];
                    var right = call.Arguments[1];

                    // string.Equals treats two nulls as equal, which LOWER alone would lose
                    return Expression.OrElse
                    (
                        Expression.AndAlso(Expression.Not(IsNotNull(left)), Expression.Not(IsNotNull(right))),
                        Expression.AndAlso
                        (
                            Expression.AndAlso(IsNotNull(left), IsNotNull(right)),
                            Expression.Equal(Lower(left), Lower(right))
                        )
                    );
                }

                if (call.Method == StringCompareMethod && IsIgnoreCase(call.Arguments[2]))
                {
                    // the caller already guards both sides against null
                    return Expression.Call(StringCompareTranslatableMethod, Lower(call.Arguments[0]), Lower(call.Arguments[1]));
                }

                var substring = call.Method == StringStartsWithMethod ? StringStartsWithTranslatableMethod
                    : call.Method == StringEndsWithMethod ? StringEndsWithTranslatableMethod
                    : call.Method == StringContainsMethod ? StringContainsTranslatableMethod
                    : null;

                if (substring is not null && IsIgnoreCase(call.Arguments[1]))
                {
                    return Expression.Call(Lower(call.Object), substring, Lower(call.Arguments[0]));
                }

                return call;
            }

            /// <summary>
            /// Determines whether an argument is the comparison that ignores case.
            /// </summary>
            /// <param name="argument">The comparison argument.</param>
            /// <returns>True for a constant ordinal comparison ignoring case.</returns>
            private static bool IsIgnoreCase(Expression argument)
            {
                return argument is ConstantExpression { Value: StringComparison.OrdinalIgnoreCase };
            }

            /// <summary>
            /// Lowers a text; a literal is lowered right away, so that a null literal is not
            /// dereferenced when a provider evaluates the constant parts of a tree.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <returns>The lowered text.</returns>
            private static Expression Lower(Expression text)
            {
                return text is ConstantExpression constant
                    ? Expression.Constant(((string)constant.Value)?.ToLower(), typeof(string))
                    : Expression.Call(text, StringToLowerMethod);
            }
        }
    }
}
