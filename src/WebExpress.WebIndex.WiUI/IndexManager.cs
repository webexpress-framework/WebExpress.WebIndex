using System.Globalization;

namespace WebExpress.WebIndex.WiUI
{
    /// <summary>
    /// Implementation of the index manager.
    /// </summary>
    public class IndexManager : WebIndex.IndexManager
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IndexManager()
        {
        }

        /// <summary>
        /// Registers a data type in the index.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="type">The index type.</param>
        public void Create(Type dataType, CultureInfo culture, IndexType type = IndexType.Memory)
        {
            var genericMethod = typeof(IndexManager).GetMethod("Create", 1, [typeof(CultureInfo), typeof(IndexType)]);
            var specificMethod = genericMethod?.MakeGenericMethod(dataType);

            specificMethod?.Invoke(this, [culture, type]);
        }

        /// <summary>
        /// Closes the index file of type T.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        public void Close(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("Close", 1, []);
            var specificMethod = genericMethod?.MakeGenericMethod(dataType);

            specificMethod?.Invoke(this, []);
        }

        /// <summary>
        /// Removes all index documents of type.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        public void Drop(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("Drop", 1, []);
            var specificMethod = genericMethod?.MakeGenericMethod(dataType);

            specificMethod?.Invoke(this, []);
        }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <param name="item">The data to be added to the index.</param>
        public void Insert(Type dataType, object item)
        {
            var genericMethod = typeof(IndexManager).GetMethods()
                .Where(m => m.Name == "Insert" && m.IsGenericMethodDefinition)
                .First();
            var specificMethod = genericMethod.MakeGenericMethod(dataType);

            specificMethod.Invoke(this, [item]);
        }

        /// <summary>
        /// Clear all data from index document.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        public void Clear(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("Clear", 1, []);
            var specificMethod = genericMethod?.MakeGenericMethod(dataType);

            specificMethod?.Invoke(this, []);
        }

        /// <summary>
        /// Counts the number of items of the index.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <returns>The number of items.</returns>
        public uint Count(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("Count", 1, []);
            var specificMethod = genericMethod?.MakeGenericMethod(dataType);

            return (uint)specificMethod?.Invoke(this, [])!;
        }

        /// <summary>
        /// Returns all documents from the index.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <returns>An enumeration of the documents.</returns>
        public IEnumerable<object> All(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("All", 1, []);
            var specificMethod = genericMethod.MakeGenericMethod(dataType);

            return specificMethod.Invoke(this, []) as IEnumerable<object>;
        }

        /// <summary>
        /// Returns an index type based on its type.
        /// </summary>
        /// <param name="dataType">The data type. This must have the IIndexItem interface.</param>
        /// <returns>The index type or null.</returns>
        public IIndexDocument GetIndexDocument(Type dataType)
        {
            var genericMethod = typeof(IndexManager).GetMethod("GetIndexDocument", 1, []);
            var specificMethod = genericMethod.MakeGenericMethod(dataType);

            return specificMethod.Invoke(this, []) as IIndexDocument;
        }
    }
}
