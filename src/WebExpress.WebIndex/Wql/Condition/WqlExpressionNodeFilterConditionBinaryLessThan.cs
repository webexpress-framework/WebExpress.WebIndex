using System;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary less-than condition in a WQL expression.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLessThan<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryLessThan()
            : base("<")
        {

        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IQueryable<Guid> Apply()
        {
            var property = Attribute?.Property;
            var value = Parameter.GetValue();

            //var filtered = unfiltered.Where
            //(
            //    x => property != null && property.GetValue(x).Equals(value)
            //);

            return null; //filtered.AsQueryable();
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
            ArgumentNullException.ThrowIfNull(Parameter);

            var value = Parameter.GetValue()?.ToString();
            var propertyName = Attribute.Property.Name;

            // build the expression: item => item.Property < value
            var param = Expression.Parameter(typeof(TIndexItem), "item");
            var property = Expression.Property(param, propertyName);
            var valueExpression = Expression.Constant(value);

            // ensure type conversion if necessary
            var convertedProperty = Expression.Convert(property, valueExpression.Type);
            var lessThan = Expression.LessThan(convertedProperty, valueExpression);

            var lambda = Expression.Lambda<Func<TIndexItem, bool>>(lessThan, param);

            // apply the condition to the query using WhereEquals
            return query.WhereEquals(lambda);
        }
    }
}
