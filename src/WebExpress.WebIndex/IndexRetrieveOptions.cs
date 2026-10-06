namespace WebExpress.WebIndex
{
    /// <summary>
    /// The settings that tune a search query: how many results to return at most, the retrieval
    /// method, the word distance for proximity searches, and the minimum similarity for fuzzy searches.
    /// </summary>
    public struct IndexRetrieveOptions
    {
        /// <summary>
        /// Gets or sets the maximum results.
        /// </summary>
        public uint MaxResults { get; internal set; } = 10000u;

        /// <summary>
        /// Gets or sets the methods for data retrieval.
        /// </summary>
        public IndexRetrieveMethod Method { get; internal set; }

        /// <summary>
        /// Gets or sets the distance for proximity searches.
        /// </summary>
        public uint Distance { get; internal set; } = 0;

        /// <summary>
        /// Gets or sets the minimum similarity in percent (1-99) for fuzzy
        /// searches. A value of 0 (default) or 100 means exact matching.
        /// </summary>
        public uint Similarity { get; internal set; } = 0;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IndexRetrieveOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="maxResults">The maximum results.</param>
        public IndexRetrieveOptions(uint maxResults)
        {
            MaxResults = maxResults;
        }
    }
}
