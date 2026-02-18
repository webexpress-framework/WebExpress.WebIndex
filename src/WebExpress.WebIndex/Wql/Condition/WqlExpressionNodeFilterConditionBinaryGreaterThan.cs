using System;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition that checks if a property value is greater than a specified value.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryGreaterThan<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryGreaterThan()
            : base(">")
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

            return Attribute.ReverseIndex?.Retrieve(value, new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.GratherThan
            }).AsQueryable();
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

            // build the expression: item => item.Property > value
            var param = Expression.Parameter(typeof(TIndexItem), "item");
            var property = Expression.Property(param, propertyName);
            var valueExpression = Expression.Constant(value);

            // ensure proper type conversion if necessary
            var convertedProperty = Expression.Convert(property, valueExpression.Type);
            var greaterThan = Expression.GreaterThan(convertedProperty, valueExpression);

            // create the lambda expression
            var lambda = Expression.Lambda<Func<TIndexItem, bool>>(greaterThan, param);

            // apply the condition to the query
            return query.WhereEquals(lambda);
        }
    }
}
