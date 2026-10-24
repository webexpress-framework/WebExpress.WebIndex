using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents the range conditions of a WQL expression node, which compare the left-hand
    /// side with a lower and an upper bound, as in <c>Price between 10 and 20</c>. Both bounds
    /// belong to the range. A range is a kind of its own next to binary and set conditions,
    /// since the <c>and</c> between its bounds is part of the condition and not the logical
    /// operator.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterConditionRange<TIndexItem> : WqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the lower bound.
        /// </summary>
        public WqlExpressionNodeParameter<TIndexItem> Lower { get; internal set; }

        /// <summary>
        /// Returns the upper bound.
        /// </summary>
        public WqlExpressionNodeParameter<TIndexItem> Upper { get; internal set; }

        /// <summary>
        /// Returns the bounds in order.
        /// </summary>
        public IEnumerable<WqlExpressionNodeParameter<TIndexItem>> Parameters => new[] { Lower, Upper }.Where(x => x is not null);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
        protected WqlExpressionNodeFilterConditionRange(string token)
            : base(token)
        {
        }

        /// <summary>
        /// Builds the test whether the left-hand side lies within both bounds.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>A boolean expression that is false if the left-hand side is null.</returns>
        protected Expression ToWithinExpression(ParameterExpression param)
        {
            if (Function is null)
            {
                ArgumentNullException.ThrowIfNull(Attribute);
            }

            ArgumentNullException.ThrowIfNull(Lower);
            ArgumentNullException.ThrowIfNull(Upper);

            var left = LeftToExpression(param);

            return Expression.AndAlso
            (
                WqlExpressionBuilder.Compare(left, WqlComparisonOperator.GreaterThanOrEqual, Lower.ToOperand(param, left.Type, Culture), Culture),
                WqlExpressionBuilder.Compare(left, WqlComparisonOperator.LessThanOrEqual, Upper.ToOperand(param, left.Type, Culture), Culture)
            );
        }

        /// <summary>
        /// Retrieves the items within both bounds as the intersection of two range queries,
        /// if the attribute has a numeric reverse index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="ids">The identifiers of the items within the range.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveWithin(IIndexDocument<TIndexItem> indexDocument, out IEnumerable<Guid> ids)
        {
            ids = null;

            if (Lower is null || Upper is null || !Lower.IsConstant || !Upper.IsConstant ||
                !TryRetrieveNumber(indexDocument, Lower.GetExactValue(), IndexRetrieveMethod.GreaterThanOrEqual, out var above) ||
                !TryRetrieveNumber(indexDocument, Upper.GetExactValue(), IndexRetrieveMethod.LessThanOrEqual, out var below))
            {
                return false;
            }

            ids = above.Intersect(below);

            return true;
        }

        /// <summary>
        /// Converts the condition expression to a string.
        /// </summary>
        /// <returns>The condition expression as a string.</returns>
        public override string ToString()
        {
            return $"{(object)Function ?? Attribute} {Operator} {Lower} and {Upper}".Trim();
        }
    }
}
