using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Defines the contract for incremental lookahead analysis (ILA) in WQL queries.
    /// </summary>
    public interface IWqlLookahead
    {
        /// <summary>
        /// Returns the list of lookahead items representing each token that was
        /// successfully processed, including metadata such as token type.
        /// </summary>
        IEnumerable<IWqlLookaheadToken> Items { get; }

        /// <summary>
        /// Indicates whether the input is syntactically valid up to the current
        /// position.
        /// </summary>
        bool IsValidSoFar { get; }

        /// <summary>
        /// Returns the type of the last token of the WQL expression.
        /// </summary>
        WqlExpressionType LastExpressionType { get; }

        /// <summary>
        /// Returns the set of tokens that would be syntactically valid at the
        /// current position (lookahead).
        /// </summary>
        IEnumerable<WqlExpressionType> ExpectedNextTokens { get; }
    }
}
