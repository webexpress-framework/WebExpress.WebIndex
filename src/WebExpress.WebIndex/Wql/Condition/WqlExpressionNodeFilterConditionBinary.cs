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
        /// Returns true if the condition gives a meaning to a similarity or a distance. Both
        /// options belong to the text search; on any other comparison the parser reports them
        /// instead of silently ignoring what the statement asks for. Conditions written before
        /// this check existed keep accepting them.
        /// </summary>
        protected internal virtual bool AcceptsOptions => true;

        /// <summary>
        /// Returns true if the condition applies to text only, so that the parser rejects it
        /// on an attribute of another type.
        /// </summary>
        protected internal virtual bool RequiresText => false;

        /// <summary>
        /// Returns the similarity option, or 0 if none was given.
        /// </summary>
        protected uint Similarity => Options?.Similarity ?? 0;

        /// <summary>
        /// Returns the distance option, or 0 if none was given.
        /// </summary>
        protected uint Distance => Options?.Distance ?? 0;

        /// <summary>
        /// Converts the condition expression to a string.
        /// </summary>
        /// <returns>The condition expression as a string.</returns>
        public override string ToString()
        {
            return $"{(object)Function ?? Attribute} {Operator} {Parameter} {Options}".Trim();
        }
    }
}
