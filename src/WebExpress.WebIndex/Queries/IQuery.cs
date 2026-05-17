using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Queries
{
    /// <summary>
    /// Represents a composable query definition for retrieving items from an index, supporting 
    /// filtering, sorting, including related data, and paging operations.
    /// </summary>
    /// <remarks>
    /// The class provides a fluent API for building complex queries against an index. It allows 
    /// chaining of filtering, sorting, inclusion of related data, and paging options before
    /// execution. This class is typically used to construct queries that can be executed by 
    /// a repository or data provider supporting the specified query semantics. Instances of 
    /// this class are immutable; each method returns a new query instance with the specified 
    /// modification applied.
    /// </remarks>
    /// <typeparam name="TIndexItem">
    /// The type of items in the index to be queried.
    /// </typeparam>
    public interface IQuery<TIndexItem>
       where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the collection of filter expressions applied to the index items.
        /// </summary>
        IEnumerable<Expression<Func<TIndexItem, bool>>> Filters { get; }

        /// <summary>
        /// Gets the collection of sorting criteria applied to the query.
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
        /// Combines an existing query tree with an additional filter condition using a logical AND.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using AND.
        /// </param>
        /// <returns>A new query instance with the updated logical tree.</returns>
        IQuery<TIndexItem> And(IQuery<TIndexItem> otherQuery);

        /// <summary>
        /// Combines an existing query tree with an additional filter condition 
        /// using a logical AND. If filters already exist, creates a combined tree.
        /// </summary>
        /// <param name="expr">
        /// An expression representing a condition to combine with the existing 
        /// filters using AND.
        /// </param>
        /// <returns>
        /// An updated query combining the existing filters with the specified 
        /// condition using logical AND.
        /// </returns>
        IQuery<TIndexItem> And(Expression<Func<TIndexItem, bool>> expr);

        /// <summary>
        /// Combines an existing query tree with an additional filter condition using a logical OR.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using OR.
        /// </param>
        /// <returns>A new query instance with the updated logical tree.</returns>
        IQuery<TIndexItem> Or(IQuery<TIndexItem> otherQuery);

        /// <summary>
        /// Combines an existing query tree with an additional filter condition 
        /// using a logical OR. If filters already exist, creates a combined tree.
        /// </summary>
        /// <param name="expr">
        /// An expression representing a condition to combine with the existing 
        /// filters using OR.
        /// </param>
        /// <returns>
        /// An updated query with the existing filters combined with the 
        /// specified condition using logical OR.
        /// </returns>
        IQuery<TIndexItem> Or(Expression<Func<TIndexItem, bool>> expr);

        /// <summary>
        /// Filters the query results based on a specified predicate expression.
        /// </summary>
        /// <param name="predicates">
        /// An array of expressions that define the conditions each item in the query 
        /// must satisfy. Each predicate is applied as a filter to the query results. 
        /// Cannot be null.
        /// </param>
        /// <returns>
        /// A new query that contains only the items that satisfy the specified predicate.
        /// </returns>
        IQuery<TIndexItem> Where(params Expression<Func<TIndexItem, bool>>[] predicates);

        /// <summary>
        /// Filters the query to include only items where the specified string property 
        /// equals the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
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
        /// Filters the query to include only items where the specified string property 
        /// equals the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// </param>
        /// <param name="value">
        /// The value to compare against the selected property. Can be null to match items 
        /// with a null property value.
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
        /// filter items in the query.
        /// </param>
        /// <returns>
        /// A query that includes only items matching the specified predicate.
        /// </returns>
        IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, bool>> predicate);

        /// <summary>
        /// Filters the query to include only items where the specified string property equals 
        /// the given value, using a case-insensitive comparison.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// </param>
        /// <param name="value">
        /// The value to compare against the selected property, using a case-insensitive 
        /// comparison. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that contains only items where the selected property equals the specified 
        /// value, ignoring case.
        /// </returns>
        IQuery<TIndexItem> WhereEqualsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property contains 
        /// the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of each item to be searched for 
        /// the specified value.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the selected property. The comparison may 
        /// be case-sensitive or case-insensitive depending on the implementation.
        /// </param>
        /// <returns>
        /// A query that returns items where the selected property contains the specified value.
        /// </returns>
        IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified collection property 
        /// contains the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings to search within each item.
        /// </param>
        /// <param name="value">
        /// The value to search for within the selected collection. Cannot be null.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected collection contains the specified value.
        /// </returns>
        IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

        /// <summary>
        /// Filters the query to include items where the specified string property contains the 
        /// given value, using a case-insensitive comparison.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to search within.
        /// </param>
        /// <param name="value">
        /// The substring to search for within the selected property. The comparison is 
        /// case-insensitive.
        /// </param>
        /// <returns>
        /// A query that returns only items where the selected property contains the 
        /// specified value, ignoring case.
        /// </returns>
        IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string collection contains 
        /// the given value, using a case-insensitive comparison.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the collection of strings from each item to be searched.
        /// </param>
        /// <param name="value">
        /// The value to search for within the selected string collections. The comparison is 
        /// case-insensitive.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected string collections contain the specified 
        /// value, ignoring case.
        /// </returns>
        IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property 
        /// starts with the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
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
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property of the index item to compare.
        /// </param>
        /// <param name="value">
        /// The string value to compare against the start of the selected property. The 
        /// comparison is case-insensitive.
        /// </param>
        /// <returns>
        /// A query that returns items whose selected string property starts with the 
        /// specified value, ignoring case.
        /// </returns>
        IQuery<TIndexItem> WhereStartsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property ends 
        /// with the given value.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// </param>
        /// <param name="value">
        /// The substring to compare against the end of the selected property value. 
        /// Cannot be null.
        /// </param>
        /// <returns>An collection that contains items whose selected property values end 
        /// with the specified substring.
        /// </returns>
        IQuery<TIndexItem> WhereEndsWith(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Filters the query to include only items where the specified string property ends 
        /// with the given value, using a case-insensitive comparison.
        /// </summary>
        /// <param name="selector">
        /// An expression that selects the string property to evaluate for each item.
        /// </param>
        /// <param name="value">
        /// The substring to compare against the end of the selected property value. The 
        /// comparison is case-insensitive.
        /// </param>
        /// <returns>
        /// A query that contains only items whose selected property value ends with the 
        /// specified substring, ignoring case.
        /// </returns>
        IQuery<TIndexItem> WhereEndsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value);

        /// <summary>
        /// Specifies an ascending sort order for the query results based on the given key expression.
        /// </summary>
        /// <param name="key">
        /// An expression that identifies the key to use for ordering the results in ascending 
        /// order. Cannot be null.
        /// </param>
        /// <returns>
        /// A query object with the specified ascending sort order applied.
        /// </returns>
        IQuery<TIndexItem> OrderByAsc(Expression<Func<TIndexItem, object>> key);

        /// <summary>
        /// Specifies a descending sort order for the query results based on the given key expression.
        /// </summary>
        /// <param name="key">An expression that identifies the key to sort the results by 
        /// in descending order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query object with the specified descending sort order applied.
        /// </returns>
        IQuery<TIndexItem> OrderByDesc(Expression<Func<TIndexItem, object>> key);

        /// <summary>
        /// Adds a secondary ascending sort order to the query based on the specified key expression.
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
        /// Adds a secondary descending sort order to the query based on the specified key expression.
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
        /// Applies the specified query specification to the given queryable source, including 
        /// filtering, sorting, paging, and related entity inclusion.
        /// </summary>
        /// <param name="query">The source queryable collection to which the specification 
        /// will be applied.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{TIndexItem}"/> representing the source query with the 
        /// specification's filters, sorting, includes, and paging applied.
        /// </returns>
        IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> query);
    }
}
