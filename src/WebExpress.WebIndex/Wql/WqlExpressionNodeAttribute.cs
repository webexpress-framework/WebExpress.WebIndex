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
        /// parameter expression. Supports dot-separated nested paths (e.g., <c>Address.City</c>).
        /// </summary> 
        /// <param name="param">
        /// The parameter expression that represents the index item in the generated 
        /// expression tree (e.g., <c>x</c> in <c>x => x.Property</c>). 
        /// </param> 
        /// <returns> 
        /// A expression that accesses the property defined by property, potentially 
        /// chained through nested objects.
        /// </returns> 
        /// <exception cref="InvalidOperationException"> 
        /// Thrown when <see cref="Name"/> is <c>null</c> or no matching public instance 
        /// property is found on <typeparamref name="TIndexItem"/>. 
        /// </exception>
        public Expression ToExpression(ParameterExpression param)
        {
            if (Name is null)
            {
                throw new InvalidOperationException($"The attribute name cannot be null for type '{typeof(TIndexItem).Name}'.");
            }

            var segments = Name.Split('.');
            Expression current = param;
            var currentType = typeof(TIndexItem);

            foreach (var segment in segments)
            {
                var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    ?? throw new InvalidOperationException($"No public instance property matching '{segment}' was found on type '{currentType.Name}' (full path: '{Name}').");

                current = Expression.Property(current, property);
                currentType = property.PropertyType;
            }

            return current;
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