using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Predicate
{
    /// <summary>
    /// A predicate whose condition is a set of ids, such as the items below a node. Against the
    /// index the set is the answer itself, so the items are not read and tested one by one; in a
    /// query the set becomes the Contains of the ids, which a database provider translates.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the items the statement filters.</typeparam>
    public interface IWqlIdPredicate<TIndexItem> : IWqlPredicate<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Binds the arguments of a call to the ids of the items the condition holds for. Ids
        /// that no item of the index carries are allowed and match nothing.
        /// </summary>
        /// <param name="arguments">The values of the call's arguments, as <see cref="IWqlPredicate{TIndexItem}.Bind"/> receives them.</param>
        /// <returns>The ids.</returns>
        /// <exception cref="ArgumentException">The arguments do not name anything the predicate knows.</exception>
        IEnumerable<Guid> BindIds(IReadOnlyList<object> arguments);

        /// <summary>
        /// Binds the arguments to the Contains of the ids.
        /// </summary>
        /// <param name="arguments">The values of the call's arguments.</param>
        /// <returns>The condition.</returns>
        Expression<Func<TIndexItem, bool>> IWqlPredicate<TIndexItem>.Bind(IReadOnlyList<object> arguments)
        {
            return Contains(BindIds(arguments).ToHashSet());
        }

        /// <summary>
        /// Returns the condition that an item is one of the ids. The set is typed as an
        /// enumeration, so the call is Enumerable.Contains, which a query provider translates
        /// and which a hash set still answers by lookup.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <returns>The condition.</returns>
        internal static Expression<Func<TIndexItem, bool>> Contains(IReadOnlySet<Guid> ids)
        {
            IEnumerable<Guid> set = ids;

            return x => set.Contains(x.Id);
        }
    }
}
