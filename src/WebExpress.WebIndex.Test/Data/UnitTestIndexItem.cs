namespace WebExpress.WebIndex.Test.Data
{
    /// <summary>
    /// Provides unit tests for deterministic identifier behavior of IndexItem.
    /// </summary>
    public class UnitTestIndexItem
    {
        /// <summary>
        /// Verifies that the Id property of an IndexItem returns a stable (deterministic) 
        /// value across multiple accesses on the same instance.
        /// </summary>
        [Fact]
        public void IsStable()
        {
            // arrange
            var item = new IndexItem();

            // act
            var id1 = item.Id;
            var id2 = item.Id;
            var id3 = item.Id;

            // validation
            Assert.Equal(id1, id2);
            Assert.Equal(id2, id3);
        }

        /// <summary>
        /// Verifies that two different IndexItem instances have distinct identifiers.
        /// </summary>
        [Fact]
        public void DistinctIds()
        {
            // arrange
            var item1 = new IndexItem();
            var item2 = new IndexItem();

            // validation
            Assert.NotEqual(item1.Id, item2.Id);
        }

        /// <summary>
        /// Verifies that the Id is a non-empty Guid.
        /// </summary>
        [Fact]
        public void IdNotEmpty()
        {
            // arrange
            var item = new IndexItem();

            // validation
            Assert.NotEqual(Guid.Empty, item.Id);
        }
    }
}
