using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Describes the binary condition expression of a wql statement.
    /// </summary>
    /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterConditionBinary<TIndexItem>(string token) : WqlExpressionNodeFilterCondition<TIndexItem>(token)
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the parameter expression.
        /// </summary>
        public WqlExpressionNodeParameter<TIndexItem> Parameter { get; internal set; }

        /// <summary>
        /// Returns the parameter options expression.
        /// </summary>
        public WqlExpressionNodeParameterOption<TIndexItem> Options { get; internal set; } = new WqlExpressionNodeParameterOption<TIndexItem>();

        /// <summary>
        /// Converts the condition expression to a string.
        /// </summary>
        /// <returns>The condition expression as a string.</returns>
        public override string ToString()
        {
            return $"{Attribute} {Operator} {Parameter} {Options}".Trim();
        }
    }
}