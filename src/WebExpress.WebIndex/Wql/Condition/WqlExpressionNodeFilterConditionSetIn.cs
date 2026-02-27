using System;
using System.Collections.Generic;
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
        /// Applies the filter condition to the index using the specified attribute 
        /// and returns the matching data identifiers. 
        /// </summary> 
        /// <param name="indexDocument">The index document.</param>
        /// <returns> 
        /// A sequence of data identifiers that satisfy the filter condition. 
        /// </returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            // get the relevant attribute by name
            var attribute = indexDocument.Fields
                .FirstOrDefault(x => x.Name.Equals(Attribute.Name, StringComparison.OrdinalIgnoreCase));

            if (attribute == null || Parameters == null || !Parameters.Any())
            {
                return [];
            }

            // get reverse index for the attribute
            var reverseIndex = indexDocument.GetReverseIndex(attribute);

            if (reverseIndex == null)
            {
                return [];
            }

            // extract all unique parameter values as string
            var includeValues = Parameters
                .Select(p => p.GetValue()?.ToString())
                .Where(v => v != null)
                .Distinct()
                .ToList();

            // collect matching guids for each value
            var resultGuids = new HashSet<Guid>();
            foreach (var val in includeValues)
            {
                var ids = reverseIndex.Retrieve(val, new IndexRetrieveOptions
                {
                    Method = IndexRetrieveMethod.Phrase,
                    Distance = 0
                });

                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        resultGuids.Add(id);
                    }
                }
            }

            return resultGuids;
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