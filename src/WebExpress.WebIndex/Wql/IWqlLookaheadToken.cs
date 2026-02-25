using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Defines an interface for a token that supports lookahead functionality during
    /// WQL parsing.
    /// </summary>
    public interface IWqlLookaheadToken
    {
        /// <summary>
        /// Returns the original token as produced by the tokenizer.
        /// </summary>
        IWqlToken Token { get; }

        /// <summary>
        /// Returns the type of the WQL expression represented by this instance.
        /// </summary>
        WqlExpressionType ExpressionType { get; }

        /// <summary>
        /// Returns the set of tokens that would be syntactically valid at the
        /// current position (lookahead).
        /// </summary>
        IEnumerable<WqlExpressionType> ExpectedNextTokens { get; }
    }
}
