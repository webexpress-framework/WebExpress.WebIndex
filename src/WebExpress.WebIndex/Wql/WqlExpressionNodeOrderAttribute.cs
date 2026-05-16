using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the order attribute of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeOrderAttribute<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the attribute expressions.
        /// </summary>
        public WqlExpressionNodeAttribute<TIndexItem> Attribute { get; internal set; }

        /// <summary>
        /// Returns the descending expressions.
        /// </summary>
        public bool Descending { get; internal set; }

        /// <summary>
        /// Returns the position of the attbibute within the order by statement.
        /// </summary>
        public int Position { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeOrderAttribute()
        {
        }

        /// <summary>
        /// Applies the filter to the unfiltered data object.
        /// Supports dot-separated nested property paths.
        /// </summary>
        /// <param name="unfiltered">The unfiltered data.</param>
        /// <returns>The filtered data.</returns>
        public IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> unfiltered)
        {
            ArgumentNullException.ThrowIfNull(unfiltered);
            ArgumentNullException.ThrowIfNull(Attribute);

            var parameter = Expression.Parameter(typeof(TIndexItem), "x");
            var keySelector = Expression.Lambda<Func<TIndexItem, object>>
            (
                Expression.Convert(Attribute.ToExpression(parameter), typeof(object)),
                parameter
            );

            if (Position > 0 && unfiltered is IOrderedQueryable<TIndexItem> orderedQueryable)
            {
                return Descending
                    ? orderedQueryable.ThenByDescending(keySelector)
                    : orderedQueryable.ThenBy(keySelector);
            }

            return Descending
                ? unfiltered.OrderByDescending(keySelector)
                : unfiltered.OrderBy(keySelector);
        }

        /// <summary>
        /// Builds a LINQ expression representing the property used for ordering in 
        /// this order-by attribute.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Property</c>).
        /// </param>
        /// <returns>
        /// An expression representing the property to order by.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <c>Attribute</c> is <c>null</c>.
        /// </exception>
        public Expression ToExpression(ParameterExpression parameter)
        {
            ArgumentNullException.ThrowIfNull(Attribute);

            // build the property access expression: x => x.Property 
            var body = Attribute.ToExpression(parameter);

            // convert to object to satisfy order requirements 
            return Expression.Convert(body, typeof(object));
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", Attribute.ToString(), Descending ? "desc" : "asc").Trim();
        }
    }
}
