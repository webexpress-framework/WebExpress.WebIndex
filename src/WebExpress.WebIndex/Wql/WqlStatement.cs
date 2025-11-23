using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
        /// Returns the index document.
        /// </summary>
        public IIndexDocument<TIndexItem> IndexDocument { get; set; }

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
        public IEnumerable<IWqlExpressionNode<TIndexItem>> SyntaxTree
        {
            get
            {
                var nodes = new List<IWqlExpressionNode<TIndexItem>>();

                if (Filter is not null)
                {
                    nodes.Add(Filter);
                }

                if (Order is not null)
                {
                    nodes.Add(Order);
                }

                if (Partitioning is not null)
                {
                    nodes.Add(Partitioning);
                }

                return nodes;
            }
        }

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
        /// <returns>The data from the index.</returns>
        public IQueryable<TIndexItem> Apply()
        {
            var filtered = Enumerable.Empty<TIndexItem>().AsQueryable();

            if (Filter is not null)
            {
                filtered = Filter.Apply().Select(x => IndexDocument.DocumentStore.GetItem(x)).AsQueryable();
            }
            else
            {
                filtered = IndexDocument?.DocumentStore.All.AsQueryable();
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
        /// Applies the filter to the index.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <returns>The data ids from the index.</returns>
        public IQueryable Apply(Type dataType)
        {
            return Apply();
        }

        /// <summary>
        /// Applies the filter to the unfiltered data object.
        /// </summary>
        /// <param name="unfiltered">The unfiltered data.</param>
        /// <returns>The filtered data.</returns>
        public IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> unfiltered)
        {
            var filtered = unfiltered;

            if (Filter is not null)
            {
                filtered = Filter.Apply(filtered);
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
        /// Returns the sql query string.
        /// </summary>
        /// <returns>The sql part of the node.</returns>
        public string GetSqlQueryString()
        {
            var sql = new StringBuilder();
            var name = typeof(TIndexItem).Name;

            sql.Append($"select * from {name}");

            if (Filter is not null)
            {
                sql.Append($" where {Filter.GetSqlQueryString()}");
            }

            //if (Order != null)
            //{
            //    sql.Add(Order);
            //}

            //if (Partitioning != null)
            //{
            //    sql.Add(Partitioning);
            //}

            return sql.ToString();
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