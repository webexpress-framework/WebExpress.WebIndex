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
    /// <typeparam name="TIndexItem">The type of items in the index being queried.</typeparam>
    public class Query<TIndexItem> : IQuery<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly List<Expression<Func<TIndexItem, bool>>> _filters = [];
        private readonly List<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> _orderBys = [];

        /// <summary>
        /// Gets the collection of filters applied to index items as boolean 
        /// expressions.
        /// </summary>
        public IEnumerable<Expression<Func<TIndexItem, bool>>> Filters => _filters;

        /// <summary>
        /// Gets the collection of sorting criteria applied to the query.
        /// </summary>
        public IReadOnlyList<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> OrderBys => _orderBys.AsReadOnly();

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
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified 
        /// filtering, ordering, and pagination options.
        /// </summary>
        /// <param name="filters">
        /// A list of filter expressions that define the criteria used 
        /// to select items from the index. Each expression should return 
        /// true for items to include.
        /// </param>
        /// <param name="orderBys">
        /// An collection of expressions that specifies the property to use for ordering 
        /// the results in ascending order.
        /// ordering is applied.
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
            List<Expression<Func<TIndexItem, bool>>> filters,
            List<(Expression<Func<TIndexItem, object>> KeySelector, bool Descending)> orderBys,
            int? skip,
            int? take
        )
        {
            _filters = filters;
            _orderBys = orderBys;
            Skip = skip;
            Take = take;
        }

        /// <summary>
        /// Combines an existing query tree with an additional filter condition using a logical AND.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using AND.
        /// </param>
        /// <returns>A new query instance with the updated logical tree.</returns>
        public IQuery<TIndexItem> And(IQuery<TIndexItem> otherQuery)
        {
            ArgumentNullException.ThrowIfNull(otherQuery);

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            if (otherQuery.Filters.Any())
            {
                var combinedTree = BuildGroupedFilterTree(
                    Expression.AndAlso,
                    newFilters,
                    otherQuery.Filters.ToList());
                newFilters.Clear();
                newFilters.Add(combinedTree);
            }

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

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
        public IQuery<TIndexItem> And(Expression<Func<TIndexItem, bool>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr);

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            if (newFilters.Count != 0)
            {
                var combinedTree = BuildGroupedFilterTree(Expression.AndAlso, newFilters, new List<Expression<Func<TIndexItem, bool>>> { expr });
                newFilters.Clear();
                newFilters.Add(combinedTree);
            }
            else
            {
                newFilters.Add(expr);
            }

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

        /// <summary>
        /// Combines an existing query tree with an additional filter condition using a logical OR.
        /// </summary>
        /// <param name="otherQuery">
        /// The other query whose filters will be combined with the current query using OR.
        /// </param>
        /// <returns>A new query instance with the updated logical tree.</returns>
        public IQuery<TIndexItem> Or(IQuery<TIndexItem> otherQuery)
        {
            ArgumentNullException.ThrowIfNull(otherQuery);

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            if (otherQuery.Filters.Any())
            {
                var combinedTree = BuildGroupedFilterTree(
                    Expression.OrElse,
                    newFilters,
                    otherQuery.Filters.ToList());
                newFilters.Clear();
                newFilters.Add(combinedTree);
            }

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

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
        public IQuery<TIndexItem> Or(Expression<Func<TIndexItem, bool>> expr)
        {
            ArgumentNullException.ThrowIfNull(expr);

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            if (newFilters.Count != 0)
            {
                var combinedTree = BuildGroupedFilterTree(Expression.OrElse, newFilters, [expr]);
                newFilters.Clear();
                newFilters.Add(combinedTree);
            }
            else
            {
                newFilters.Add(expr);
            }

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

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
        public IQuery<TIndexItem> Where(params Expression<Func<TIndexItem, bool>>[] predicates)
        {
            ArgumentNullException.ThrowIfNull(predicates);

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            newFilters.AddRange(predicates);

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

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
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, string>> selector, string value)
        {
            var param = selector.Parameters[0];

            var body = Expression.Equal
            (
                selector.Body,
                Expression.Constant(value, typeof(string))
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, Guid>> selector, Guid value)
        {
            var param = selector.Parameters[0];

            var body = Expression.Equal
            (
                selector.Body,
                Expression.Constant(value, typeof(Guid))
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereEquals(Expression<Func<TIndexItem, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters)
            {
                predicate
            };
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            return new Query<TIndexItem>(newFilters, neworderBys, Skip, Take);
        }

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
        public IQuery<TIndexItem> WhereEqualsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);

            var left = Expression.Call(selector.Body, toLower);
            var right = Expression.Constant(value.ToLower());

            var body = Expression.Equal(left, right);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, string>> selector, string value)
        {
            var param = selector.Parameters[0];

            var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);

            var body = Expression.Call
            (
                selector.Body,
                contains,
                Expression.Constant(value, typeof(string))
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereContains(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
            var any = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var sParam = Expression.Parameter(typeof(string), "s");
            var containsBody = Expression.Call
            (
                sParam,
                contains,
                Expression.Constant(value, typeof(string))
            );
            var containsLambda = Expression.Lambda<Func<string, bool>>(containsBody, sParam);

            var body = Expression.Call
            (
                any,
                selector.Body,
                containsLambda
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);

            var left = Expression.Call(selector.Body, toLower);
            var right = Expression.Constant(value.ToLower());

            var body = Expression.Call(left, contains, right);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereContainsIgnoreCase(Expression<Func<TIndexItem, IEnumerable<string>>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
            var any = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var sParam = Expression.Parameter(typeof(string), "s");

            var sToLower = Expression.Call(sParam, toLower);
            var valueToLower = Expression.Constant(value.ToLower());

            var containsBody = Expression.Call(sToLower, contains, valueToLower);
            var containsLambda = Expression.Lambda<Func<string, bool>>(containsBody, sParam);

            var body = Expression.Call
            (
                any,
                selector.Body,
                containsLambda
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereStartsWith(Expression<Func<TIndexItem, string>> selector, string value)
        {
            var param = selector.Parameters[0];

            var startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)]);

            var body = Expression.Call
            (
                selector.Body,
                startsWith,
                Expression.Constant(value, typeof(string))
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereStartsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)]);

            var left = Expression.Call(selector.Body, toLower);
            var right = Expression.Constant(value.ToLower());

            var body = Expression.Call(left, startsWith, right);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereEndsWith(Expression<Func<TIndexItem, string>> selector, string value)
        {
            var param = selector.Parameters[0];

            var endsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)]);

            var body = Expression.Call
            (
                selector.Body,
                endsWith,
                Expression.Constant(value, typeof(string))
            );

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

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
        public IQuery<TIndexItem> WhereEndsWithIgnoreCase(Expression<Func<TIndexItem, string>> selector, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var param = selector.Parameters[0];

            var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var endsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)]);

            var left = Expression.Call(selector.Body, toLower);
            var right = Expression.Constant(value.ToLower());

            var body = Expression.Call(left, endsWith, right);

            return Where(Expression.Lambda<Func<TIndexItem, bool>>(body, param));
        }

        /// <summary>
        /// Adds an ascending sort order to the query's sorting criteria.
        /// </summary>
        /// <param name="key">
        /// An expression that identifies the key to use for ordering the results in ascending 
        /// order. Cannot be null.
        /// </param>
        /// <returns>
        /// A query object with the specified ascending sort order applied.
        /// </returns>
        public IQuery<TIndexItem> OrderByAsc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            var orderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys)
            {
                (key, false)
            };

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);

            return new Query<TIndexItem>(newFilters, orderBys, Skip, Take);
        }

        /// <summary>
        /// Adds a descending sort order to the query's sorting criteria.
        /// </summary>
        /// <param name="key">An expression that identifies the key to sort the results by 
        /// in descending order. Cannot be null.
        /// </param>
        /// <returns>
        /// A new query object with the specified descending sort order applied.
        /// </returns>
        public IQuery<TIndexItem> OrderByDesc(Expression<Func<TIndexItem, object>> key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            var orderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys)
            {
                (key, true)
            };

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);

            return new Query<TIndexItem>(newFilters, orderBys, Skip, Take);
        }

        /// <summary>
        /// Adds another ascending sort order (ThenBy) criterion.
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
            // simply add as additional orderBy
            return OrderByAsc(key);
        }

        /// <summary>
        /// Adds another descending sort order (ThenByDescending) criterion.
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
            // simply add as additional orderBy, set descending
            return OrderByDesc(key);
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
            ArgumentOutOfRangeException.ThrowIfNegative(skip, nameof(skip));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take, nameof(take));

            var newFilters = new List<Expression<Func<TIndexItem, bool>>>(_filters);
            var neworderBys = new List<(Expression<Func<TIndexItem, object>>, bool)>(_orderBys);

            return new Query<TIndexItem>(newFilters, neworderBys, skip, take);
        }

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
        public virtual IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> query)
        {
            // filters
            foreach (var filter in Filters)
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
        /// Combines existing filters with either logical AND or OR, supporting multiple filters.
        /// </summary>
        /// <param name="logicalOperator">The logical operator (AND or OR) to use when combining filters.</param>
        /// <param name="existingFilters">The current filters in the query.</param>
        /// <param name="newFilters">A list of filters from the other query to combine.</param>
        /// <returns>A combined filter expression that represents the logical tree.</returns>
        private static Expression<Func<TIndexItem, bool>> BuildGroupedFilterTree
        (
            Func<Expression, Expression, BinaryExpression> logicalOperator,
            List<Expression<Func<TIndexItem, bool>>> existingFilters,
            List<Expression<Func<TIndexItem, bool>>> newFilters
        )
        {
            var param = Expression.Parameter(typeof(TIndexItem), "x");

            // Combine existing filters
            var combinedExistingBody = existingFilters
                .Select(filter => Expression.Invoke(filter, param))
                .Aggregate(logicalOperator);

            // Combine new filters
            var combinedNewBody = newFilters
                .Select(filter => Expression.Invoke(filter, param))
                .Aggregate(logicalOperator);

            // Combine both trees
            var finalBody = logicalOperator(combinedExistingBody, combinedNewBody);

            return Expression.Lambda<Func<TIndexItem, bool>>(finalBody, param);
        }
    }
}