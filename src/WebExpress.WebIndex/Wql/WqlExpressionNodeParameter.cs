using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the parameter expression of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeParameter<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the value expressions.
        /// </summary>
        public WqlExpressionNodeValue<TIndexItem> Value { get; internal set; }

        /// <summary>
        /// Returns the function expressions.
        /// </summary>
        public WqlExpressionNodeFilterFunction<TIndexItem> Function { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeParameter()
        {
        }

        /// <summary>
        /// Returns the value.
        /// </summary>
        /// <returns>The value.</returns>
        public object GetValue()
        {
            return Function is not null ? Function.Execute() : Value.GetValue();
        }

        /// <summary>
        /// Converts the parameter expression to a string.
        /// </summary>
        /// <returns>The parameter expression as a string.</returns>
        public override string ToString()
        {
            return Value is not null ? Value.ToString() : Function.ToString().Trim();
        }
    }
}