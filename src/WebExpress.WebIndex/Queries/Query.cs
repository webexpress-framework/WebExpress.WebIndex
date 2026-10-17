using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WebExpress.WebIndex.Queries
{
    /// <summary>
    /// Represents a composable query definition for retrieving items from an index, supporting
    /// filtering, sorting and paging operations.
    /// </summary>
    /// <remarks>
    /// The filters of a query form a conjunction. <c>And</c> and <c>Or</c> treat each side as
    /// such a conjunction, so <c>Where(a).Where(b).Or(c)</c> means <c>(a &amp;&amp; b) || c</c>.
    /// A side without filters carries no condition and is ignored, so combining with an empty
    /// side returns the other side unchanged; <c>Or</c> with an empty side does not widen the
    /// result to all items, and <c>Not</c> on a query without filters leaves it unchanged.
    /// Combined expressions are rebound to a single parameter instead of being invoked, so
    /// query providers that cannot translate invocation nodes can still process them.
    /// </remarks>
    /// <typeparam name="TIndexItem">The type of items in the index being queried.</typeparam>
    public class Query<TIndexItem> : IQuery<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private static readonly MethodInfo StringContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
        private static readonly MethodInfo StringStartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)]);
        private static readonly MethodInfo StringEndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)]);
        private static readonly MethodInfo StringToLowerInvariantMethod = typeof(string).GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes);
        private static readonly MethodInfo StringIsNullOrEmptyMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)]);
        private static readonly MethodInfo AnyStringMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));
        private static readonly ConstantExpression NullString = Expression.Constant(null, typeof(string));

        private readonly List<Expression<Func<TIndexItem, bool>>> _filters;
        private readonly List<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> _orderBys;
        private readonly ReadOnlyCollection<Expression<Func<TIndexItem, bool>>> _filtersView;
        private readonly ReadOnlyCollection<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> _orderBysView;

        /// <summary>
        /// Gets the filters applied to index items as boolean expressions. The view is
        /// read-only, so a caller cannot alter an immutable query through a cast.
        /// </summary>
        public IEnumerable<Expression<Func<TIndexItem, bool>>> Filters => _filtersView;

        /// <summary>
        /// Gets the sorting criteria applied to the query, the primary one first.
        /// </summary>
        public IReadOnlyList<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> OrderBys => _orderBysView;

        /// <summary>
        /// Gets the number of items to skip before starting to return results.
        /// </summary>
        public int? Skip { get; private set; }

        /// <summary>
        /// Gets the maximum number of items to return in a query result.
        /// </summary>
        public int? Take { get; private set; }

        /// <summary>
        /// Creates a new empty query with default settings.
        /// </summary>
        public Query()
            : this([], [], null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified filtering, ordering,
        /// and pagination options. The lists are always created here, so no two instances
        /// share state and the immutability of a query cannot be broken by a caller that
        /// still holds the source sequences.
        /// </summary>
        /// <param name="filters">
        /// The filter expressions that define the criteria used to select items from the index.
        /// </param>
        /// <param name="orderBys">
        /// The sorting criteria, the primary one first.
        /// </param>
        /// <param name="skip">
        /// The number of items to skip before starting to collect the
        /// result set. Specify null to skip no items.
        /// </param>
        /// <param name="take">
        /// The maximum number of items to return in the result set.
        /// Specify null to return all remaining items after skipping.
        /// </param>
        private Query
        (
            IEnumerable<Expression<Func<TIndexItem, bool>>> filters,
            IEnumerable<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> orderBys,
            int? skip,
            int? take
        )
        {
            _filters = [.. filters];
            _orderBys = [.. orderBys];
            _filtersView = _filters.AsReadOnly();
            _orderBysView = _orderBys.AsReadOnly();
            Skip = skip;
            Take = take;
        }

        /// <summary>
        /// Combines the filters of this query with the filters of another query using a
        /// logical AND. Each side is evaluated as the conjunction of its filters; a side
        /// without filters is ignored. Sorting and paging are taken from this query.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using AND.
        /// </param>
        /// <returns>A new query instance with the combined filter.</returns>
        public IQuery<TIndexItem> And(IQuery<TIndexItem> otherQuery)
        {
            ArgumentNullException.ThrowIfNull(otherQuery);

            return Join(otherQuery.Filters, Expression.AndAlso);
        }

        /// <summary>
        /// Combines the filters of this query with an additional condition using a logical
        /// AND. If this query has no filters, the condition becomes its only filter.
        /// </summary>
        /// <param name="expr">
        /// An expression representing a condition to combine with the existing
        /// filters using AND. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query combining the existing filters with the specified condition using
        /// logical AND.
        /// </returns>
        public IQuery<TIndexItem> And(Expression<Func<TIndexItem, bool>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr);

            return Join([expr], Expression.AndAlso);
        }

        /// <summary>
        /// Combines the filters of this query with the filters of another query using a
        /// logical OR. Each side is evaluated as the conjunction of its filters; a side
        /// without filters is ignored. Sorting and paging are taken from this query.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using OR.
        /// </param>
        /// <returns>A new query instance with the combined filter.</returns>
        public IQuery<TIndexItem> Or(IQuery<TIndexItem> otherQuery)
        {
            ArgumentNullException.ThrowIfNull(otherQuery);

            return Join(otherQuery.Filters, Expression.OrElse);
        }

        /// <summary>
        /// Combines the filters of this query with an additional condition using a logical
        /// OR. If this query has no filters, the condition becomes its only filter.
        /// </summary>
        /// <param name="expr">
        /// An expression representing a condition to combine with the existing
        /// filters using OR. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the existing filters combined with the specified condition
        /// using logical OR.
        /// </returns>
        public IQuery<TIndexItem> Or(Expression<Func<TIndexItem, bool>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr);

            return Join([expr], Expression.OrElse);
        }

        /// <summary>
        /// Negates the filters of this query: the result contains the items that do not
        /// satisfy all of them. A query without filters is returned unchanged.
        /// </summary>
        /// <returns>A new query with the negated filter.</returns>
        public IQuery<TIndexItem> Not()
        {
            var conjunction = Conjoin(_filters);

            if (conjunction is null)
            {
                return new Query<TIndexItem>(_filters, _orderBys, Skip, Take);
            }

            var negation = Expression.Lambda<Func<TIndexItem, bool>>(Expression.Not(conjunction.Body), conjunction.Parameters);

            return new Query<TIndexItem>([negation], _orderBys, Skip, Take);
        }

        /// <summary>
        /// Filters the query results based on the specified predicate expressions.
        /// </summary>
        /// <param name="predicates">
        /// An array of expressions that define the conditions each item in the query
        /// must satisfy. Neither the array nor any of its elements can be null.
        /// </param>
        /// <returns>
        /// A new query that contains only the items that satisfy all specified predicates.
        /// </returns>
        public IQuery<TIndexItem> Where(params Expression<Func<TIndexItem, bool>>[] predicates)
        {
            ArgumentNullException.ThrowIfNull(predicates);

            if (predicates.Any(x => x is null))
            {
                throw new ArgumentException("The predicates must not contain null.", nameof(predicates));
            }

            return new Query<TIndexItem>(_filters.Concat(predicates), _orderBys, Skip, Take);
        }

        /// <summary>
        /// Filters the query to include only items that do not satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// The condition the returned items must not satisfy. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query that contains only the items for which the predicate is false.
        /// </returns>
        public IQuery<TIndexItem> WhereNot(Expression<Func<TIndexItem, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(Expression.Not(predicate.Body), predicate.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property
        /// equals the given value, using an ordinal comparison.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The value to compare against the selected property. Can be null to match items
        /// with a null property value.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property equals the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(selector);

            if (value is null)
            {
                return Where(Expression.Lambda<Func<TIndexItem, bool>>(Expression.Equal(selector.Body, NullString), selector.Parameters));
            }

            return WhereString(selector, value, false, Expression.Equal);
        }

        /// <summary>
        /// Filters the query to include only items where the specified Guid property
        /// equals the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the Guid property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The identifier to compare against the selected property.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property equals the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, Guid>> selector, Guid value)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var body = Expression.Equal(selector.Body, Expression.Constant(value, typeof(Guid)));

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items that satisfy the given lambda expression condition.
        /// </summary>
        /// <param name="predicate">
        /// A lambda expression representing a boolean condition. This condition will be used to
        /// filter items in the query. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that includes only items matching the specified predicate.
        /// </returns>
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return Where(predicate);
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property equals
        /// the given value, using a culture-invariant case-insensitive comparison. Items with
        /// a null property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The value to compare against the selected property. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that contains only items where the selected property equals the specified
        /// value, ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereEqualsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, true, Expression.Equal);
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property differs
        /// from the given value, using an ordinal comparison. Items with a null property value
        /// differ from every non-null value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The value the selected property must not equal. Can be null to match items with a
        /// non-null property value.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property differs from the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereNotEquals(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(selector);

            // the inequality operator calls no instance method, so a null property needs no guard
            var body = Expression.NotEqual(selector.Body, Expression.Constant(value, typeof(string)));

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items where the specified Guid property differs
        /// from the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the Guid property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The identifier the selected property must not equal.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property differs from the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereNotEquals(Expression<Func<TIndexItem, Guid>> selector, Guid value)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var body = Expression.NotEqual(selector.Body, Expression.Constant(value, typeof(Guid)));

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value is one of the
        /// given values. The values are copied when the filter is built, so later changes to
        /// the source collection do not affect the query.
        /// </summary>
        /// <typeparam name="TValue">The type of the compared value.</typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="values">
        /// The accepted values. Cannot be null; an empty collection matches no item.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected value is contained in the given values.
        /// </returns>
        public IQuery<TIndexItem> WhereIn<TValue>(Expression<Func<TIndexItem, TValue>> selector, IEnumerable<TValue> values)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(values);

            var contains = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TValue));

            var body = Expression.Call
            (
                contains,
                Expression.Constant(values.ToArray(), typeof(IEnumerable<TValue>)),
                selector.Body
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value is greater than
        /// the given value.
        /// </summary>
        /// <typeparam name="TValue">
        /// A comparable value type. Types without comparison operators are compared via
        /// <see cref="IComparable{T}.CompareTo"/>.
        /// </typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="value">The exclusive lower bound.</param>
        /// <returns>A query that returns items whose selected value is greater than the bound.</returns>
        public IQuery<TIndexItem> WhereGreaterThan<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return WhereCompare(selector, (ExpressionType.GreaterThan, value));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value is greater than
        /// or equal to the given value.
        /// </summary>
        /// <typeparam name="TValue">
        /// A comparable value type. Types without comparison operators are compared via
        /// <see cref="IComparable{T}.CompareTo"/>.
        /// </typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="value">The inclusive lower bound.</param>
        /// <returns>A query that returns items whose selected value is at least the bound.</returns>
        public IQuery<TIndexItem> WhereGreaterThanOrEqual<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return WhereCompare(selector, (ExpressionType.GreaterThanOrEqual, value));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value is less than
        /// the given value.
        /// </summary>
        /// <typeparam name="TValue">
        /// A comparable value type. Types without comparison operators are compared via
        /// <see cref="IComparable{T}.CompareTo"/>.
        /// </typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="value">The exclusive upper bound.</param>
        /// <returns>A query that returns items whose selected value is less than the bound.</returns>
        public IQuery<TIndexItem> WhereLessThan<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return WhereCompare(selector, (ExpressionType.LessThan, value));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value is less than
        /// or equal to the given value.
        /// </summary>
        /// <typeparam name="TValue">
        /// A comparable value type. Types without comparison operators are compared via
        /// <see cref="IComparable{T}.CompareTo"/>.
        /// </typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="value">The inclusive upper bound.</param>
        /// <returns>A query that returns items whose selected value is at most the bound.</returns>
        public IQuery<TIndexItem> WhereLessThanOrEqual<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return WhereCompare(selector, (ExpressionType.LessThanOrEqual, value));
        }

        /// <summary>
        /// Filters the query to include only items where the selected value lies within the
        /// given range, both bounds included.
        /// </summary>
        /// <typeparam name="TValue">
        /// A comparable value type. Types without comparison operators are compared via
        /// <see cref="IComparable{T}.CompareTo"/>.
        /// </typeparam>
        /// <param name="selector">
        /// An expression that selects the value of the index item to compare. Cannot be null.
        /// </param>
        /// <param name="lower">The inclusive lower bound.</param>
        /// <param name="upper">The inclusive upper bound. Cannot be less than the lower bound.</param>
        /// <returns>A query that returns items whose selected value lies within the range.</returns>
        public IQuery<TIndexItem> WhereBetween<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue lower, TValue upper)
            where TValue : struct, IComparable<TValue>
        {
            if (lower.CompareTo(upper) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upper), upper, "The upper bound must not be less than the lower bound.");
            }

            return WhereCompare(selector, (ExpressionType.GreaterThanOrEqual, lower), (ExpressionType.LessThanOrEqual, upper));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property contains
        /// the given value as a substring, using an ordinal comparison. Items with a null
        /// property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of each item to be searched for
        /// the specified value. Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the selected property. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property contains the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, false, (subject, operand) => Expression.Call(subject, StringContainsMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection contains the given value as a substring, using an ordinal comparison.
        /// A null collection is treated as empty and null elements are skipped.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings to search within each item.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the elements. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items with at least one element containing the specified value.
        /// </returns>
        public IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            return WhereAnyElement(selector, value, false, (element, operand) => Expression.Call(element, StringContainsMethod, operand));
        }

        /// <summary>
        /// Filters the query to include items where the specified string property contains the
        /// given value as a substring, using a culture-invariant case-insensitive comparison.
        /// Items with a null property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to search within.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the selected property. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns only items where the selected property contains the
        /// specified value, ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, true, (subject, operand) => Expression.Call(subject, StringContainsMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection contains the given value as a substring, using a culture-invariant
        /// case-insensitive comparison. A null collection is treated as empty and null elements
        /// are skipped.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings from each item to be searched.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the elements. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items with at least one element containing the specified
        /// value, ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            return WhereAnyElement(selector, value, true, (element, operand) => Expression.Call(element, StringContainsMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection equals the given value, using an ordinal comparison. A null
        /// collection is treated as empty and null elements are skipped.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings from each item. Cannot be null.
        /// </param>
        /// <param name="value">The element to look for. Cannot be null.</param>
        /// <returns>
        /// A query that returns items whose collection contains the specified element.
        /// </returns>
        public IQuery<TIndexItem> WhereElementEquals(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            return WhereAnyElement(selector, value, false, Expression.Equal);
        }

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection equals the given value, using a culture-invariant case-insensitive
        /// comparison. A null collection is treated as empty and null elements are skipped.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings from each item. Cannot be null.
        /// </param>
        /// <param name="value">The element to look for. Cannot be null.</param>
        /// <returns>
        /// A query that returns items whose collection contains the specified element,
        /// ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereElementEqualsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            return WhereAnyElement(selector, value, true, Expression.Equal);
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property
        /// starts with the given value, using an ordinal comparison. Items with a null
        /// property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The string value to compare against the start of the selected property.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected property values start with the
        /// specified string.
        /// </returns>
        public IQuery<TIndexItem> WhereStartsWith(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, false, (subject, operand) => Expression.Call(subject, StringStartsWithMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property
        /// starts with the given value, using a culture-invariant case-insensitive comparison.
        /// Items with a null property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The string value to compare against the start of the selected property.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected string property starts with the
        /// specified value, ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereStartsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, true, (subject, operand) => Expression.Call(subject, StringStartsWithMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property ends
        /// with the given value, using an ordinal comparison. Items with a null property
        /// value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to compare against the end of the selected property value.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected property values end with the
        /// specified substring.
        /// </returns>
        public IQuery<TIndexItem> WhereEndsWith(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, false, (subject, operand) => Expression.Call(subject, StringEndsWithMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property ends
        /// with the given value, using a culture-invariant case-insensitive comparison. Items
        /// with a null property value never match.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <param name="value">
        /// The substring to compare against the end of the selected property value.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A query that contains only items whose selected property value ends with the
        /// specified substring, ignoring case.
        /// </returns>
        public IQuery<TIndexItem> WhereEndsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            return WhereString(selector, value, true, (subject, operand) => Expression.Call(subject, StringEndsWithMethod, operand));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property is
        /// null or empty.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <returns>A query that returns items without a value in the selected property.</returns>
        public IQuery<TIndexItem> WhereIsNullOrEmpty(Expression<Func<TIndexItem, string>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var body = Expression.Call(StringIsNullOrEmptyMethod, selector.Body);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Filters the query to include only items where the specified string property is
        /// neither null nor empty.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <returns>A query that returns items with a value in the selected property.</returns>
        public IQuery<TIndexItem> WhereIsNotNullOrEmpty(Expression<Func<TIndexItem, string>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            var body = Expression.Not(Expression.Call(StringIsNullOrEmptyMethod, selector.Body));

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Sorts the query results in ascending order by the given key, replacing any
        /// existing sort order.
        /// </summary>
        /// <param name="key">
        /// An expression that identifies the key to use for ordering the results in ascending
        /// order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the specified key as its only sort criterion.
        /// </returns>
        public IQuery<TIndexItem> OrderByAsc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new Query<TIndexItem>(_filters, [(key, false)], Skip, Take);
        }

        /// <summary>
        /// Sorts the query results in descending order by the given key, replacing any
        /// existing sort order.
        /// </summary>
        /// <param name="key">An expression that identifies the key to sort the results by
        /// in descending order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the specified key as its only sort criterion.
        /// </returns>
        public IQuery<TIndexItem> OrderByDesc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new Query<TIndexItem>(_filters, [(key, true)], Skip, Take);
        }

        /// <summary>
        /// Appends an ascending sort criterion that orders items the previous criteria
        /// consider equal. Without a previous criterion, it becomes the primary sort order.
        /// </summary>
        /// <param name="key">
        /// An expression that specifies the key to use for the secondary ascending sort.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the additional ascending sort order applied.
        /// </returns>
        public IQuery<TIndexItem> ThenByAsc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new Query<TIndexItem>(_filters, _orderBys.Append((key, false)), Skip, Take);
        }

        /// <summary>
        /// Appends a descending sort criterion that orders items the previous criteria
        /// consider equal. Without a previous criterion, it becomes the primary sort order.
        /// </summary>
        /// <param name="key">
        /// An expression that specifies the key to use for the secondary descending sort.
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the additional descending sort order applied.
        /// </returns>
        public IQuery<TIndexItem> ThenByDesc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new Query<TIndexItem>(_filters, _orderBys.Append((key, true)), Skip, Take);
        }

        /// <summary>
        /// Limits the query results by skipping a specified number of items and returning
        /// up to a specified maximum number of items.
        /// </summary>
        /// <param name="skip">
        /// The number of items to skip before starting to return results. Must be greater than
        /// or equal to 0.
        /// </param>
        /// <param name="take">
        /// The maximum number of items to return. Must be greater than 0.
        /// </param>
        /// <returns>
        /// A query that, when executed, returns up to the specified number of items after
        /// skipping the given number of items.
        /// </returns>
        public IQuery<TIndexItem> WithPaging(int skip, int take)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

            return new Query<TIndexItem>(_filters, _orderBys, skip, take);
        }

        /// <summary>
        /// Sets the number of items to skip before starting to return results, keeping the
        /// current limit. Allows an offset without a limit, which <c>WithPaging</c> cannot
        /// express.
        /// </summary>
        /// <param name="skip">
        /// The number of items to skip. Must be greater than or equal to 0.
        /// </param>
        /// <returns>A new query with the specified offset.</returns>
        public IQuery<TIndexItem> WithSkip(int skip)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);

            return new Query<TIndexItem>(_filters, _orderBys, skip, Take);
        }

        /// <summary>
        /// Sets the maximum number of items to return, keeping the current offset. Unlike
        /// <c>WithPaging</c>, a limit of 0 is allowed and yields an empty result.
        /// </summary>
        /// <param name="take">
        /// The maximum number of items to return. Must be greater than or equal to 0.
        /// </param>
        /// <returns>A new query with the specified limit.</returns>
        public IQuery<TIndexItem> WithTake(int take)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(take);

            return new Query<TIndexItem>(_filters, _orderBys, Skip, take);
        }

        /// <summary>
        /// Applies the query to the given queryable source: filtering, sorting and paging.
        /// </summary>
        /// <param name="query">The source queryable collection to which the query
        /// will be applied.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{TIndexItem}"/> representing the source with the
        /// filters, sorting and paging of this query applied.
        /// </returns>
        public virtual IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> query)
        {
            // filters
            foreach (var filter in _filters)
            {
                query = query.Where(filter);
            }

            // sorting
            IOrderedQueryable<TIndexItem> ordered = null;
            for (int i = 0; i < _orderBys.Count; i++)
            {
                var expr = _orderBys[i];
                if (i == 0)
                {
                    if (expr.Descending)
                    {
                        ordered = query.OrderByDescending(expr.KeySelector);
                    }
                    else
                    {
                        ordered = query.OrderBy(expr.KeySelector);
                    }
                }
                else
                {
                    if (expr.Descending)
                    {
                        ordered = ordered.ThenByDescending(expr.KeySelector);
                    }
                    else
                    {
                        ordered = ordered.ThenBy(expr.KeySelector);
                    }
                }
            }

            query = ordered ?? query;

            // paging
            if (Skip.HasValue)
            {
                query = query.Skip(Skip.Value);
            }

            if (Take.HasValue)
            {
                query = query.Take(Take.Value);
            }

            return query;
        }

        /// <summary>
        /// Combines this query's filters with another set of filters using the given
        /// operator. Each side stands for the conjunction of its filters, because that is
        /// how <see cref="Apply"/> evaluates a filter list; joining the filters one by one
        /// with the operator would turn <c>(a &amp;&amp; b) || c</c> into <c>a || b || c</c>.
        /// </summary>
        /// <param name="others">The filters of the right-hand side.</param>
        /// <param name="logicalOperator">The operator that joins both sides.</param>
        /// <returns>A new query with the joined filter and this query's sorting and paging.</returns>
        private Query<TIndexItem> Join
        (
            IEnumerable<Expression<Func<TIndexItem, bool>>> others,
            Func<Expression, Expression, BinaryExpression> logicalOperator
        )
        {
            var left = Conjoin(_filters);
            var right = Conjoin(others);

            if (right is null)
            {
                return new Query<TIndexItem>(_filters, _orderBys, Skip, Take);
            }

            var joined = left is null ? right : Combine(left, right, logicalOperator);

            return new Query<TIndexItem>([joined], _orderBys, Skip, Take);
        }

        /// <summary>
        /// Reduces a filter list to the single expression that <see cref="Apply"/> would
        /// evaluate for it.
        /// </summary>
        /// <param name="filters">The filters to reduce.</param>
        /// <returns>The conjunction of the filters, or null if there are none.</returns>
        private static Expression<Func<TIndexItem, bool>> Conjoin(IEnumerable<Expression<Func<TIndexItem, bool>>> filters)
        {
            Expression<Func<TIndexItem, bool>> conjunction = null;

            foreach (var filter in filters)
            {
                conjunction = conjunction is null ? filter : Combine(conjunction, filter, Expression.AndAlso);
            }

            return conjunction;
        }

        /// <summary>
        /// Joins two lambdas into one. The right body is rebound to the left parameter
        /// instead of being wrapped in an invocation, which many query providers cannot
        /// translate and which hides the predicate from expression analysis.
        /// </summary>
        /// <param name="left">The left-hand lambda, whose parameter the result uses.</param>
        /// <param name="right">The right-hand lambda.</param>
        /// <param name="logicalOperator">The operator that joins both bodies.</param>
        /// <returns>A lambda over a single parameter with the joined bodies.</returns>
        private static Expression<Func<TIndexItem, bool>> Combine
        (
            Expression<Func<TIndexItem, bool>> left,
            Expression<Func<TIndexItem, bool>> right,
            Func<Expression, Expression, BinaryExpression> logicalOperator
        )
        {
            var param = left.Parameters[0];
            var rightBody = new ParameterRebinder(right.Parameters[0], param).Visit(right.Body);

            return Expression.Lambda<Func<TIndexItem, bool>>(logicalOperator(left.Body, rightBody), param);
        }

        /// <summary>
        /// Adds a filter that matches a string property against a value. All string filters
        /// share it, so they validate their arguments when the query is built rather than
        /// when it is executed, never call a method on a null property and normalize case
        /// with the invariant culture, which keeps the result independent of the culture
        /// that is current at execution time.
        /// </summary>
        /// <param name="selector">The selector of the string property.</param>
        /// <param name="value">The value to match.</param>
        /// <param name="ignoreCase">True to compare both sides in invariant lower case.</param>
        /// <param name="match">Builds the comparison from the property and the value.</param>
        /// <returns>A new query with the additional filter.</returns>
        private IQuery<TIndexItem> WhereString
        (
            Expression<Func<TIndexItem, string>> selector,
            string value,
            bool ignoreCase,
            Func<Expression, Expression, Expression> match
        )
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(value);

            var subject = ignoreCase ? Expression.Call(selector.Body, StringToLowerInvariantMethod) : selector.Body;
            var operand = Expression.Constant(ignoreCase ? value.ToLowerInvariant() : value, typeof(string));

            var body = Expression.AndAlso
            (
                Expression.NotEqual(selector.Body, NullString),
                match(subject, operand)
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Adds a filter that matches if any element of a string collection matches a value.
        /// A missing collection is treated as empty and null elements are skipped, so items
        /// with incomplete data are excluded instead of failing the whole query.
        /// </summary>
        /// <param name="selector">The selector of the string collection.</param>
        /// <param name="value">The value to match.</param>
        /// <param name="ignoreCase">True to compare both sides in invariant lower case.</param>
        /// <param name="match">Builds the comparison from an element and the value.</param>
        /// <returns>A new query with the additional filter.</returns>
        private IQuery<TIndexItem> WhereAnyElement
        (
            Expression<Func<TIndexItem, IEnumerable<string>>> selector,
            string value,
            bool ignoreCase,
            Func<Expression, Expression, Expression> match
        )
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(value);

            var element = Expression.Parameter(typeof(string), "s");
            var subject = ignoreCase ? Expression.Call(element, StringToLowerInvariantMethod) : (Expression)element;
            var operand = Expression.Constant(ignoreCase ? value.ToLowerInvariant() : value, typeof(string));

            var elementMatch = Expression.Lambda<Func<string, bool>>
            (
                Expression.AndAlso(Expression.NotEqual(element, NullString), match(subject, operand)),
                element
            );

            var collection = Expression.Coalesce
            (
                selector.Body,
                Expression.Constant(Array.Empty<string>(), typeof(IEnumerable<string>))
            );

            var body = Expression.Call(AnyStringMethod, collection, elementMatch);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Adds a filter that compares a value against one or more bounds, all of which
        /// must hold.
        /// </summary>
        /// <typeparam name="TValue">A comparable value type.</typeparam>
        /// <param name="selector">The selector of the compared value.</param>
        /// <param name="bounds">The comparisons and their operands.</param>
        /// <returns>A new query with the additional filter.</returns>
        private IQuery<TIndexItem> WhereCompare<TValue>
        (
            Expression<Func<TIndexItem, TValue>> selector,
            params (ExpressionType Comparison, TValue Value)[] bounds
        )
            where TValue : struct, IComparable<TValue>
        {
            ArgumentNullException.ThrowIfNull(selector);

            var body = bounds
                .Select(x => Compare(selector.Body, x.Comparison, x.Value))
                .Aggregate(Expression.AndAlso);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, selector.Parameters));
        }

        /// <summary>
        /// Builds a comparison of a value with a constant. Types such as <see cref="char"/>
        /// or <see cref="bool"/> implement <see cref="IComparable{T}"/> but define no
        /// comparison operator, so they are compared through the sign of <c>CompareTo</c>.
        /// </summary>
        /// <typeparam name="TValue">A comparable value type.</typeparam>
        /// <param name="subject">The compared value.</param>
        /// <param name="comparison">The comparison to build.</param>
        /// <param name="value">The constant to compare against.</param>
        /// <returns>A boolean expression.</returns>
        private static Expression Compare<TValue>(Expression subject, ExpressionType comparison, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            var operand = Expression.Constant(value, typeof(TValue));

            try
            {
                return Expression.MakeBinary(comparison, subject, operand);
            }
            catch (InvalidOperationException)
            {
                var compareTo = typeof(IComparable<TValue>).GetMethod(nameof(IComparable<TValue>.CompareTo));

                return Expression.MakeBinary(comparison, Expression.Call(subject, compareTo, operand), Expression.Constant(0));
            }
        }

        /// <summary>
        /// Replaces one parameter with another, so that the body of one lambda can be
        /// placed inside another lambda.
        /// </summary>
        /// <param name="from">The parameter to replace.</param>
        /// <param name="to">The replacement.</param>
        private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            /// <summary>
            /// Replaces the parameter if it is the one to rebind.
            /// </summary>
            /// <param name="node">The visited parameter.</param>
            /// <returns>The replacement or the unchanged parameter.</returns>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == from ? to : base.VisitParameter(node);
            }
        }
    }
}
