using System;
using System.Linq.Expressions;
using System.Reflection;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the attribute expression node of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeAttribute<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the name of the attribute.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeAttribute()
        {
        }

        /// <summary> 
        /// Creates a expression representing access to the attribute's underlying property on the given 
        /// parameter expression. 
        /// </summary> 
        /// <param name="param">
        /// The parameter expression that represents the index item in the generated 
        /// expression tree (e.g., <c>x</c> in <c>x => x.Property</c>). 
        /// </param> 
        /// <returns> 
        /// A expression that accesses the property defined by property. 
        /// </returns> 
        /// <exception cref="InvalidOperationException"> 
        /// Thrown when no <see cref="PropertyInfo"/> is assigned to this attribute node. 
        /// </exception>
        public Expression ToExpression(ParameterExpression param)
        {
            var property = typeof(TIndexItem).GetProperty(Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new InvalidOperationException($"No public instance property matching '{Name}' was found on type '{typeof(TIndexItem).Name}'.");

            return Expression.Property(param, property);
        }

        /// <summary>
        /// Converts the attribute expression to a string.
        /// </summary>
        /// <returns>The attribute expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0}", Name).Trim();
        }
    }
}