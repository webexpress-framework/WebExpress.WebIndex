namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Factory class for creating unit test documents of type UnitTestIndexTestDocumentC.
    /// </summary>
    public class UnitTestIndexTestDocumentFactoryC : UnitTestIndexTestDocumentFactory
    {
        /// <summary>
        /// Generates a list of test data for unit testing.
        /// </summary>
        /// <returns>
        /// A list of objects.
        /// </returns>
        public static List<UnitTestIndexTestDocumentC> GenerateTestData()
        {
            return GenerateTestData(100, 10, 3000, 20).ToList();
        }

        /// <summary>
        /// Generates a list of test data for unit testing.
        /// </summary>
        /// <returns>
        /// A list of objects.
        /// </returns>
        public static IEnumerable<UnitTestIndexTestDocumentC> GenerateTestData(int itemCount, int wordCount, int vocabulary, int wordLength)
        {
            // a locally owned, seeded generator makes the data reproducible across runs and is safe
            // under parallel test execution, unlike the process-wide shared random instance
            var random = new Random(10);

            // generate a vocabulary with the specified size and word length
            var set = GenerateVocabulary(random, vocabulary, 3, wordLength).ToList(); // convert to list for efficient indexing
            if (set.Count == 0)
            {
                throw new ArgumentException("Vocabulary must contain at least one word.", nameof(vocabulary));
            }

            // check bounds for generation
            if (itemCount <= 0) throw new ArgumentOutOfRangeException(nameof(itemCount), "Item count must be greater than zero.");
            if (wordCount <= 0) throw new ArgumentOutOfRangeException(nameof(wordCount), "Word count must be greater than zero.");

            for (int i = 0; i < itemCount; i++)
            {
                var words = new List<string>();

                for (int j = 0; j < wordCount; j++)
                {
                    // select a random word from the vocabulary
                    var randomWord = set[random.Next(set.Count)];
                    words.Add(randomWord);
                }

                yield return new UnitTestIndexTestDocumentC
                {
                    Id = Guid.NewGuid(),
                    Text = string.Join(" ", words), // create a space-separated text
                    Number = i                      // assign a unique number to each record
                };
            }
        }
    }
}
