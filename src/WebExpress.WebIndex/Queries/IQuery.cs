using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Queries
{
    /// <summary>
    /// Represents a composable query definition for retrieving items from an index, supporting
    /// filtering, sorting and paging operations.
    /// </summary>
    /// <remarks>
    /// The interface provides a fluent API for building queries against an index. Filtering,
    /// sorting and paging can be chained before execution. Instances are immutable; each method
    /// returns a new query instance with the specified modification applied.
    ///
    /// The filters of a query form a conjunction: an item is returned only if it satisfies
    /// all of them. <see cref="And(IQuery{TIndexItem})"/> and <see cref="Or(IQuery{TIndexItem})"/>
    /// treat each side as such a conjunction, so <c>Where(a).Where(b).Or(c)</c> means
    /// <c>(a &amp;&amp; b) || c</c>. A side without filters carries no condition and is ignored,
    /// so combining with an empty side returns the other side unchanged. In particular,
    /// <c>Or</c> with an empty side does not widen the result to all items, and <see cref="Not"/>
    /// on a query without filters leaves it unchanged.
    /// </remarks>
    /// <typeparam name="TIndexItem">
    /// The type of items in the index to be queried.
    /// </typeparam>
    public interface IQuery<TIndexItem>
       where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the filter expressions applied to the index items. All of them must be
        /// satisfied for an item to be returned. The collection is read-only.
        /// </summary>
        IEnumerable<Expression<Func<TIndexItem, bool>>> Filters { get; }

        /// <summary>
        /// Gets the sorting criteria applied to the query, the primary one first.
        /// </summary>
        public IReadOnlyList<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> OrderBys { get; }

        /// <summary>
        /// Gets the number of items to skip before starting to return results.
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Gets the maximum number of items to return in a query result.
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Combines the filters of this query with the filters of another query using a
        /// logical AND. Each side is evaluated as the conjunction of its filters; a side
        /// without filters is ignored. Sorting and paging are taken from this query.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using AND.
        /// </param>
        /// <returns>A new query instance with the combined filter.</returns>
        IQuery<TIndexItem> And(IQuery<TIndexItem> otherQuery);

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
        IQuery<TIndexItem> And(Expression<Func<TIndexItem, bool>> expr);

        /// <summary>
        /// Combines the filters of this query with the filters of another query using a
        /// logical OR. Each side is evaluated as the conjunction of its filters; a side
        /// without filters is ignored. Sorting and paging are taken from this query.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using OR.
        /// </param>
        /// <returns>A new query instance with the combined filter.</returns>
        IQuery<TIndexItem> Or(IQuery<TIndexItem> otherQuery);

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
        IQuery<TIndexItem> Or(Expression<Func<TIndexItem, bool>> expr);

        /// <summary>
        /// Negates the filters of this query: the result contains the items that do not
        /// satisfy all of them. A query without filters is returned unchanged.
        /// </summary>
        /// <returns>A new query with the negated filter.</returns>
        IQuery<TIndexItem> Not();

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
        IQuery<TIndexItem> Where(params Expression<Func<TIndexItem, bool>>[] predicates);

        /// <summary>
        /// Filters the query to include only items that do not satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// The condition the returned items must not satisfy. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query that contains only the items for which the predicate is false.
        /// </returns>
        IQuery<TIndexItem> WhereNot(Expression<Func<TIndexItem, bool>> predicate);

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
        IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, string>> selector, string value);

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
        IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, Guid>> selector, Guid value);

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
        IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, bool>> predicate);

        /// <summary>
        /// Filters the query to include only items where the specified string property equals
        /// the given value, using a case-insensitive comparison. Items with
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
        IQuery<TIndexItem> WhereEqualsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

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
        IQuery<TIndexItem> WhereNotEquals(Expression<Func<TIndexItem, string>> selector, string value);

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
        IQuery<TIndexItem> WhereNotEquals(Expression<Func<TIndexItem, Guid>> selector, Guid value);

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
        IQuery<TIndexItem> WhereIn<TValue>(Expression<Func<TIndexItem, TValue>> selector, IEnumerable<TValue> values);

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
        IQuery<TIndexItem> WhereGreaterThan<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>;

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
        IQuery<TIndexItem> WhereGreaterThanOrEqual<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>;

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
        IQuery<TIndexItem> WhereLessThan<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>;

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
        IQuery<TIndexItem> WhereLessThanOrEqual<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue value)
            where TValue : struct, IComparable<TValue>;

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
        IQuery<TIndexItem> WhereBetween<TValue>(Expression<Func<TIndexItem, TValue>> selector, TValue lower, TValue upper)
            where TValue : struct, IComparable<TValue>;

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
        IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection contains the given value as a substring, using an ordinal comparison.
        /// A null collection is treated as empty and null elements are skipped. Use
        /// <see cref="WhereElementEquals"/> to match whole elements.
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
        IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

        /// <summary>
        /// Filters the query to include items where the specified string property contains the
        /// given value as a substring, using a case-insensitive comparison.
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
        IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection contains the given value as a substring, using a
        /// case-insensitive comparison. A null collection is treated as empty and null elements
        /// are skipped. Use <see cref="WhereElementEqualsIgnoreCase"/> to match whole elements.
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
        IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

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
        IQuery<TIndexItem> WhereElementEquals(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where at least one element of the specified
        /// string collection equals the given value, using a case-insensitive
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
        IQuery<TIndexItem> WhereElementEqualsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

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
        IQuery<TIndexItem> WhereStartsWith(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property
        /// starts with the given value, using a case-insensitive comparison.
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
        IQuery<TIndexItem> WhereStartsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

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
        IQuery<TIndexItem> WhereEndsWith(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property ends
        /// with the given value, using a case-insensitive comparison. Items
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
        IQuery<TIndexItem> WhereEndsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property is
        /// null or empty.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <returns>A query that returns items without a value in the selected property.</returns>
        IQuery<TIndexItem> WhereIsNullOrEmpty(Expression<Func<TIndexItem, string>> selector);

        /// <summary>
        /// Filters the query to include only items where the specified string property is
        /// neither null nor empty.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// Cannot be null.
        /// </param>
        /// <returns>A query that returns items with a value in the selected property.</returns>
        IQuery<TIndexItem> WhereIsNotNullOrEmpty(Expression<Func<TIndexItem, string>> selector);

        /// <summary>
        /// Sorts the query results in ascending order by the given key, replacing any
        /// existing sort order. Use <see cref="ThenByAsc"/> or <see cref="ThenByDesc"/> to
        /// add further criteria.
        /// </summary>
        /// <param name="key">
        /// An expression that identifies the key to use for ordering the results in ascending
        /// order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the specified key as its only sort criterion.
        /// </returns>
        IQuery<TIndexItem> OrderByAsc(Expression<Func<TIndexItem, object>> key);

        /// <summary>
        /// Sorts the query results in descending order by the given key, replacing any
        /// existing sort order. Use <see cref="ThenByAsc"/> or <see cref="ThenByDesc"/> to
        /// add further criteria.
        /// </summary>
        /// <param name="key">An expression that identifies the key to sort the results by
        /// in descending order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query with the specified key as its only sort criterion.
        /// </returns>
        IQuery<TIndexItem> OrderByDesc(Expression<Func<TIndexItem, object>> key);

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
        IQuery<TIndexItem> ThenByAsc(Expression<Func<TIndexItem, object>> key);

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
        IQuery<TIndexItem> ThenByDesc(Expression<Func<TIndexItem, object>> key);

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
        IQuery<TIndexItem> WithPaging(int skip, int take);

        /// <summary>
        /// Sets the number of items to skip before starting to return results, keeping the
        /// current limit. Allows an offset without a limit, which <see cref="WithPaging"/>
        /// cannot express.
        /// </summary>
        /// <param name="skip">
        /// The number of items to skip. Must be greater than or equal to 0.
        /// </param>
        /// <returns>A new query with the specified offset.</returns>
        IQuery<TIndexItem> WithSkip(int skip);

        /// <summary>
        /// Sets the maximum number of items to return, keeping the current offset. Unlike
        /// <see cref="WithPaging"/>, a limit of 0 is allowed and yields an empty result.
        /// </summary>
        /// <param name="take">
        /// The maximum number of items to return. Must be greater than or equal to 0.
        /// </param>
        /// <returns>A new query with the specified limit.</returns>
        IQuery<TIndexItem> WithTake(int take);

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
        IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> query);
    }
}
