using System;

namespace WebExpress.WebIndex.Queries
{
    /// <summary>
    /// Provides a default implementation of a query context that manages 
    /// resources for query operations.
    /// </summary>
    public class DefaultQueryContext : IQueryContext
    {
        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
