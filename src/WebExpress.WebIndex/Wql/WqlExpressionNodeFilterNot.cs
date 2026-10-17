using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the negation of a filter in a WQL statement, written <c>not</c> in front of a
    /// condition or a group, as in <c>not (Text ~ 'a' or Text ~ 'b')</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterNot<TIndexItem> : WqlExpressionNodeFilter<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the negated filter.
        /// </summary>
        public WqlExpressionNodeFilter<TIndexItem> NegatedFilter { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeFilterNot()
        {
        }

        /// <summary>
        /// Applies the filter to the index as the complement of the negated filter. The
        /// conditions retrieve their matches without a limit, so that no match can turn into
        /// a false hit of the complement.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The data ids from the index.</returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            var excluded = new HashSet<Guid>(NegatedFilter.Apply(indexDocument));

            return indexDocument.All
                .Select(x => x.Id)
                .Where(x => !excluded.Contains(x));
        }

        /// <summary>
        /// Builds the negation of the expression of the negated filter.
        /// </summary>
        /// <param name="param">The parameter representing the index item in the expression.</param>
        /// <returns>The negated expression.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Not(NegatedFilter.ToExpression(param));
        }

        /// <summary>
        /// Converts the filter expression to a string.
        /// </summary>
        /// <returns>The filter expression as a string.</returns>
        public override string ToString()
        {
            return NegatedFilter is WqlExpressionNodeFilterBinary<TIndexItem>
                ? $"not {NegatedFilter}"
                : $"not ({NegatedFilter})";
        }
    }
}
