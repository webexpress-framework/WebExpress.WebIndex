namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Specifies the comparisons that the <see cref="WqlExpressionBuilder"/> creates for the
    /// conditions of a WQL statement.
    /// </summary>
    public enum WqlComparisonOperator
    {
        /// <summary>
        /// Both values are equal.
        /// </summary>
        Equal,

        /// <summary>
        /// Both values differ.
        /// </summary>
        NotEqual,

        /// <summary>
        /// The left value is smaller than the right one.
        /// </summary>
        LessThan,

        /// <summary>
        /// The left value is smaller than or equal to the right one.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// The left value is greater than the right one.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// The left value is greater than or equal to the right one.
        /// </summary>
        GreaterThanOrEqual
    }
}
