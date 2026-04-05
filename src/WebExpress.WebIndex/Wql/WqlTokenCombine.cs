using System;
using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a token of the wql syntax.
    /// </summary>
    public class WqlTokenCombine : IWqlToken
    {
        /// <summary>
        /// Returns the starting position of the token in the raw statement.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Returns the length of the token (start + length = end) in the raw statement.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Checks if the token is empty.
        /// </summary>
        /// <returns>True if no value is stored, false otherwise.</returns>
        public bool IsEmpty => Value is null || Value.Length == 0;

        /// <summary>
        /// Returns the token value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class by combining the specified WQL 
        /// tokens into a single token for query processing.
        /// </summary>
        /// <param name="tokens">
        /// An array of IWqlToken instances representing the tokens to be combined. This 
        /// parameter cannot be null or empty.
        /// </param>
        internal WqlTokenCombine(params IWqlToken[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                throw new ArgumentException("Token list cannot be null or empty.", nameof(tokens));
            }

            var nonNullTokens = tokens.Where(t => t != null).ToList();

            if (nonNullTokens.Count == 0)
            {
                throw new ArgumentException("Token list must contain at least one non-null token.", nameof(tokens));
            }

            var firstToken = nonNullTokens.First();
            var lastToken = nonNullTokens.Last();

            Offset = firstToken.Offset;
            Length = (lastToken.Offset + lastToken.Length) - Offset;

            Value = string.Concat(nonNullTokens.Select(t => t.Value));
        }

        /// <summary>
        /// Initializes a new instance of the class by combining the specified WQL 
        /// tokens into a single token for query processing.
        /// </summary>
        /// <param name="tokens">
        /// An array of IWqlToken instances representing the tokens to be combined. This 
        /// parameter cannot be null or empty.
        /// </param>
        internal WqlTokenCombine(IEnumerable<IWqlToken> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentException("Token list cannot be null.", nameof(tokens));
            }

            var nonNullTokens = tokens.Where(t => t != null).ToList();

            if (nonNullTokens.Count == 0)
            {
                throw new ArgumentException("Token list must contain at least one non-null token.", nameof(tokens));
            }

            var firstToken = nonNullTokens.First();
            var lastToken = nonNullTokens.Last();

            Offset = firstToken.Offset;
            Length = (lastToken.Offset + lastToken.Length) - Offset;

            Value = string.Concat(nonNullTokens.Select(t => t.Value));
        }

        /// <summary>
        /// Converts the token to a string.
        /// </summary>
        /// <returns>The token as a string.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}
