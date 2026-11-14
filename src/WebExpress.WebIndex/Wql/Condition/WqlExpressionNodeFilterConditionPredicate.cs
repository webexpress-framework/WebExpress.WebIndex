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
        private readonly IReadOnlyList<WqlExpressionNodeParameter<TIndexItem>> _parameters;
        private readonly IReadOnlySet<Guid> _ids;

        /// <summary>
        /// Gets the name the predicate was called by.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the argument values the predicate was bound with.
        /// </summary>
        public IReadOnlyList<object> Arguments { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The name the predicate was called by.</param>
        /// <param name="parameters">The arguments as written, so the statement prints the call and not its values.</param>
        /// <param name="arguments">The argument values the predicate was bound with.</param>
        /// <param name="condition">The bound condition.</param>
        /// <param name="ids">The ids the condition holds for, if the predicate names them; the index then answers without a scan.</param>
        internal WqlExpressionNodeFilterConditionPredicate(string name, IReadOnlyList<WqlExpressionNodeParameter<TIndexItem>> parameters, IReadOnlyList<object> arguments, Expression<Func<TIndexItem, bool>> condition, IReadOnlySet<Guid> ids)
            : base(name, true)
        {
            Name = name;
            Arguments = arguments;
            _parameters = parameters;
            _condition = condition;
            _ids = ids;
        }

        /// <summary>
        /// Returns the matching items. A predicate brings no reverse index of its own, so one
        /// that does not name its ids is evaluated against every item.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The ids of the matching items.</returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            return _ids ?? Scan(indexDocument);
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
        /// Returns the call as written, in the form the parser reads back.
        /// </summary>
        /// <returns>The call.</returns>
        public override string ToString()
        {
            return $"{Name}({string.Join(", ", _parameters.Select(x => x.ToString()))})";
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
