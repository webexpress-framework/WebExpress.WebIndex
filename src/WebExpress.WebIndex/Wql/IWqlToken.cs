namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// One token from a WQL query string, as produced by the lexer — a single syntactic unit (such
    /// as a word, operator, or value) together with its position and length in the raw query.
    /// WQL (WebExpress Query Language) is the query language used to search the index.
    /// </summary>
    public interface IWqlToken
    {
        /// <summary>
        /// Returns the starting position of the token in the raw statement.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Returns the length of the token (start + length = end) in the raw statement.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Checks if the token is empty.
        /// </summary>
        /// <returns>True if no value is stored, false otherwise.</returns>
        bool IsEmpty { get; }

        /// <summary>
        /// Returns the token value.
        /// </summary>
        string Value { get; }
    }
}
