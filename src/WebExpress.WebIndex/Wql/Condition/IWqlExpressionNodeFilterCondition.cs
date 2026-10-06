using System.Globalization;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// A WQL node representing a single filter condition, i.e. a comparison of an attribute against
    /// a value (such as <c>name = 'foo'</c>). When applied, it returns the items that satisfy the comparison.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IWqlExpressionNodeFilterCondition<TIndexItem> : IWqlExpressionNodeApply<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the attribute expression.
        /// </summary>
        WqlExpressionNodeAttribute<TIndexItem> Attribute { get; }

        /// <summary>
        /// Returns the operator expression.
        /// </summary>
        string Operator { get; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        CultureInfo Culture { get; }
    }
}
