using System.Text;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Test.Fixture
{
    /// <summary>
    /// A unit test fixture for tokens.
    /// </summary
    public class UnitTestIndexFixtureToken : UnitTestIndexFixture
    {
        /// <summary>
        /// Returns the context.
        /// </summary>
        public IndexContext Context { get; private set; }

        /// <summary>
        /// Returns the token analyzer.
        /// </summary>
        public IndexTokenAnalyzer TokenAnalyzer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public UnitTestIndexFixtureToken()
        {
            var context = new IndexContext();
            context.IndexDirectory = Path.Combine(context.IndexDirectory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Context = context;

            TokenAnalyzer = new IndexTokenAnalyzer(Context);
        }

        /// <summary>
        /// Disposes of the resources used by the current instance.
        /// </summary>
        public override void Dispose()
        {
            TokenAnalyzer.Dispose();
            Directory.Delete(Context.IndexDirectory, true);
        }

        /// <summary>
        /// Gets the resource with the specified name.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <returns>The resource as a string, or an empty string if the resource is not found.</returns>
        public string GetRessource(string name)
        {
            var assembly = typeof(UnitTestIndexFixtureToken).Assembly;
            var resources = assembly.GetManifestResourceNames();

            var resource = resources
                .Where(x => x.Contains(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (resource is null)
            {
                return "";
            }

            using var stream = assembly.GetManifestResourceStream(resource);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
