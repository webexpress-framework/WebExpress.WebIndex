using System;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Caches a compiled constructor delegate per segment type; reflection
    /// based activation is too slow for the segment read hot path.
    /// </summary>
    /// <typeparam name="TIndexStorageSegment">The segment type.</typeparam>
    internal static class IndexSegmentFactory<TIndexStorageSegment>
        where TIndexStorageSegment : IIndexStorageSegment
    {
        /// <summary>
        /// The compiled factory delegate (context, addr) => new TIndexStorageSegment(context, addr).
        /// </summary>
        public static readonly Func<IndexStorageContext, ulong, TIndexStorageSegment> Create = Build();

        /// <summary>
        /// Builds the factory delegate, falling back to reflection for
        /// segment types without the conventional constructor.
        /// </summary>
        /// <returns>The factory delegate.</returns>
        private static Func<IndexStorageContext, ulong, TIndexStorageSegment> Build()
        {
            var ctor = typeof(TIndexStorageSegment)
                .GetConstructor([typeof(IndexStorageContext), typeof(ulong)]);

            if (ctor is null)
            {
                return (context, addr) => (TIndexStorageSegment)Activator.CreateInstance(typeof(TIndexStorageSegment), context, addr);
            }

            var contextParam = Expression.Parameter(typeof(IndexStorageContext), "context");
            var addrParam = Expression.Parameter(typeof(ulong), "addr");

            return Expression.Lambda<Func<IndexStorageContext, ulong, TIndexStorageSegment>>
            (
                Expression.New(ctor, contextParam, addrParam),
                contextParam,
                addrParam
            ).Compile();
        }
    }
}
