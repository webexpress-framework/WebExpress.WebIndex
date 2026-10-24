namespace WebExpress.WebIndex.Test.Fixture
{
    /// <summary>
    /// Provides a base class for unit test fixtures that require disposable resources.
    /// </summary>
    public class UnitTestIndexFixture : IDisposable
    {
        /// <summary>
        /// The random number generator. Uses the thread-safe shared instance because fixtures of
        /// parallel (non-serialized) test collections are constructed concurrently, and a single
        /// shared <see cref="Random"/> instance is not safe for concurrent access.
        /// </summary>
        protected static Random Rand => Random.Shared;

        /// <summary>
        /// Disposes of the resources used by the current instance.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
