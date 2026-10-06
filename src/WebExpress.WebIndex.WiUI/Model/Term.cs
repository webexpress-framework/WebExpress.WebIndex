namespace WebExpress.WebIndex.WiUI.Model
{
    /// <summary>
    /// Represents an index term.
    /// </summary>
    public class Term
    {
        /// <summary>
        /// Returns or sets the collection of document IDs associated with the term.
        /// </summary>
        public IEnumerable<Guid> DocumentIDs { get; set; } = [];

        /// <summary>
        /// Returns a comma-separated string of document IDs associated with the term.
        /// </summary>
        public uint Documents => (uint)DocumentIDs.Count();

        /// <summary>
        /// Returns or sets the term.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Returns or sets the frequency of the term in the documents.
        /// </summary>
        public uint Frequency { get; set; }

        /// <summary>
        /// Returns or sets the height of the term tree.
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// Returns or sets the balance factor of the term tree.
        /// </summary>
        public uint Balance { get; set; }
    }
}
