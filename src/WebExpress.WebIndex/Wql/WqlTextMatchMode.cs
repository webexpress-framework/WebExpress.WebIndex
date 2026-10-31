namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Specifies how the words of a WQL text search are matched against a text.
    /// </summary>
    public enum WqlTextMatchMode
    {
        /// <summary>
        /// The words must occur in the order and spacing of the query, as the <c>=</c>
        /// operator demands.
        /// </summary>
        Phrase,

        /// <summary>
        /// Every word must occur anywhere in the text, as the <c>~</c> operator demands; a
        /// distance additionally requires each word to stand near the previous one.
        /// </summary>
        Words
    }
}
