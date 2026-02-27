using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The data from the index.</returns>
        public virtual IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            return Condition?.Apply(indexDocument) ?? [];
        }

        /// <summary>
        /// Builds a LINQ expression representing the filter condition
        /// contained in this filter node.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => ...</c>).
        /// </param>
        /// <returns>
        /// The expression produced by the underlying filter condition, or
        /// expression constant with <c>true</c> if no condition exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <c>Condition</c> is <c>null</c>.
        /// </exception>
        public virtual Expression ToExpression(ParameterExpression param)
        {
            ArgumentNullException.ThrowIfNull(Condition);

            return Condition.ToExpression(param);
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