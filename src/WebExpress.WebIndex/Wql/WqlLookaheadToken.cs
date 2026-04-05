using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a single processed token within a lookahead analysis,
    /// including the token itself and additional metadata.
    /// </summary>
    public sealed class WqlLookaheadToken : IWqlLookaheadToken
    {
        /// <summary>
        /// Returns the original token as produced by the tokenizer.
        /// </summary>
        public IWqlToken Token { get; }

        /// <summary>
        /// Returns the type of the WQL expression represented by this instance.
        /// </summary>
        public WqlExpressionType ExpressionType { get; }

        /// <summary>
        /// Returns the set of tokens that would be syntactically valid at the
        /// current position (lookahead).
        /// </summary>
        public IEnumerable<WqlExpressionType> ExpectedNextTokens { get; init; } = [];

        /// <summary>
        /// Initializes a new instance of the class using the specified WqlToken.
        /// </summary>
        /// <param name="token">
        /// The WqlToken to associate with this lookahead token. This parameter cannot 
        /// be null.
        /// </param>
        /// <param name="expressionType">
        /// The type of expression represented by the token.
        /// </param>
        public WqlLookaheadToken(IWqlToken token, WqlExpressionType expressionType)
        {
            ArgumentNullException.ThrowIfNull(token);

            Token = token;
            ExpressionType = expressionType;
        }

        /// <summary>
        /// Returns a string that represents the value of the current token.
        /// </summary>
        /// <returns>
        /// A string containing the textual representation of the token associated 
        /// with this instance.
        /// </returns>
        public override string ToString()
        {
            return Token.ToString();
        }
    }
}
