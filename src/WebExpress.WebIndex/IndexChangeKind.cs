namespace WebExpress.WebIndex
{
    /// <summary>
    /// The kinds of change an index document reports.
    /// </summary>
    public enum IndexChangeKind
    {
        /// <summary>
        /// An item was added.
        /// </summary>
        Insert,

        /// <summary>
        /// An item was replaced by a newer version.
        /// </summary>
        Update,

        /// <summary>
        /// An item was removed.
        /// </summary>
        Delete,

        /// <summary>
        /// Every item of the document was removed.
        /// </summary>
        Clear
    }
}
