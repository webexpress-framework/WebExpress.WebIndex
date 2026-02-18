using System;
using System.Collections.Generic;
using WebExpress.WebIndex.Queries;
using WebExpress.WebIndex.Wql.Condition;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the filter expression of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilter<TIndexItem> : IWqlExpressionNodeApply<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the condition expression.
        /// </summary>
        public WqlExpressionNodeFilterCondition<TIndexItem> Condition { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeFilter()
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data from the index.</returns>
        public virtual IEnumerable<Guid> Apply()
        {
            return Condition?.Apply() ?? [];
        }

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
        public virtual IQuery<TIndexItem> Apply(IQuery<TIndexItem> query)
        {
            return Condition?.Apply(query) ?? query;
        }

        /// <summary>
        /// Converts the filter expression to a string.
        /// </summary>
        /// <returns>The filter expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0}", Condition).Trim();
        }
    }
}