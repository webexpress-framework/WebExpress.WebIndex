using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the parameter option expression of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeParameterOption<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the similarity value for the fuzzy search.
        /// </summary>
        public uint? Similarity { get; internal set; }

        /// <summary>
        /// Returns the distance value for the proximity search.
        /// </summary>
        public uint? Distance { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeParameterOption()
        {
        }

        /// <summary>
        /// Converts the options expression to a string.
        /// </summary>
        /// <returns>The options expression as a string.</returns>
        public override string ToString()
        {
            return $"{(Similarity is not null ? "~" + Similarity : "")} {(Distance is not null ? ":" + Distance : "")}".Trim();
        }
    }
}