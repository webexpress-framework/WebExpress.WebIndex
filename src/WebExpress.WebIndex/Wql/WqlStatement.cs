using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a WQL (WebExpress Query Language) statement with a specific index item type.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item. This type parameter must implement the IIndexItem interface.</typeparam>
    public class WqlStatement<TIndexItem> : IWqlStatement<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the original wql statement.
        /// </summary>
        public string Raw { get; internal set; }

        /// <summary>
        /// Returns the filter expression.
        /// </summary>
        public WqlExpressionNodeFilter<TIndexItem> Filter { get; internal set; }

        /// <summary>
        /// Returns the order expression.
        /// </summary>
        public WqlExpressionNodeOrder<TIndexItem> Order { get; internal set; }

        /// <summary>
        /// Returns the partitioning expression.
        /// </summary>
        public WqlExpressionNodePartitioning<TIndexItem> Partitioning { get; internal set; }

        /// <summary>
        /// Returns true if there are any errors that occurred during parsing, false otherwise.
        /// </summary>
        public bool HasErrors => Error is not null;

        /// <summary>
        /// Returns the part in error of the original wql statement.
        /// </summary>
        public WqlExpressionError Error { get; internal set; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Returns the syntax tree of the wql query.
        /// </summary>
        public IWqlSyntaxTree<TIndexItem> AbstractSyntaxTree => new WqlSyntaxTree<TIndexItem>(Filter, Order, Partitioning);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="raw">The original wql statement.</param>
        internal WqlStatement(string raw)
        {
            Raw = raw;
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The data from the index.</returns>
        public IQueryable<TIndexItem> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            var filtered = Enumerable.Empty<TIndexItem>().AsQueryable();

            if (Filter is not null)
            {
                filtered = Filter.Apply(indexDocument).Select(x => indexDocument.DocumentStore.GetItem(x)).AsQueryable();
            }
            else
            {
                filtered = indexDocument?.DocumentStore.All.AsQueryable();
            }

            if (Order is not null)
            {
                filtered = Order.Apply(filtered);
            }

            if (Partitioning is not null)
            {
                filtered = Partitioning.Apply(filtered);
            }

            return filtered;
        }

        /// <summary>
        /// Converts the current wql statemment to a query.
        /// </summary>
        /// <returns>
        /// An query that represents a query for retrieving indexed items.
        /// </returns>
        public IQuery<TIndexItem> ToQuery()
        {
            var query = new Query<TIndexItem>() as IQuery<TIndexItem>;

            // filter
            if (Filter is not null)
            {
                // create the parameter: x => 
                var param = Expression.Parameter(typeof(TIndexItem), "x");

                // build the expression tree for the filter condition 
                var body = Filter.ToExpression(param);

                // wrap into a lambda: x => <body> 
                var lambda = Expression.Lambda<Func<TIndexItem, bool>>(body, param);

                // apply to the query 
                query = query.Where(lambda);
            }

            // order
            bool isFirst = true;
            foreach (var att in Order?.Attributes ?? [])
            {
                var param = Expression.Parameter(typeof(TIndexItem), "x");
                var propertyExpr = att.ToExpression(param);
                var propertyExprObj = Expression.Convert(propertyExpr, typeof(object)); // ensure object type for LINQ
                var keySelector = Expression.Lambda<Func<TIndexItem, object>>(propertyExprObj, param);

                if (isFirst)
                {
                    if (att.Descending)
                    {
                        query = query.OrderByDesc(keySelector);
                    }
                    else
                    {
                        query = query.OrderByAsc(keySelector);
                    }
                    isFirst = false;
                }
                else
                {
                    if (att.Descending)
                    {
                        query = query.ThenByDesc(keySelector);
                    }
                    else
                    {
                        query = query.ThenByAsc(keySelector);
                    }
                }
            }

            if (Partitioning is not null)
            {
                int take = -1;
                int skip = -1;

                foreach (var function in Partitioning.PartitioningFunctions)
                {
                    if (function.Operator == WqlExpressionNodePartitioningOperator.Take)
                    {
                        take = function.Value;
                    }
                    else if (function.Operator == WqlExpressionNodePartitioningOperator.Skip)
                    {
                        skip = function.Value;
                    }
                }

                query = query.WithPaging(take, skip);
            }

            return query;
        }

        /// <summary>
        /// Converts the WQL expression to a string.
        /// </summary>
        /// <returns>The WQL expression as a string.</returns>
        public override string ToString()
        {
            if (Error is not null)
            {
                return Raw;
            }

            return string.Format
            (
                "{0} {1} {2} {3}",
                Filter is not null ? Filter.ToString() : "",
                Order is not null ? Order.ToString() : "",
                Partitioning is not null ? Partitioning.ToString() : "",
                Error is not null ? Error.ToString() : ""
            ).Trim();
        }
    }
}