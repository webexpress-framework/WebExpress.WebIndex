using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentB;

namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Factory class for creating unit test documents of type UnitTestIndexTestDocumentB.
    /// </summary>
    public class UnitTestIndexTestDocumentFactoryB : UnitTestIndexTestDocumentFactory
    {
        /// <summary>
        /// Generates a list of test data for unit testing.
        /// </summary>
        /// <returns>
        /// A list of objects.
        /// </returns>
        public static List<UnitTestIndexTestDocumentB> GenerateTestData()
        {
            var testDataList = new List<UnitTestIndexTestDocumentB>();

            // Add more test data here
            for (int i = 0; i < 100; i++)
            {
                testDataList.Add(new UnitTestIndexTestDocumentB
                {
                    Id = Guid.NewGuid(),
                    Name = $"Name_{i}",
                    Summary = $"The Name_{i}",
                    Description = GenerateLoremIpsum(100),
                    Date = DateTime.Now.AddMonths(i % 12),
                    Price = i,
                    New = i % 2 != 0,
                    Address = new UnitTestIndexTestDocumentB.AddressClass()
                    {
                        Country = Country.USA,
                        City = GenerateCity(1),
                        Zip = GenerateZip(i),
                        Street = GenerateSreet(i)
                    }
                });
            }

            return testDataList;
        }
    }
}
