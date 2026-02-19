using System;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a WQL expression node filter condition for the "not in" set operation.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionSetNotIn<TIndexItem> : WqlExpressionNodeFilterConditionSet<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionSetNotIn()
            : base("not in")
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IQueryable<Guid> Apply()
        {
            return null;
        }

        /// <summary>
        /// Builds a LINQ expression representing a "NOT IN" set-membership comparison 
        /// between the attribute expression and the list of parameter values.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => !values.Contains(x.Property)</c>).
        /// </param>
        /// <returns>
        /// A unary expression that checks whether the attribute value is not contained 
        /// in the provided set.
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

            var containsCall = Expression.Call(containsMethod, listConstant, left);

            return Expression.Not(containsCall);
        }
    }
}