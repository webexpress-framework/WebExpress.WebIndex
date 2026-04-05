using System.Globalization;
using System.Reflection;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Wql;

namespace WebExpress.WebIndex.Test.Fixture
{
    public class UnitTestIndexFixtureWqlB : UnitTestIndexFixture
    {
        /// <summary>
        /// Returns the index manager.
        /// </summary>
        public WebIndex.IndexManager IndexManager { get; } = new IndexManagerTest();

        /// <summary>
        /// Returns the test data.
        /// </summary>
        public IEnumerable<UnitTestIndexTestDocumentB> TestData { get; } = UnitTestIndexTestDocumentFactoryB.GenerateTestData();

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public UnitTestIndexFixtureWqlB()
        {
            var context = new IndexContext();
            context.IndexDirectory = Path.Combine(context.IndexDirectory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManagerTest).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(IndexManager, [context]);

            IndexManager.Create<UnitTestIndexTestDocumentB>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
            IndexManager.ReIndex(TestData);
        }

        /// <summary>
        /// Disposes of the resources used by the current instance.
        /// </summary>
        public override void Dispose()
        {
            IndexManager.Dispose();
            Directory.Delete(IndexManager.Context.IndexDirectory, true);
        }

        /// <summary>
        /// Executes a wql statement.
        /// </summary>
        /// <param name="wql">The wql statement.</param>
        /// <returns>The WQL parser.</returns>
        public IWqlStatement<UnitTestIndexTestDocumentB> ExecuteWql(string wql)
        {
            return new WqlParser<UnitTestIndexTestDocumentB>().Parse(wql);
        }
    }
}
