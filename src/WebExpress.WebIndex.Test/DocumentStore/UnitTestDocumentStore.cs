using Xunit;
namespace WebExpress.WebIndex.Test.DocumentStore
{
    /// <summary>
    /// Test class for testing the document store.
    /// </summary>
    /// <param name="fixture">The log.</param>
    /// <param name="output">The test context.</param>
    public abstract class UnitTestDocumentStore<T>(T fixture, ITestOutputHelper output) : IClassFixture<T> where T : class
    {
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
        protected IIndexDocumemntContext Context { get; private set; }

        /// <summary>
        /// It sets up the preconditions for a unit test.
        /// </summary>
        protected void Preconditions()
        {
            var context = new IndexContext();

            context.IndexDirectory = Path.Combine(context.IndexDirectory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Context = new IndexDocumemntContext(context, new Term.IndexTokenAnalyzer(context));
        }

        /// <summary>
        /// It performs cleanup tasks after a unit test.
        /// </summary>
        protected void Postconditions()
        {
            Context.TokenAnalyzer.Dispose();
            Directory.Delete(Context.IndexDirectory, true);
        }
    }
}
