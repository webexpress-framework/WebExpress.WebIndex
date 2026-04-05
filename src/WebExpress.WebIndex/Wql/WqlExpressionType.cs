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
        Parameter,

        /// <summary>
        /// Represents a function that performs a specific operation.
        /// </summary>
        Function,

        /// <summary>
        /// Represents an order in the system, encapsulating details about the items 
        /// purchased and their quantities.
        /// </summary>
        Order,

        /// <summary>
        /// Specifies the direction in which a collection is sorted, such as ascending 
        /// or descending.
        /// </summary>
        OrderDirection,

        /// <summary>
        /// Gets or sets the partitioning strategy used for data distribution.
        /// </summary>
        Partitioning,

        /// <summary>
        /// Represents an operator that partitions a sequence into multiple segments based on 
        /// specified criteria.
        /// </summary>
        PartitioningOperator,

        /// <summary>
        /// Represents the opening parenthesis character used in expressions.
        /// </summary>
        OpenParenthesis,

        /// <summary>
        /// Represents the closing parenthesis character used in expressions.
        /// </summary>
        CloseParenthesis,

        /// <summary>
        /// Represents the quotation mark character used to delimit string literals.
        /// </summary>
        Quotation,

        /// <summary>
        /// Represents the separator character used to delimit values.
        /// </summary>
        Separator
    }
}
