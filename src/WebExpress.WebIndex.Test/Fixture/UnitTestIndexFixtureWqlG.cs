using System.Globalization;
using System.Reflection;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Wql;

namespace WebExpress.WebIndex.Test.Fixture
{
    /// <summary>
    /// Provides a fixture on the memory index with the typed documents of type G, so that
    /// every WQL statement can be run through the reverse index and through a query and
    /// both results compared.
    /// </summary>
    public class UnitTestIndexFixtureWqlG : UnitTestIndexFixture
    {
        /// <summary>
        /// Returns the index manager.
        /// </summary>
        public WebIndex.IndexManager IndexManager { get; } = new IndexManagerTest();

        /// <summary>
        /// Returns the test data.
        /// </summary>
        public IReadOnlyList<UnitTestIndexTestDocumentG> TestData { get; } = UnitTestIndexTestDocumentFactoryG.GenerateTestData();

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public UnitTestIndexFixtureWqlG()
        {
            var context = new IndexContext();
            context.IndexDirectory = Path.Combine(context.IndexDirectory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManagerTest).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(IndexManager, [context]);

            IndexManager.Create<UnitTestIndexTestDocumentG>(CultureInfo.GetCultureInfo("en"), IndexType.Memory);
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
        /// Parses a wql statement.
        /// </summary>
        /// <param name="wql">The wql statement.</param>
        /// <returns>The parsed statement.</returns>
        public IWqlStatement<UnitTestIndexTestDocumentG> ExecuteWql(string wql)
        {
            return new WqlParser<UnitTestIndexTestDocumentG>().Parse(wql);
        }

        /// <summary>
        /// Runs a statement through the reverse index.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <returns>The identifiers in result order.</returns>
        public List<Guid> RetrieveIndexed(IWqlStatement<UnitTestIndexTestDocumentG> statement)
        {
            return [.. statement.Apply(IndexManager.GetIndexDocument<UnitTestIndexTestDocumentG>()).Select(x => x.Id)];
        }

        /// <summary>
        /// Runs a statement as a query over the test data in memory.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <returns>The identifiers in result order.</returns>
        public List<Guid> RetrieveQueried(IWqlStatement<UnitTestIndexTestDocumentG> statement)
        {
            return [.. statement.ToQuery().Apply(TestData.AsQueryable()).Select(x => x.Id)];
        }

        /// <summary>
        /// Asserts that both paths return the hand-written documents with the given numbers.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="numbers">The numbers of the expected documents.</param>
        public void AssertPaths(string wql, int[] numbers)
        {
            AssertPaths(wql, [.. numbers.Select(UnitTestIndexTestDocumentFactoryG.Id).Order()]);
        }

        /// <summary>
        /// Asserts that both paths return the documents the predicate selects.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="predicate">The expectation written in C#.</param>
        public void AssertPaths(string wql, Func<UnitTestIndexTestDocumentG, bool> predicate)
        {
            AssertPaths(wql, [.. TestData.Where(predicate).Select(x => x.Id).Order()]);
        }

        /// <summary>
        /// Asserts that both paths find nothing.
        /// </summary>
        /// <param name="wql">The statement.</param>
        public void AssertNone(string wql)
        {
            Compare(wql, [], false);
        }

        /// <summary>
        /// Asserts that both paths return the expected documents in the expected order.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="expected">The expected documents in result order.</param>
        public void AssertOrdered(string wql, IEnumerable<UnitTestIndexTestDocumentG> expected)
        {
            Compare(wql, [.. expected.Select(x => x.Id)], true);
        }

        /// <summary>
        /// Asserts that both paths return the expected identifiers, and that the expectation
        /// is neither empty nor everything, which would let a broken filter pass.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="expected">The expected identifiers in ascending order.</param>
        private void AssertPaths(string wql, List<Guid> expected)
        {
            Assert.True(expected.Count > 0 && expected.Count < TestData.Count, $"the expectation of '{wql}' is trivial");

            Compare(wql, expected, false);
        }

        /// <summary>
        /// Runs a statement through both paths and compares each result with the expectation.
        /// </summary>
        /// <param name="wql">The statement.</param>
        /// <param name="expected">The expected identifiers.</param>
        /// <param name="ordered">True to compare in result order, false to compare as sets.</param>
        private void Compare(string wql, List<Guid> expected, bool ordered)
        {
            var statement = ExecuteWql(wql);

            Assert.False(statement.HasErrors, $"'{wql}' reports {statement.Error}");

            var indexed = RetrieveIndexed(statement);
            var queried = RetrieveQueried(statement);

            if (!ordered)
            {
                indexed.Sort();
                queried.Sort();
            }

            Assert.True(expected.SequenceEqual(indexed), $"the index returns {Describe(indexed, expected)} for '{wql}'");
            Assert.True(expected.SequenceEqual(queried), $"the query returns {Describe(queried, expected)} for '{wql}'");
        }

        /// <summary>
        /// Describes how a result differs from the expectation.
        /// </summary>
        /// <param name="actual">The result.</param>
        /// <param name="expected">The expectation.</param>
        /// <returns>A short description.</returns>
        private static string Describe(List<Guid> actual, List<Guid> expected)
        {
            var missing = expected.Except(actual).Take(5);
            var surplus = actual.Except(expected).Take(5);

            return $"{actual.Count} instead of {expected.Count} items (missing {string.Join(", ", missing)}; surplus {string.Join(", ", surplus)})";
        }
    }
}
