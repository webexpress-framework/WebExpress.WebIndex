using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents an exception that is thrown when a WQL parsing error occurs.
    /// </summary>
    public class WqlParseException : Exception
    {
        /// <summary>
        /// Returns the token that caused the exception.
        /// </summary>
        public IEnumerable<IWqlToken> Token { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The massage.</param>
        /// <param name="token">The token that caused the exception.</param>
        public WqlParseException(string message, IWqlToken token)
            : base(message)
        {
            Token = [token];
        }

        /// <summary>
        /// Initializes a new instance of the class with a specified error message 
        /// and the collection of WQL tokens being processed when the parsing 
        /// error occurred.
        /// </summary>
        /// <param name="message">
        /// The error message that describes the reason for the parsing exception.
        /// </param>
        /// <param name="tokens">
        /// The collection of WQL tokens that were being processed at the time the 
        /// exception was thrown.
        /// </param>
        public WqlParseException(string message, IEnumerable<IWqlToken> tokens)
            : base(message)
        {
            Token = tokens;
        }
    }
}