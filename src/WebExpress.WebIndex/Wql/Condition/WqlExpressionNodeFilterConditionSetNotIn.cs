using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

            if (attribute is null)
            {
                return [];
            }

            // get reverse index for the attribute
            var reverseIndex = indexDocument.GetReverseIndex(attribute);

            if (reverseIndex == null || Parameters == null || !Parameters.Any())
            {
                return [];
            }

            // extract all unique parameter values as string
            var excludeValues = Parameters
                .Select(p => p.GetValue()?.ToString())
                .Where(v => v != null)
                .Distinct()
                .ToList();

            // get all ids for every value to be excluded
            var excludedGuids = new HashSet<Guid>();
            foreach (var val in excludeValues)
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
                        excludedGuids.Add(id);
                    }
                }
            }

            // finally select all guids not in the excluded set
            return indexDocument.All
                .Select(x => x.Id)
                .Except(excludedGuids);
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

            // build a strongly-typed array constant matching left.Type so that
            // Enumerable.Contains<T> receives an IEnumerable<T> argument
            var typedArray = Array.CreateInstance(left.Type, rawValues.Count);
            for (int i = 0; i < rawValues.Count; i++)
            {
                typedArray.SetValue(rawValues[i] is null ? null : Convert.ChangeType(rawValues[i], left.Type), i);
            }

            // create a constant expression for the typed array
            var listConstant = Expression.Constant(typedArray, typedArray.GetType());

            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(left.Type);

            var containsCall = Expression.Call(containsMethod, listConstant, left);

            return Expression.Not(containsCall);
        }
    }
}