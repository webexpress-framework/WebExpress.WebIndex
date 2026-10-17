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
        /// Returns the declared type of the attribute, or null if the name denotes no property
        /// of the index item. The parser keeps unknown attributes in the tree so that the
        /// lookahead analysis can continue behind them, and reports them as a semantic error.
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// Returns the token the attribute was read from, used to locate semantic errors.
        /// </summary>
        internal IWqlToken Token { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeAttribute()
        {
        }

        /// <summary>
        /// Creates a expression representing access to the attribute's underlying property on the given
        /// parameter expression. Supports dot-separated nested paths (e.g., <c>Address.City</c>).
        /// A missing intermediate object yields null instead of an exception, so that neither a
        /// filter nor a sort fails on an item whose nested object is absent; a value type at the
        /// end of such a path is therefore lifted to its nullable form.
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

            for (var i = 0; i < segments.Length; i++)
            {
                var instanceType = Nullable.GetUnderlyingType(current.Type) ?? current.Type;
                var property = instanceType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    ?? throw new InvalidOperationException($"No public instance property matching '{segments[i]}' was found on type '{instanceType.Name}' (full path: '{Name}').");

                var instance = current.Type != instanceType
                    ? Expression.Property(current, nameof(Nullable<int>.Value))
                    : current;

                Expression access = Expression.Property(instance, property);

                // the item itself is never null, only the objects reached through it
                if (i > 0 && CanBeNull(current.Type))
                {
                    var type = WqlExpressionBuilder.MakeNullable(access.Type);

                    access = Expression.Condition
                    (
                        Expression.Equal(current, Expression.Constant(null, current.Type)),
                        Expression.Constant(null, type),
                        access.Type == type ? access : Expression.Convert(access, type)
                    );
                }

                current = access;
            }

            return current;
        }

        /// <summary>
        /// Determines whether a value of the given type can be null.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True for reference and nullable types, otherwise false.</returns>
        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
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