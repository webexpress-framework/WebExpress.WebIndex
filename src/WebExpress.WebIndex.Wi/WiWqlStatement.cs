using System.Reflection;
using WebExpress.WebIndex.Wql;

namespace WebExpress.WebIndex.Wi
{
    /// <summary>
    /// Non-generic adapter over <see cref="IWqlStatement{TIndexItem}"/> used by the wi console.
    /// Holds a typed statement together with its index document and exposes the
    /// members the wi tool needs without requiring compile-time knowledge of the item type.
    /// </summary>
    internal class WiWqlStatement
    {
        private readonly object _statement;
        private readonly object _indexDocument;
        private readonly Type _itemType;

        /// <summary>
        /// Initializes a new instance wrapping the given typed statement and index document.
        /// </summary>
        /// <param name="statement">The typed <see cref="IWqlStatement{TIndexItem}"/> instance.</param>
        /// <param name="indexDocument">The <see cref="IIndexDocument{TIndexItem}"/> the statement will be applied to.</param>
        /// <param name="itemType">The item type the statement was parsed for.</param>
        public WiWqlStatement(object statement, object indexDocument, Type itemType)
        {
            _statement = statement;
            _indexDocument = indexDocument;
            _itemType = itemType;
        }

        /// <summary>
        /// Returns true if there are any errors that occurred during parsing.
        /// </summary>
        public bool HasErrors
            => _statement is not null
               && (bool)_statement.GetType().GetProperty(nameof(HasErrors)).GetValue(_statement);

        /// <summary>
        /// Returns the error of the original wql statement, or null.
        /// </summary>
        public WqlExpressionError Error
            => _statement?.GetType().GetProperty(nameof(Error)).GetValue(_statement) as WqlExpressionError;

        /// <summary>
        /// Applies the statement to its index document and returns the matching items as objects.
        /// </summary>
        /// <param name="dataType">The item type. Must match the type the statement was parsed for.</param>
        /// <returns>An enumeration of matching items, or an empty sequence on error.</returns>
        public IEnumerable<object> Apply(Type dataType)
        {
            if (_statement is null || _indexDocument is null)
            {
                return [];
            }

            var apply = _statement.GetType().GetMethod(nameof(Apply), [typeof(IIndexDocument<>).MakeGenericType(_itemType)]);
            if (apply is null)
            {
                return [];
            }

            var result = apply.Invoke(_statement, [_indexDocument]);
            return ((System.Collections.IEnumerable)result).Cast<object>();
        }
    }
}
