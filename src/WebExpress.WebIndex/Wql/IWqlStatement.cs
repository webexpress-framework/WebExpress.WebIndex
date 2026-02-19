using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a WQL (WebExpress Query Language) statement with a specific index item type.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IWqlStatement<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the original wql statement.
        /// </summary>
        string Raw { get; }

        /// <summary>
        /// Returns true if there are any errors that occurred during parsing, false otherwise.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Returns the part in error of the original wql statement.
        /// </summary>
        WqlExpressionError Error { get; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        CultureInfo Culture { get; }

        /// <summary>
        /// Returns the filter expression.
        /// </summary>
        WqlExpressionNodeFilter<TIndexItem> Filter { get; }

        /// <summary>
        /// Returns the order expression.
        /// </summary>
        WqlExpressionNodeOrder<TIndexItem> Order { get; }

        /// <summary>
        /// Returns the partitioning expression.
        /// </summary>
        WqlExpressionNodePartitioning<TIndexItem> Partitioning { get; }

        /// <summary>
        /// Returns the syntax tree of the wql query.
        /// </summary>
        IEnumerable<IWqlExpressionNode<TIndexItem>> AbstractSyntaxTree { get; }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        IQueryable<TIndexItem> Apply();

        /// <summary>
        /// Converts the current wql statemment to a query.
        /// </summary>
        /// <returns>
        /// An query that represents a query for retrieving indexed items.
        /// </returns>
        IQuery<TIndexItem> ToQuery();
    }
}
