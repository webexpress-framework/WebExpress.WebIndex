using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents an abstract base class for a WQL expression node filter condition.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterCondition<TIndexItem> : IWqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the attribute expression.
        /// </summary>
        public WqlExpressionNodeAttribute<TIndexItem> Attribute { get; internal set; }

        /// <summary>
        /// Returns the operator expression.
        /// </summary>
        public string Operator { get; internal set; }

        /// <summary> 
        /// Returns a value indicating whether this operator expects a scalar value 
        /// instead of a list of values. 
        /// </summary> 
        public bool IsScalar { get; internal set; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
        protected WqlExpressionNodeFilterCondition(string token, bool isScalar = true)
        {
            Operator = token;
            IsScalar = isScalar;
        }

        /// <summary> 
        /// Applies the filter condition to the index using the specified attribute 
        /// and returns the matching data identifiers. 
        /// </summary> 
        /// <param name="indexDocument">The index document.</param>
        /// <returns> 
        /// A sequence of data identifiers that satisfy the filter condition. 
        /// </returns>
        public abstract IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument);

        /// <summary>
        /// Builds the LINQ expression that represents this WQL node within the 
        /// generated expression tree.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the resulting
        /// lambda expression (e.g., <c>x</c> in <c>x => ...</c>).
        /// </param>
        /// <returns>
        /// An expression that models the semantic meaning of this WQL expression node.
        /// </returns>
        public abstract Expression ToExpression(ParameterExpression param);
    }
}