using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentG;

namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Creates the test data of document G: a small set of hand-written documents whose
    /// values exercise null, case, diacritics and every attribute type, and a bulk of
    /// generated documents that pushes the result of a query beyond the default limit of
    /// the reverse index.
    /// </summary>
    public class UnitTestIndexTestDocumentFactoryG : UnitTestIndexTestDocumentFactory
    {
        /// <summary>
        /// The number of hand-written documents.
        /// </summary>
        public const int HandwrittenCount = 16;

        /// <summary>
        /// The number of generated documents, more than the 10,000 results the reverse
        /// index returns by default.
        /// </summary>
        public const int BulkCount = 10200;

        /// <summary>
        /// Returns the identifier of a hand-written document.
        /// </summary>
        /// <param name="number">The number of the document, starting at 1.</param>
        /// <returns>The identifier.</returns>
        public static Guid Id(int number)
        {
            return new Guid($"00000000-0000-0000-0000-{number:D12}");
        }

        /// <summary>
        /// Returns the reference of a hand-written document.
        /// </summary>
        /// <param name="number">The number of the document, starting at 1.</param>
        /// <returns>The reference.</returns>
        public static Guid Reference(int number)
        {
            return new Guid($"10000000-0000-0000-0000-{number:D12}");
        }

        /// <summary>
        /// Generates the test data.
        /// </summary>
        /// <returns>The hand-written documents followed by the generated ones.</returns>
        public static List<UnitTestIndexTestDocumentG> GenerateTestData()
        {
            var data = new List<UnitTestIndexTestDocumentG>
            {
                Create(1, "alpha bravo charlie", 1, 10m, 1, true, new DateTime(2024, 1, 15), StateKind.Open, "Berlin", 10115),
                Create(2, "bravo alpha", 2, 12m, null, false, new DateTime(2024, 1, 20), StateKind.Pending, "Hamburg", 20095),
                Create(3, "alpha echo charlie", 3, 15m, 3, true, new DateTime(2024, 2, 1), StateKind.Closed, null, 0),
                Create(4, "Charlie delta", 4, 20m, null, true, new DateTime(2024, 2, 15, 10, 30, 0), StateKind.Open, "Berlin", 10117),
                Create(5, "ALPHA", 5, 25m, 5, false, new DateTime(2024, 2, 20), StateKind.Closed, "München", 80331),
                Create(6, "foxtrot golf hotel", 6, 30m, 6, true, new DateTime(2024, 3, 1), StateKind.Pending, "Köln", 50667),
                Create(7, "hotel india", 7, 35m, null, false, new DateTime(2024, 3, 15), StateKind.Open, null, 0),
                Create(8, null, 8, 40m, 8, true, new DateTime(2024, 4, 1), StateKind.Closed, "Berlin", 10115),
                Create(9, "", 9, 45m, 9, false, new DateTime(2024, 4, 15), StateKind.Open, "Hamburg", 20095),
                Create(10, "kilo lima mike", 10, 50m, null, true, new DateTime(2024, 5, 1), StateKind.Pending, "Bremen", 28195),
                Create(11, "kilo'lima alpha", -4, 12m, 2, false, new DateTime(2023, 12, 31), StateKind.Closed, "Berlin", 10178),
                Create(12, "alpah bravo", 0, 0m, 4, true, new DateTime(2024, 1, 1), StateKind.Open, null, 0),
                Create(13, "  zulu  ", 13, 99m, 13, false, new DateTime(2024, 6, 30, 23, 59, 0), StateKind.Pending, "Leipzig", 4109),
                Create(14, "café crème", 14, 14m, null, true, new DateTime(2024, 7, 14), StateKind.Open, "Paris", 75001),
                Create(15, "alpha xray charlie", 15, 75m, 15, false, new DateTime(2024, 8, 1), StateKind.Closed, "Berlin", 10115),
                Create(16, "oscar papa quebec romeo sierra tango", 16, 80m, null, true, new DateTime(2024, 9, 1), StateKind.Pending, "Dresden", 1067)
            };

            for (var i = 0; i < BulkCount; i++)
            {
                data.Add(new UnitTestIndexTestDocumentG
                {
                    Id = new Guid($"20000000-0000-0000-0000-{i:D12}"),
                    Text = $"bulk {i}",
                    Number = 1000 + i,
                    Price = 100 + i % 50,
                    Optional = i % 3 == 0 ? null : i % 7,
                    Active = i % 2 == 0,
                    Date = new DateTime(2025, 1, 1).AddDays(i % 365),
                    State = (StateKind)(i % 3),
                    Reference = new Guid($"30000000-0000-0000-0000-{i:D12}"),
                    Address = i % 4 == 0 ? null : new AddressClass { City = "Bulkstadt", Zip = 90000 + i % 10 }
                });
            }

            return data;
        }

        /// <summary>
        /// Creates a hand-written document.
        /// </summary>
        /// <param name="number">The number of the document.</param>
        /// <param name="text">The text.</param>
        /// <param name="value">The number.</param>
        /// <param name="price">The price; whole numbers only, see the remark below.</param>
        /// <param name="optional">The optional number.</param>
        /// <param name="active">Whether the document is active.</param>
        /// <param name="date">The date.</param>
        /// <param name="state">The state.</param>
        /// <param name="city">The city, or null for a document without address.</param>
        /// <param name="zip">The zip code.</param>
        /// <returns>The document.</returns>
        /// <remarks>
        /// The numeric reverse index reads a value from its text in the current culture with
        /// the culture of the index, so a price with decimal places would be indexed wrongly
        /// on a machine whose decimal separator is the group separator of the index culture.
        /// </remarks>
        private static UnitTestIndexTestDocumentG Create(int number, string text, int value, decimal price, int? optional, bool active, DateTime date, StateKind state, string city, int zip)
        {
            return new UnitTestIndexTestDocumentG
            {
                Id = Id(number),
                Text = text,
                Number = value,
                Price = price,
                Optional = optional,
                Active = active,
                Date = date,
                State = state,
                Reference = Reference(number),
                Address = city is null ? null : new AddressClass { City = city, Zip = zip }
            };
        }
    }
}
