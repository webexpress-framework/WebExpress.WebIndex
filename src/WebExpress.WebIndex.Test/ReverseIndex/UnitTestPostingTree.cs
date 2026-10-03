using System.Globalization;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;

namespace WebExpress.WebIndex.Test.ReverseIndex
{
    /// <summary>
    /// Tests the balance of the on-disk posting trees. Document ids arrive in insertion order, and
    /// sequential ids would degrade an unbalanced tree to a list; the tests insert them in ascending
    /// order, which is the worst case, and check the AVL bounds after inserting, removing and reopening.
    /// </summary>
    /// <param name="fixture">The fixture.</param>
    /// <param name="output">The test context.</param>
    [Collection("NonParallelTests")]
    public class UnitTestPostingTree(UnitTestIndexFixture fixture, ITestOutputHelper output) : UnitTestReverseIndex<UnitTestIndexFixture>(fixture, output)
    {
        /// <summary>
        /// A document with one text and one numeric field.
        /// </summary>
        public class Document : IIndexItem
        {
            /// <summary>
            /// Returns or sets the id.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Returns or sets the text; every document carries the same term.
            /// </summary>
            public string Text { get; set; } = "balanced";

            /// <summary>
            /// Returns or sets the price; every document carries the same value.
            /// </summary>
            public double Price { get; set; } = 42;
        }

        /// <summary>
        /// Returns the text field.
        /// </summary>
        private static IndexFieldData TextField => new()
        {
            Name = "Text",
            PropertyInfo = typeof(Document).GetProperty("Text"),
            Type = typeof(Document)
        };

        /// <summary>
        /// Returns the price field.
        /// </summary>
        private static IndexFieldData PriceField => new()
        {
            Name = "Price",
            PropertyInfo = typeof(Document).GetProperty("Price"),
            Type = typeof(Document)
        };

        /// <summary>
        /// The number of documents; enough for an unbalanced tree to be a hundred times deeper than a balanced one.
        /// </summary>
        private const int Count = 300;

        /// <summary>
        /// The AVL height bound, 1.44 log2(n + 2), with a little room.
        /// </summary>
        private static uint HeightBound(int count)
        {
            return (uint)Math.Ceiling(1.44 * Math.Log2(count + 2)) + 1;
        }

        /// <summary>
        /// Builds documents whose ids ascend, so they arrive at the tree in sorted order.
        /// </summary>
        private static List<Document> Documents()
        {
            return [.. Enumerable.Range(1, Count).Select(i => new Document { Id = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) })];
        }

