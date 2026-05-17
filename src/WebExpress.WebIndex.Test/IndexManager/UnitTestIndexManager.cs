using System.Reflection;
using Xunit;
namespace WebExpress.WebIndex.Test.IndexManager
{
    /// <summary>
    /// Test class for testing the memory-based document store.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    public abstract class UnitTestIndexManager<T>(T fixture, ITestOutputHelper output) : IClassFixture<T> where T : class
    {
        /// <summary>
        /// Returns the index manager.
        /// </summary>
        public WebIndex.IndexManager IndexManager { get; private set; }

        /// <summary>
        /// Returns the log.
        /// </summary>
        protected ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected T Fixture { get; private set; } = fixture;

        /// <summary>
        /// Returns the context.
        /// </summary>
        protected IIndexContext Context { get; private set; }

        /// <summary>
        /// It sets up the preconditions for a unit test.
        /// </summary>
        protected void Preconditions()
        {
            var context = new IndexContext();
            context.IndexDirectory = Path.Combine(context.IndexDirectory, Guid.NewGuid().ToString());
            IndexManager = new IndexManagerTest();

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManagerTest).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(IndexManager, [context]);

            Context = context;
        }

        /// <summary>
        /// It performs cleanup tasks after a unit test.
        /// </summary>
        protected void Postconditions()
        {
            IndexManager.Dispose();

            if (Directory.Exists(Context.IndexDirectory))
            {
                Directory.Delete(Context.IndexDirectory, true);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
