namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a token of the wql syntax.
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
