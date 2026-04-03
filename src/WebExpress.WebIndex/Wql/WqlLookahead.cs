using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents the result of a lookahead operation (ILA) during WQL parsing, providing 
    /// information about processed tokens and the syntactic validity of the input.
    /// </summary>
    public class WqlLookahead : IWqlLookahead
    {
        /// <summary>
        /// Returns the list of lookahead items representing each token that was
        /// successfully processed, including metadata such as token type.
        /// </summary>
        public IEnumerable<IWqlLookaheadToken> Items { get; init; } = [];

        /// <summary>
        /// Indicates whether the input is syntactically valid up to the current
        /// position.
        /// </summary>
        public bool IsValidSoFar { get; init; }

        /// <summary>
        /// Returns the type of the last token of the WQL expression.
        /// </summary>
        public WqlExpressionType LastExpressionType =>
            Items.LastOrDefault()?.ExpressionType ?? WqlExpressionType.None;

        /// <summary>
        /// Returns the set of tokens that would be syntactically valid at the
        /// current position (lookahead).
        /// </summary>
        public IEnumerable<WqlExpressionType> ExpectedNextTokens =>
            Items.LastOrDefault()?.ExpectedNextTokens ?? [WqlExpressionType.Attribute];

    }
}