        /// <summary>
        /// The term tree stays within the AVL bounds however the ids arrive.
        /// </summary>
        [Fact]
        public void TermPostingsStayBalanced()
        {
            // arrange
            Preconditions();
            var documents = Documents();
            var reverseIndex = new IndexStorageReverseTerm<Document>(Context, TextField, CultureInfo.GetCultureInfo("en"));

            // act
            foreach (var document in documents)
            {
                reverseIndex.Add(document);
            }

            // assert
            var root = reverseIndex.Term.Terms.Single().Item2.Posting;
            AssertBalanced(root, documents.Select(x => x.Id));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Removing postings - including the root's and nodes with two children - keeps the tree
        /// balanced and the positions of the remaining postings intact.
        /// </summary>
        [Fact]
        public void TermPostingsStayBalancedAfterRemoval()
        {
            // arrange
            Preconditions();
            var documents = Documents();
            var reverseIndex = new IndexStorageReverseTerm<Document>(Context, TextField, CultureInfo.GetCultureInfo("en"));

            foreach (var document in documents)
            {
                reverseIndex.Add(document);
            }

            var term = reverseIndex.Term.Terms.Single().Item2;
            var rootId = term.Posting.DocumentID;

            // act - the root, every third document and the last one
            var removed = documents.Where((x, i) => x.Id == rootId || i % 3 == 0 || i == documents.Count - 1).ToList();

            foreach (var document in removed)
            {
                reverseIndex.Delete(document);
            }

            // assert
            var remaining = documents.Except(removed).Select(x => x.Id).ToList();
            var root = reverseIndex.Term.Terms.Single().Item2.Posting;

            AssertBalanced(root, remaining);
            Assert.Equal((uint)remaining.Count, term.Frequency);
            Assert.All(root.PreOrder, node => Assert.Equal([0u], node.Positions.Select(x => x.Position)));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// The heights are part of the file: a tree reopened from disk reports the same shape and
        /// goes on balancing.
        /// </summary>
        [Fact]
        public void TermPostingsKeepTheirHeightsOnDisk()
        {
            // arrange
            Preconditions();
            var documents = Documents();
            var reverseIndex = new IndexStorageReverseTerm<Document>(Context, TextField, CultureInfo.GetCultureInfo("en"));

            foreach (var document in documents.Take(Count / 2))
            {
                reverseIndex.Add(document);
            }

            var heightBefore = reverseIndex.Term.Terms.Single().Item2.Posting.Height;
            reverseIndex.Dispose();

            // act
            reverseIndex = new IndexStorageReverseTerm<Document>(Context, TextField, CultureInfo.GetCultureInfo("en"));
            var heightAfter = reverseIndex.Term.Terms.Single().Item2.Posting.Height;

            foreach (var document in documents.Skip(Count / 2))
            {
                reverseIndex.Add(document);
            }

            // assert
            Assert.Equal(heightBefore, heightAfter);
            AssertBalanced(reverseIndex.Term.Terms.Single().Item2.Posting, documents.Select(x => x.Id));

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// The numeric tree stays within the AVL bounds however the ids arrive, through removals too.
        /// </summary>
        [Fact]
        public void NumericPostingsStayBalanced()
        {
            // arrange
            Preconditions();
            var documents = Documents();
            var reverseIndex = new IndexStorageReverseNumeric<Document>(Context, PriceField, CultureInfo.GetCultureInfo("en"));

            // act
            foreach (var document in documents)
            {
                reverseIndex.Add(document);
            }

            // assert
            AssertBalanced(reverseIndex.Numeric[42].Posting, documents.Select(x => x.Id));

            // act - the root, every third document and the last one
            var rootId = reverseIndex.Numeric[42].Posting.DocumentID;
            var removed = documents.Where((x, i) => x.Id == rootId || i % 3 == 0 || i == documents.Count - 1).ToList();

            foreach (var document in removed)
            {
                reverseIndex.Delete(document);
            }

            // assert
            var remaining = documents.Except(removed).Select(x => x.Id).ToList();

            AssertBalanced(reverseIndex.Numeric[42].Posting, remaining);
            Assert.Equal((uint)remaining.Count, reverseIndex.Numeric[42].Frequency);

            // postconditions
            reverseIndex.Dispose();
            Postconditions();
        }

        /// <summary>
        /// Checks the AVL invariants of a term posting tree and that it holds exactly the given ids.
        /// </summary>
        private static void AssertBalanced(IndexStorageSegmentPostingNode root, IEnumerable<Guid> expected)
        {
            var ids = expected.ToList();
            var inOrder = InOrder(root).ToList();

            Assert.Equal(ids.Order().ToList(), inOrder.Select(x => x.DocumentID).ToList());
            Assert.True(root.Height <= HeightBound(ids.Count), $"height {root.Height} exceeds the AVL bound {HeightBound(ids.Count)} for {ids.Count} postings");
            Assert.All(inOrder, node => Assert.True(node.Balance <= 1, $"node {node.DocumentID} is out of balance"));
            Assert.All(inOrder, node => Assert.Equal(Math.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0) + 1, node.Height));
        }

        /// <summary>
        /// Checks the AVL invariants of a numeric posting tree and that it holds exactly the given ids.
        /// </summary>
        private static void AssertBalanced(IndexStorageSegmentNumericPostingNode root, IEnumerable<Guid> expected)
        {
            var ids = expected.ToList();
            var inOrder = InOrder(root).ToList();

            Assert.Equal(ids.Order().ToList(), inOrder.Select(x => x.DocumentID).ToList());
            Assert.True(root.Height <= HeightBound(ids.Count), $"height {root.Height} exceeds the AVL bound {HeightBound(ids.Count)} for {ids.Count} postings");
            Assert.All(inOrder, node => Assert.True(node.Balance <= 1, $"node {node.DocumentID} is out of balance"));
            Assert.All(inOrder, node => Assert.Equal(Math.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0) + 1, node.Height));
        }

        /// <summary>
        /// Walks a term posting tree in order, which is ascending by id for a search tree.
        /// </summary>
        private static IEnumerable<IndexStorageSegmentPostingNode> InOrder(IndexStorageSegmentPostingNode node)
        {
            if (node is null)
            {
                yield break;
            }

            foreach (var left in InOrder(node.Left))
            {
                yield return left;
            }

            yield return node;

            foreach (var right in InOrder(node.Right))
            {
                yield return right;
            }
        }

        /// <summary>
        /// Walks a numeric posting tree in order, which is ascending by id for a search tree.
        /// </summary>
        private static IEnumerable<IndexStorageSegmentNumericPostingNode> InOrder(IndexStorageSegmentNumericPostingNode node)
        {
            if (node is null)
            {
                yield break;
            }

            foreach (var left in InOrder(node.Left))
            {
                yield return left;
            }

            yield return node;

            foreach (var right in InOrder(node.Right))
            {
                yield return right;
            }
        }
    }
}
