using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Data class for unit testing.
    /// </summary>
    public class UnitTestIndexTestDocumentB : UnitTestIndexTestDocument
    {
        /// <summary>
        /// Enumeration representing various countries.
        /// </summary>
        public enum Country
        {
            Germany,
            France,
            Italy,
            Spain,
            England,
            USA,
            Canada,
            Australia,
            Japan,
            China
        }

        /// <summary>
        /// Class representing an address with street, city, and zip code.
        /// </summary>
        public class AddressClass
        {
            /// <summary>
            /// Returns or sets the country.
            /// </summary>
            public Country Country { get; set; }

            /// <summary>
            /// Returns or sets the street.
            /// </summary>
            public string Street { get; set; }

            /// <summary>
            /// Returns or sets the city.
            /// </summary>
            public string City { get; set; }

            /// <summary>
            /// Returns or sets the zip code.
            /// </summary>
            [IndexIgnore]
            public int Zip { get; set; }
        }

        /// <summary>
        /// Returns or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns or sets the summary.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Returns or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Returns or sets the date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Returns or sets the price.
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Returns or sets the new attribute.
        /// </summary>
        public bool New { get; set; }

        /// <summary>
        /// Returns or sets the address attribute.
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
            return string.Format("({0}:{1},{2},{3},{4})", Id, Name, Date.ToShortDateString(), Price, New).Trim();
        }
    }
}
