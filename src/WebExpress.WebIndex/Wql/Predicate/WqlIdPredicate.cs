using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Predicate
{
    /// <summary>
    /// A predicate over a set of ids defined by a delegate, for a registration that needs no
    /// class of its own:
    /// <c>parser.RegisterPredicate(new WqlIdPredicate&lt;Issue&gt;("within", args =&gt; Hierarchy.Below((string)args[0])))</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the items the statement filters.</typeparam>
    public sealed class WqlIdPredicate<TIndexItem> : IWqlIdPredicate<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly Func<IReadOnlyList<object>, IEnumerable<Guid>> _bindIds;

        /// <summary>
        /// Gets the name the predicate is called by.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The name the predicate is called by.</param>
        /// <param name="bindIds">Binds the arguments of a call to the ids the condition holds for.</param>
        public WqlIdPredicate(string name, Func<IReadOnlyList<object>, IEnumerable<Guid>> bindIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(bindIds);

            Name = name;
            _bindIds = bindIds;
        }

        /// <summary>
        /// Binds the arguments of a call to the ids.
        /// </summary>
        /// <param name="arguments">The values of the call's arguments.</param>
        /// <returns>The ids.</returns>
        public IEnumerable<Guid> BindIds(IReadOnlyList<object> arguments)
        {
            return _bindIds(arguments);
        }
    }
}
