using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents s filter conditions for sets in a WQL expression node. A set condition is
    /// the disjunction of one equality per value, each typed against the left-hand side like
    /// a single <c>=</c>; this keeps nullable, enumeration and Guid attributes working and lets
    /// functions in the list be evaluated when the query runs instead of when it is built.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterConditionSet<TIndexItem> : WqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the parameter expressions.
        /// </summary>
        public IEnumerable<WqlExpressionNodeParameter<TIndexItem>> Parameters { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
        protected WqlExpressionNodeFilterConditionSet(string token)
            : base(token)
        {
        }

        /// <summary>
        /// Builds the disjunction of the equalities of the left-hand side with every value. On
        /// a text attribute each value is searched as a phrase, as <c>=</c> does.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>A boolean expression.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <c>Attribute</c> or <c>Parameters</c> is <c>null</c>.
        /// </exception>
        protected Expression ToAnyEqualExpression(ParameterExpression param)
        {
            if (Function is null)
            {
                ArgumentNullException.ThrowIfNull(Attribute);
            }

            ArgumentNullException.ThrowIfNull(Parameters);

            var left = LeftToExpression(param);

            return Parameters
                .Select(x => IsTextAttribute && !(x.Value?.IsNull ?? false)
                    ? WqlExpressionBuilder.MatchText(left, x.ToOperand(param, typeof(string), Culture), WqlTextMatchMode.Phrase, 0, 0, Culture)
                    : WqlExpressionBuilder.Compare(left, WqlComparisonOperator.Equal, x.ToOperand(param, left.Type, Culture), Culture))
                .DefaultIfEmpty(Expression.Constant(false))
                .Aggregate(Expression.OrElse);
        }

        /// <summary>
        /// Retrieves the items equal to any of the values, if the reverse index answers every
        /// single equality exactly.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="ids">The identifiers of the items equal to any value.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveAnyEqual(IIndexDocument<TIndexItem> indexDocument, out IEnumerable<Guid> ids)
        {
            var matches = new HashSet<Guid>();

            ids = matches;

            foreach (var parameter in Parameters ?? [])
            {
                if (!parameter.IsConstant ||
                    !TryRetrieveEqual(indexDocument, parameter.GetExactValue(), 0, 0, out var match))
                {
                    return false;
                }

                matches.UnionWith(match);
            }

            return true;
        }

        /// <summary>
        /// Converts the condition expression to a string.
        /// </summary>
        /// <returns>The condition expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", (object)Function ?? Attribute, Operator.ToLower(), string.Join(", ", Parameters ?? [])).Trim();
        }
    }
}
