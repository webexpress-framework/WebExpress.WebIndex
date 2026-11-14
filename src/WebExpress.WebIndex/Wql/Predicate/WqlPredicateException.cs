using System;

namespace WebExpress.WebIndex.Wql.Predicate
{
    /// <summary>
    /// Thrown when a predicate fails while it binds a call for a reason other than its arguments,
    /// such as a lookup in a database that timed out. Such a failure is no fault of the statement,
    /// so it is not reported as a parse error that would read as an empty result; it reaches the
    /// caller with the original exception as <see cref="Exception.InnerException"/>.
    /// </summary>
    public class WqlPredicateException : Exception
    {
        /// <summary>
        /// Gets the name of the predicate that failed.
        /// </summary>
        public string Predicate { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="predicate">The name of the predicate that failed.</param>
        /// <param name="innerException">The failure of the predicate.</param>
        public WqlPredicateException(string predicate, Exception innerException)
            : base($"The WQL predicate '{predicate}' failed to bind its arguments.", innerException)
        {
            Predicate = predicate;
        }
    }
}
