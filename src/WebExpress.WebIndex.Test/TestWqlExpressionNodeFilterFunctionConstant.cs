using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Test
{
    /// <summary>
    /// Represents a test WQL expression node filter function constant for UnitTestIndexTestDocumentA.
    /// </summary>
    /// <typeparam name="UnitTestIndexTestDocumentA">The type of the index item.</typeparam>
    internal class TestWqlExpressionNodeFilterFunctionConstant<TIndexItem> : IWqlExpressionNodeFilterFunction<TIndexItem> where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        // <summary>
        // Returns the name of the function.
        // </summary>
        public string Name => "Test";

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public object Execute()
        {
            return "abc";
        }
    }
}
