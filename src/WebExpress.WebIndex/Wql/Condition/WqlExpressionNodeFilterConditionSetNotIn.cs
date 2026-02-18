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
        /// Applies the current filter condition to the specified query and returns the 
        /// resulting query.
        /// </summary>
        /// <param name="query">
        /// The query to which the filter condition will be applied. This parameter must 
        /// not be null.
        /// </param>
        /// <returns>
        /// An <see cref="IQuery{TIndexItem}"/> representing the filtered query if a 
        /// condition exists; otherwise, the original query.
        /// </returns>
        public override IQuery<TIndexItem> Apply(IQuery<TIndexItem> query)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(Attribute);
            ArgumentNullException.ThrowIfNull(Parameters);

            var values = Parameters.Select(x => x.GetValue()?.ToString());
            var propertyName = Attribute.Property.Name;

            // build the expression: item => !values.Contains(item.Property)
            var param = Expression.Parameter(typeof(TIndexItem), "item");
            var property = Expression.Property(param, propertyName);
            var valueArray = Expression.Constant(values.ToList());

            // use Enumerable.Contains to check if the property value is not in the set
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(property.Type);

            var containsCall = Expression.Call(containsMethod, valueArray, property);
            var notContains = Expression.Not(containsCall); // Negate the condition (!values.Contains(...))

            // create the lambda expression
            var lambda = Expression.Lambda<Func<TIndexItem, bool>>(notContains, param);

            // apply the condition to the query
            return query.WhereEquals(lambda);
        }
    }
}