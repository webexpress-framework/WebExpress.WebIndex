namespace WebExpress.WebIndex
{
    /// <summary>
    /// Where an index keeps its data: entirely in memory (fast, not persistent) or in a file on
    /// disk (persistent, larger than memory). Chosen when an index is created.
    /// </summary>
    public enum IndexType
    {
        /// <summary>
        /// The index is managed in memory.
        /// </summary>
        Memory,

        /// <summary>
        /// The index is managed in a file.
        /// </summary>
        Storage
    }
}
