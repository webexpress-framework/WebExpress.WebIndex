using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Predicate
{
    /// <summary>
    /// A predicate defined by a delegate, for a registration that needs no class of its own:
    /// <c>parser.RegisterPredicate(new WqlPredicate&lt;Issue&gt;("within", args =&gt; { var ids = Below(args[0]); return x =&gt; ids.Contains(x.Id); }))</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the items the statement filters.</typeparam>
    public sealed class WqlPredicate<TIndexItem> : IWqlPredicate<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly Func<IReadOnlyList<object>, Expression<Func<TIndexItem, bool>>> _bind;

        /// <summary>
        /// Gets the name the predicate is called by.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The name the predicate is called by.</param>
        /// <param name="bind">Binds the arguments of a call to the condition.</param>
        public WqlPredicate(string name, Func<IReadOnlyList<object>, Expression<Func<TIndexItem, bool>>> bind)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(bind);

            Name = name;
            _bind = bind;
        }

        /// <summary>
        /// Binds the arguments of a call.
        /// </summary>
        /// <param name="arguments">The values of the call's arguments.</param>
        /// <returns>The condition.</returns>
        public Expression<Func<TIndexItem, bool>> Bind(IReadOnlyList<object> arguments)
        {
            return _bind(arguments);
        }
    }
}
