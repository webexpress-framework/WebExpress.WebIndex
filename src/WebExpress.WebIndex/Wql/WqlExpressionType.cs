namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Specifies the types of expressions that can be used in a WQL 
    /// (WebExpress Query Language) query.
    /// </summary>
    public enum WqlExpressionType
    {
        /// <summary>
        /// Represents a value that indicates no value or an absence of data.
        /// </summary>
        None,

        /// <summary>
        /// Represents an attribute that can be applied to program entities to provide metadata.
        /// </summary>
        Attribute,

        /// <summary>
        /// Specifies the logical operators that can be used to combine boolean expressions.
        /// </summary>
        LogicalOperator,

        /// <summary>
        /// Defines a custom operator for the type, enabling user-defined behavior for 
        /// operations such as addition, subtraction, or comparison.
        /// </summary>
        Operator,

        /// <summary>
        /// Represents a parameter that can be used in various contexts.
        /// </summary>
        Parameter
    }
}
