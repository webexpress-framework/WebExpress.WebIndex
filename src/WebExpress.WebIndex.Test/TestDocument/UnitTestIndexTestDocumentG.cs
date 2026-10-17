namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Represents a test document whose attributes cover every type WQL converts literals
    /// into, so that each query can be compared between the reverse index and a query.
    /// </summary>
    public class UnitTestIndexTestDocumentG : UnitTestIndexTestDocument
    {
        /// <summary>
        /// The states of a document, for comparisons on an enumeration.
        /// </summary>
        public enum StateKind
        {
            Open,
            Pending,
            Closed
        }

        /// <summary>
        /// Represents an address, for nested attributes that may be absent.
        /// </summary>
        public class AddressClass
        {
            /// <summary>
            /// Gets or sets the city.
            /// </summary>
            public string City { get; set; }

            /// <summary>
            /// Gets or sets the zip code.
            /// </summary>
            public int Zip { get; set; }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets an optional number.
        /// </summary>
        public int? Optional { get; set; }

        /// <summary>
        /// Gets or sets whether the document is active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        public StateKind State { get; set; }

        /// <summary>
        /// Gets or sets a reference to another document.
        /// </summary>
        public Guid Reference { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public AddressClass Address { get; set; }

        /// <summary>
        /// Convert the object into a string representation.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Id} - {Text}";
        }
    }
}
