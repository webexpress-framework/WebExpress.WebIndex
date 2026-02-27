namespace WebExpress.WebIndex
{
    /// <summary>
    /// Defines the methods for data retrieval.
    /// </summary>
    public enum IndexRetrieveMethod
    {
        /// <summary>
        /// Query based on an word search (~).
        /// </summary>
        Default,

        /// <summary>
        /// Query based on an exact phrase (=).
        /// </summary>
        Phrase,

        /// <summary>
        /// Query based on the proximity of search terms.
        /// </summary>
        Proximity,

        /// <summary>
        /// Query based on a greater than comparison (>).
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Query based on a greater than or equal to comparison (>=).
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Query based on a less than comparison (<).
        /// </summary>
        LessThan,

        /// <summary>
        /// Query based on a less than or equal to comparison (<=).
        /// </summary>
        LessThanOrEqual
    }
}
