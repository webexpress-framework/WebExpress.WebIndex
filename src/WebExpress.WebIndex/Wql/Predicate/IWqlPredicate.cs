using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Predicate
{
    /// <summary>
    /// A WQL function that stands for a whole condition, such as within("SD-12"). A scalar
    /// function computes a value that a condition compares; set membership - "below this page",
    /// "in this sprint" - is no comparison of a value, so a predicate returns the condition itself.
    /// </summary>
    /// <remarks>
    /// A predicate is registered as an instance, unlike a scalar function, because it may bind
    /// state that was computed ahead: the ids below a node, a key range of a hierarchy. Binding
    /// happens once per parse, with the arguments of the call, so a statement that is parsed
    /// again binds the current state again. A predicate whose condition is a known set of ids
    /// implements <see cref="IWqlIdPredicate{TIndexItem}"/>, so the index answers it without a scan.
    /// </remarks>
    /// <typeparam name="TIndexItem">The type of the items the statement filters.</typeparam>
    public interface IWqlPredicate<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the name the predicate is called by; case does not matter. It has the form of an
        /// attribute name, and no scalar function of the parser may carry it.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Binds the arguments of a call and returns the condition over an item. The expression
        /// runs in memory against the index and, for a translatable query, in a database query
        /// provider - a predicate meant for both keeps to what the provider can translate, such
        /// as the Contains of a list of ids.
        /// </summary>
        /// <param name="arguments">
        /// The values of the call's arguments, which are constants: a string as <see cref="string"/>,
        /// a whole number as <see cref="long"/>, any other number as <see cref="decimal"/>,
        /// true and false as <see cref="bool"/>, null as null and a function such as today() as
        /// its result. A narrower number is read with <see cref="Convert"/>, as in
        /// <c>Convert.ToInt32(arguments[0])</c>, since an unboxing cast to it fails.
        /// </param>
        /// <returns>The condition.</returns>
        /// <exception cref="ArgumentException">
        /// The arguments do not name anything the predicate knows. This, and the exceptions of
        /// reading an argument the call does not have or of the wrong type (an invalid cast, a
        /// format or overflow error, a missing key or index), is reported as an error of the
        /// statement; any other exception is a failure of the predicate and is passed on to the
        /// caller as a <see cref="WqlPredicateException"/>.
        /// </exception>
        Expression<Func<TIndexItem, bool>> Bind(IReadOnlyList<object> arguments);
    }
}
