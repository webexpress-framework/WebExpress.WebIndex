using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition for sets in a WQL expression node.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionSetIn<TIndexItem> : WqlExpressionNodeFilterConditionSet<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionSetIn()
            : base("in")
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IQueryable<Guid> Apply()
        {
            var property = Attribute?.Property;
            //var value = Parameter.GetValue();

            //var filtered = unfiltered.Where
            //(
            //    x => property != null && property.GetValue(x).Equals(value)
            //);

            return null; //filtered.AsQueryable();
        }

        /// <summary>
        /// Builds a LINQ expression representing an "IN" set-membership comparison between 
        /// the attribute expression and the list of parameter values.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => values.Contains(x.Property)</c>).
        /// </param>
        /// <returns>
        /// A method call expression to determine whether the attribute value is 
        /// contained in the provided set.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <c>Attribute</c> or <c>Parameters</c> is <c>null</c>.
        /// </exception>
        public override Expression ToExpression(ParameterExpression param)
        {
            ArgumentNullException.ThrowIfNull(Attribute);
            ArgumentNullException.ThrowIfNull(Parameters);

            Expression left = Attribute.ToExpression(param);

            // extract raw values from parameters
            var rawValues = Parameters.Select(p => p.GetValue()).ToList();

            // convert all values to the property type
            var typedValues = rawValues
                .Select(v => v is null ? null : Convert.ChangeType(v, left.Type))
                .ToList();

            // create a constant expression for the typed list
            var listConstant = Expression.Constant(typedValues);

            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(left.Type);

            return Expression.Call(containsMethod, listConstant, left);
        }
    }
}