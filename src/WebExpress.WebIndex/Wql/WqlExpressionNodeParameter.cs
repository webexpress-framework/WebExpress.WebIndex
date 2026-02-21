using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the parameter expression of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeParameter<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the value expressions.
        /// </summary>
        public WqlExpressionNodeValue<TIndexItem> Value { get; internal set; }

        /// <summary>
        /// Returns the function expressions.
        /// </summary>
        public WqlExpressionNodeFilterFunction<TIndexItem> Function { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeParameter()
        {
        }

        /// <summary>
        /// Returns the value.
        /// </summary>
        /// <returns>The value.</returns>
        public object GetValue()
        {
            return Function is not null ? Function.Execute() : Value.GetValue();
        }

        /// <summary> 
        /// Converts the parameter expression into a LINQ expression. If the parameter 
        /// represents a function, the function is translated into method call expression. Otherwise, 
        /// the underlying value is wrapped in a constant expression. 
        /// </summary> 
        /// <param name="param"> 
        /// The parameter expression representing the index item in the generated 
        /// expression tree. This parameter is not used directly here, but is 
        /// required to satisfy the <see cref="IWqlExpressionNode{TIndexItem}"/> 
        /// interface and for consistency with other node types. 
        /// </param> 
        /// <returns> 
        /// A constant expression if the parameter is a literal value, or a method call 
        /// expression if the parameter represents a function invocation. 
        /// </returns> 
        public Expression ToExpression(ParameterExpression param)
        {
            if (Function is not null)
            {
                // delegate expression building to the function node 
                return Function.ToExpression(param);
            }

            if (Value is not null)
            {
                // wrap literal values in a constant expression 
                return Expression.Constant(Value.GetValue());
            }

            throw new InvalidOperationException("Parameter node contains neither a value nor a function.");
        }

        /// <summary>
        /// Converts the parameter expression to a string.
        /// </summary>
        /// <returns>The parameter expression as a string.</returns>
        public override string ToString()
        {
            return Value is not null ? Value.ToString() : Function.ToString().Trim();
        }
    }
}