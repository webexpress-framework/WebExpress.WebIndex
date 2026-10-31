namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Specifies for which query provider a WQL statement builds its expression tree, since
    /// the text contract can only be reproduced exactly by code running in memory.
    /// </summary>
    public enum WqlQueryMode
    {
        /// <summary>
        /// The conditions are evaluated as in the index, with the <see cref="WqlTextMatcher"/>
        /// and ordinal comparisons that ignore case. Suitable for providers that run the
        /// expression in memory, such as LINQ to Objects.
        /// </summary>
        Exact,

        /// <summary>
        /// The conditions are rewritten into null tests, ToLower, Contains, StartsWith,
        /// EndsWith and Compare, which providers such as Entity Framework translate into SQL.
        /// A text search turns into a substring test per word and so selects a superset of
        /// the exact result - and a negated one a subset. Conditions on functions keep their
        /// evaluation in memory and cannot be translated.
        /// </summary>
        Translatable
    }
}
