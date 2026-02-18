using System;
using System.Collections.Generic;
using System.Globalization;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents an abstract base class for a WQL expression node filter condition.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterCondition<TIndexItem> : IWqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the attribute expression.
        /// </summary>
        public WqlExpressionNodeAttribute<TIndexItem> Attribute { get; internal set; }

        /// <summary>
        /// Returns the operator expression.
        /// </summary>
        public string Operator { get; internal set; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
        protected WqlExpressionNodeFilterCondition(string token)
        {
            Operator = token;
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public abstract IEnumerable<Guid> Apply();

        /// <summary>
        /// Applies the current filter condition to the specified query and returns the 
        /// resulting query.
        /// </summary>
        /// <param name="query">
        /// The query to which the filter condition will be applied. This parameter must 
        /// not be null.
        /// </param>
        /// <returns>
        /// An <see cref="IQuery{TIndexItem}"/> representing the filtered query if a 
        /// condition exists; otherwise, the original query.
        /// </returns>
        public abstract IQuery<TIndexItem> Apply(IQuery<TIndexItem> query);
    }
}