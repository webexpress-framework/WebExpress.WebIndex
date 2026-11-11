using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// A condition that a predicate function stands for, such as within("SD-12"). The condition
    /// was bound when the statement was parsed; it has no attribute and no operator.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the items the statement filters.</typeparam>
    public class WqlExpressionNodeFilterConditionPredicate<TIndexItem> : WqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly Expression<Func<TIndexItem, bool>> _condition;

        /// <summary>
        /// Gets the name the predicate was called by.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the argument values of the call.
        /// </summary>
        public IReadOnlyList<object> Arguments { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The name the predicate was called by.</param>
        /// <param name="arguments">The argument values of the call.</param>
        /// <param name="condition">The bound condition.</param>
        internal WqlExpressionNodeFilterConditionPredicate(string name, IReadOnlyList<object> arguments, Expression<Func<TIndexItem, bool>> condition)
            : base(name, true)
        {
            Name = name;
            Arguments = arguments;
            _condition = condition;
        }

        /// <summary>
        /// Evaluates the condition against every item of the index; a predicate brings no reverse
        /// index of its own.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The ids of the matching items.</returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            return Scan(indexDocument);
        }

        /// <summary>
        /// Returns the bound condition, applied to the item parameter of the statement.
        /// </summary>
        /// <param name="param">The item parameter.</param>
        /// <returns>The condition expression.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return new Rebind(_condition.Parameters[0], param).Visit(_condition.Body);
        }

        /// <summary>
        /// Returns the call as written.
        /// </summary>
        /// <returns>The call.</returns>
        public override string ToString()
        {
            var arguments = Arguments.Select(x => x switch
            {
                null => "null",
                string text => $"\"{text.Replace("\"", "\\\"")}\"",
                IFormattable number => number.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => x.ToString()
            });

            return $"{Name}({string.Join(", ", arguments)})";
        }

        /// <summary>
        /// Puts the statement's item parameter in place of the predicate's own.
        /// </summary>
        /// <param name="from">The parameter of the predicate's expression.</param>
        /// <param name="to">The item parameter of the statement.</param>
        private sealed class Rebind(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            /// <summary>
            /// Replaces the parameter.
            /// </summary>
            /// <param name="node">The parameter.</param>
            /// <returns>The replacement.</returns>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == from ? to : base.VisitParameter(node);
            }
        }
    }
}
