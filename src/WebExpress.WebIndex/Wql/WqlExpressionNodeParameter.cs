using System;
using System.Globalization;
using System.Linq.Expressions;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the parameter expression of a WQL statement. A parameter is a literal value,
    /// a function or another attribute of the same item.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeParameter<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the value expressions.
        /// </summary>
        public WqlExpressionNodeValue<TIndexItem> Value { get; internal set; }

        /// <summary>
        /// Returns the function expressions.
        /// </summary>
        public WqlExpressionNodeFilterFunction<TIndexItem> Function { get; internal set; }

        /// <summary>
        /// Returns the attribute expression, if the parameter refers to another attribute.
        /// </summary>
        public WqlExpressionNodeAttribute<TIndexItem> Attribute { get; internal set; }

        /// <summary>
        /// Returns true if the parameter has the same value for every item. Only such a
        /// parameter can be evaluated once and handed to a reverse index; one that depends on
        /// the item is evaluated per item by a scan.
        /// </summary>
        public bool IsConstant => Attribute is null && (Function?.IsConstant ?? true);

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
        /// <exception cref="InvalidOperationException">The parameter refers to an attribute, which has no value outside of an item.</exception>
        public object GetValue()
        {
            if (Attribute is not null)
            {
                throw new InvalidOperationException($"The attribute '{Attribute}' has no value outside of an item.");
            }

            return Function is not null ? Function.Execute() : Value?.GetValue();
        }

        /// <summary>
        /// Returns the value with a literal number in its exact form.
        /// </summary>
        /// <returns>The value.</returns>
        internal object GetExactValue()
        {
            return Value is not null && Function is null && Attribute is null
                ? Value.GetExactValue()
                : GetValue();
        }

        /// <summary>
        /// Converts the parameter expression into a LINQ expression. If the parameter
        /// represents a function, the function is translated into method call expression. Otherwise,
        /// the underlying value is wrapped in a constant expression.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree, through which an attribute or a function reads the item.
        /// </param>
        /// <returns>
        /// A constant expression if the parameter is a literal value, or a method call
        /// expression if the parameter represents a function invocation.
        /// </returns>
        public Expression ToExpression(ParameterExpression param)
        {
            if (Attribute is not null)
            {
                return Attribute.ToExpression(param);
            }

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
        /// Converts the parameter into an operand that is compared with a value of the given
        /// type. A literal is converted when the expression is built, so that it carries the
        /// type of the attribute; attributes and functions keep their own type and are aligned
        /// by the <see cref="WqlExpressionBuilder"/>.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <param name="targetType">The type of the other operand.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <returns>The operand.</returns>
        internal Expression ToOperand(ParameterExpression param, Type targetType, CultureInfo culture)
        {
            if (Value is not null && Function is null && Attribute is null && targetType != typeof(object))
            {
                return WqlExpressionBuilder.Literal(Value.GetExactValue(), targetType, culture);
            }

            return ToExpression(param);
        }

        /// <summary>
        /// Converts the parameter expression to a string.
        /// </summary>
        /// <returns>The parameter expression as a string.</returns>
        public override string ToString()
        {
            if (Attribute is not null)
            {
                return Attribute.ToString();
            }

            return Value is not null ? Value.ToString() : Function.ToString().Trim();
        }
    }
}
