using System.Runtime.CompilerServices;
using WebExpress.WebIndex.Storage;
using Xunit;

namespace WebExpress.WebIndex.Test.Storage
{
    /// <summary>
    /// Tests that a damaged or foreign index file is refused when its header is read, instead of
    /// failing later on every lookup.
    /// </summary>
    public class UnitTestIndexStorageHeaderValidation
    {
        /// <summary>
        /// Verifies that a bucket count of zero - every lookup divided by it - or one past what an
        /// address can reach is refused, and a sound one is taken.
        /// </summary>
        /// <param name="count">The stored bucket count.</param>
        /// <param name="valid">Whether the count is accepted.</param>
        [Theory]
        [InlineData(0u, false)]
        [InlineData(uint.MaxValue, false)]
        [InlineData(997u, true)]
        public void BucketCountIsValidated(uint count, bool valid)
        {
            // arrange
            var map = (IndexStorageSegmentHashMap)RuntimeHelpers.GetUninitializedObject(typeof(IndexStorageSegmentHashMap));
            using var stream = new MemoryStream(BitConverter.GetBytes(count));
            using var reader = new BinaryReader(stream);

            // act
            var error = Record.Exception(() => map.Read(reader));

            // validation
            if (valid)
            {
                Assert.Null(error);
                Assert.Equal(count, map.BucketCount);
            }
            else
            {
                Assert.IsType<InvalidDataException>(error);
            }
        }
    }
}
