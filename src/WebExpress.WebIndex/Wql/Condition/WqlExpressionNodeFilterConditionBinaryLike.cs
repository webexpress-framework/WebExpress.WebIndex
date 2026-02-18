using System;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary 'LIKE' condition in a WQL expression.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLike<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryLike()
            : base("~")
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IQueryable<Guid> Apply()
        {
            var value = Parameter.GetValue();

            return Attribute.ReverseIndex?.Retrieve(value?.ToString(), new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.Default,
                Distance = Options.Distance ?? 0
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

            // build the expression: item => item.Property.Contains(value)
            var param = Expression.Parameter(typeof(TIndexItem), "item");
            var property = Expression.Property(param, propertyName);

            // use the string contains method for "LIKE" functionality
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);
            var containsCall = Expression.Call(property, containsMethod, Expression.Constant(value));

            // create the lambda expression
            var lambda = Expression.Lambda<Func<TIndexItem, bool>>(containsCall, param);

            // apply the condition to the query
            return query.WhereEquals(lambda);
        }
    }
}
