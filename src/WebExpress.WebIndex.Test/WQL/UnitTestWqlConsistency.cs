using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentFactoryG;
using static WebExpress.WebIndex.Test.Document.UnitTestIndexTestDocumentG;

namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Runs every statement through the reverse index and as a query and checks both results
    /// against the same expected items: operator precedence, null, every attribute type,
    /// paging and results beyond the default limit of the index.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestWqlConsistency(UnitTestIndexFixtureWqlG fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlG>
    {
        private static readonly int[] Alpha = [1, 2, 3, 5, 11, 15];

        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlG Fixture { get; set; } = fixture;

        /// <summary>
        /// Tests that "and" binds stronger than "or" and that chains group from the left.
        /// </summary>
        [Fact]
        public void OperatorPrecedence()
        {
            // a right-grouping parser would read this as bravo and (active or alpha) and lose items 3, 5, 11 and 15
            Fixture.AssertPaths("Text ~ 'bravo' and Active = true or Text ~ 'alpha'", [1, 2, 3, 5, 11, 12, 15]);
            Fixture.AssertPaths("Number < 3 or Number > 14 and Number < 16", x => x.Number < 3 || x.Number > 14 && x.Number < 16);
            Fixture.AssertPaths("Active = true and Number > 10 or Number < 0", x => x.Active && x.Number > 10 || x.Number < 0);
            Fixture.AssertPaths("Number < 0 or Active = true and Number > 10", x => x.Number < 0 || x.Active && x.Number > 10);
            Fixture.AssertPaths("(Number < 3 or Number > 14) and Number < 16", x => (x.Number < 3 || x.Number > 14) && x.Number < 16);
            Fixture.AssertPaths("Number = 1 or Number = 2 or Number = 3 and Active = true", x => x.Number is 1 or 2 || x.Number == 3 && x.Active);
            Fixture.AssertPaths("Number > 0 & Number < 5 || Number = 13", x => x.Number > 0 && x.Number < 5 || x.Number == 13);
        }

        /// <summary>
        /// Tests that the tree groups chains from the left and nests "and" below "or".
        /// </summary>
        [Fact]
        public void OperatorPrecedenceStructure()
        {
            Assert.Equal("((Number = 1 or Number = 2) or Number = 3)", Fixture.ExecuteWql("Number = 1 or Number = 2 or Number = 3").ToString());
            Assert.Equal("(Number = 1 or (Number = 2 and Number = 3))", Fixture.ExecuteWql("Number = 1 or Number = 2 and Number = 3").ToString());
            Assert.Equal("((Number = 1 and Number = 2) or Number = 3)", Fixture.ExecuteWql("Number = 1 and Number = 2 or Number = 3").ToString());
            Assert.Equal("((Number = 1 or Number = 2) and Number = 3)", Fixture.ExecuteWql("(Number = 1 or Number = 2) and Number = 3").ToString());
        }

        /// <summary>
        /// Tests the text contract: phrase, words, distance, wildcards, similarity, case and
        /// diacritics.
        /// </summary>
        [Fact]
        public void Text()
        {
            Fixture.AssertPaths("Text ~ 'alpha'", Alpha);
            Fixture.AssertPaths("Text = 'ALPHA'", Alpha);
            Fixture.AssertPaths("Text = 'alpha bravo'", [1]);
            Fixture.AssertPaths("Text = 'bravo alpha'", [2]);
            Fixture.AssertPaths("Text ~ 'bravo alpha'", [1, 2]);
            Fixture.AssertPaths("Text = 'alpha charlie' :1", [1, 3, 15]);
            Fixture.AssertPaths("Text ~ 'charlie alpha' :2", [1, 3, 15]);
            Fixture.AssertPaths("Text ~ 'al*'", [1, 2, 3, 5, 11, 12, 15]);
            Fixture.AssertPaths("Text ~ 'br?vo'", [1, 2, 12]);
            Fixture.AssertPaths("Text ~ '*lie'", [1, 3, 4, 15]);
            Fixture.AssertPaths("Text ~ 'alpah' ~60", [1, 2, 3, 5, 11, 12, 15]);
            Fixture.AssertPaths("Text ~ 'cafe'", [14]);
            Fixture.AssertPaths("Text = 'crème'", [14]);
            Fixture.AssertPaths("Text = 'kilo''lima'", [10, 11]);
            Fixture.AssertPaths("Text ~ zulu", [13]);
            Fixture.AssertPaths("Text != 'alpha'", x => !Alpha.Select(Id).Contains(x.Id));
            Fixture.AssertPaths("Text in ('alpha bravo', 'zulu')", [1, 13]);
            Fixture.AssertPaths("Text not in ('alpha', 'bulk')", x => x.Number < 1000 && !Alpha.Select(Id).Contains(x.Id));
            Fixture.AssertNone("Text ~ 'xyz'");
            Fixture.AssertNone("Text = 'charlie alpha'");
            Fixture.AssertNone("Text = ''");
        }

        /// <summary>
        /// Tests the ordering of text, which compares without regard to case and never
        /// orders null.
        /// </summary>
        [Fact]
        public void TextOrdering()
        {
            Fixture.AssertPaths("Text > 'k'", x => x.Text is not null && string.Compare(x.Text, "k", StringComparison.OrdinalIgnoreCase) > 0);
            Fixture.AssertPaths("Text <= 'BRAVO'", x => x.Text is not null && string.Compare(x.Text, "bravo", StringComparison.OrdinalIgnoreCase) <= 0);
            Fixture.AssertPaths("Text >= ''", x => x.Text is not null);
        }

        /// <summary>
        /// Tests that null is equal to null only, differs from every value and is never ordered.
        /// </summary>
        [Fact]
        public void Null()
        {
            Fixture.AssertPaths("Text is null", [8]);
            Fixture.AssertPaths("Text is not null", x => x.Text is not null);
            Fixture.AssertPaths("Optional is null", x => x.Optional is null);
            Fixture.AssertPaths("Optional is not null", x => x.Optional is not null);
            Fixture.AssertPaths("Optional = 5", x => x.Optional == 5);
            Fixture.AssertPaths("Optional != 5", x => x.Optional != 5);
            Fixture.AssertPaths("Optional > 3", x => x.Optional > 3);
            Fixture.AssertPaths("Optional <= 3", x => x.Optional <= 3);
            Fixture.AssertPaths("Optional in (1, 13)", x => x.Optional is 1 or 13);
            Fixture.AssertPaths("Optional not in (1, 13)", x => x.Optional is not (1 or 13));
            Fixture.AssertPaths("Address is null", x => x.Address is null);
            Fixture.AssertPaths("Address.City is null", x => x.Address?.City is null);
            Fixture.AssertPaths("Address.Zip = 10115", x => x.Address?.Zip == 10115);
            Fixture.AssertPaths("Address.Zip != 10115", x => x.Address?.Zip != 10115);
            Fixture.AssertPaths("Address.Zip > 50000", x => x.Address?.Zip > 50000);
            Fixture.AssertPaths("Address.City = 'berlin'", x => x.Address?.City == "Berlin");
            Fixture.AssertPaths("Address.City ~ 'munchen'", [5]);
        }

        /// <summary>
        /// Tests integral, decimal and boolean attributes.
        /// </summary>
        [Fact]
        public void Numbers()
        {
            Fixture.AssertPaths("Number = 3", x => x.Number == 3);
            Fixture.AssertPaths("Number != 3", x => x.Number != 3);
            Fixture.AssertPaths("Number = -4", x => x.Number == -4);
            Fixture.AssertPaths("Number > 10", x => x.Number > 10);
            Fixture.AssertPaths("Number >= 11000", x => x.Number >= 11000);
            Fixture.AssertPaths("Number < 0", x => x.Number < 0);
            Fixture.AssertPaths("Number <= 5", x => x.Number <= 5);
            Fixture.AssertPaths("Number in (1, 3, 5)", x => x.Number is 1 or 3 or 5);
            Fixture.AssertPaths("Number not in (1, 3, 5)", x => x.Number is not (1 or 3 or 5));
            Fixture.AssertPaths("Number = '7'", x => x.Number == 7);
            Fixture.AssertPaths("Price > 20", x => x.Price > 20);
            Fixture.AssertPaths("Price = 12", x => x.Price == 12);
            Fixture.AssertPaths("Price <= 12.5", x => x.Price <= 12.5m);
            Fixture.AssertPaths("Price in (10, 75)", x => x.Price is 10 or 75);
            Fixture.AssertPaths("Active = true", x => x.Active);
            Fixture.AssertPaths("Active is false", x => !x.Active);
            Fixture.AssertPaths("Active != true", x => !x.Active);
            Fixture.AssertPaths("Active is not true", x => !x.Active);
            Fixture.AssertPaths("Active in (false)", x => !x.Active);
        }

        /// <summary>
        /// Tests dates, enumerations and Guids, which the reverse index cannot compare and
        /// which therefore scan the document store.
        /// </summary>
        [Fact]
        public void ScannedTypes()
        {
            Fixture.AssertPaths("Date = 2024-01-15", x => x.Date == new DateTime(2024, 1, 15));
            Fixture.AssertPaths("Date != 2024-01-15", x => x.Date != new DateTime(2024, 1, 15));
            Fixture.AssertPaths("Date > '2024-03-01'", x => x.Date > new DateTime(2024, 3, 1));
            Fixture.AssertPaths("Date >= '2024-02-15 10:30'", x => x.Date >= new DateTime(2024, 2, 15, 10, 30, 0));
            Fixture.AssertPaths("Date < 2024-01-01", x => x.Date < new DateTime(2024, 1, 1));
            Fixture.AssertPaths("Date in ('2024-01-15', '2024-02-01')", x => x.Date == new DateTime(2024, 1, 15) || x.Date == new DateTime(2024, 2, 1));
            Fixture.AssertPaths("State = Closed", x => x.State == StateKind.Closed);
            Fixture.AssertPaths("State = 2", x => x.State == StateKind.Closed);
            Fixture.AssertPaths("State != 'open'", x => x.State != StateKind.Open);
            Fixture.AssertPaths("State in (Open, Pending)", x => x.State is StateKind.Open or StateKind.Pending);
            Fixture.AssertPaths("State > Open", x => x.State > StateKind.Open);
            Fixture.AssertPaths("State <= Pending", x => x.State <= StateKind.Pending);
            Fixture.AssertPaths($"Reference = '{Reference(3)}'", [3]);
            Fixture.AssertPaths($"Reference != '{Reference(3)}'", x => x.Reference != Reference(3));
            Fixture.AssertPaths($"Reference in ('{Reference(1)}', {Reference(2)})", [1, 2]);
            Fixture.AssertPaths("Reference > '30000000-0000-0000-0000-000000010000'", x => x.Reference.CompareTo(new Guid("30000000-0000-0000-0000-000000010000")) > 0);
        }

        /// <summary>
        /// Tests that skip and take form one window in the order written, applied alike in
        /// both paths.
        /// </summary>
        [Fact]
        public void Paging()
        {
            var handwritten = Fixture.TestData.Where(x => x.Number < 100).OrderBy(x => x.Number).ToList();

            Fixture.AssertOrdered("Number < 100 order by Number skip 2 take 3", handwritten.Skip(2).Take(3));
            Fixture.AssertOrdered("Number < 100 order by Number take 6 skip 4", handwritten.Take(6).Skip(4));
            Fixture.AssertOrdered("Number < 100 order by Number skip 3 take 2 skip 1", handwritten.Skip(4).Take(1));
            Fixture.AssertOrdered("Number < 100 order by Number take 5 take 3", handwritten.Take(3));
            Fixture.AssertOrdered("Number < 100 order by Number skip 20", []);
            Fixture.AssertOrdered("Number < 100 order by Number take 0", []);
            Fixture.AssertOrdered("order by Number desc take 3", Fixture.TestData.OrderByDescending(x => x.Number).Take(3));
            Fixture.AssertOrdered("Number < 100 order by Price desc, Number asc skip 1 take 4", handwritten.OrderByDescending(x => x.Price).ThenBy(x => x.Number).Skip(1).Take(4));
            Fixture.AssertOrdered("Number < 100 order by Address.City, Number", handwritten.OrderBy(x => x.Address?.City, StringComparer.CurrentCulture).ThenBy(x => x.Number));
        }

        /// <summary>
        /// Tests that the query receives skip and take in the right order.
        /// </summary>
        [Fact]
        public void PagingToQuery()
        {
            var query = Fixture.ExecuteWql("Number > 1 skip 2 take 3").ToQuery();
            var reversed = Fixture.ExecuteWql("Number > 1 take 10 skip 4").ToQuery();
            var skipOnly = Fixture.ExecuteWql("Number > 1 skip 7").ToQuery();

            Assert.Equal(2, query.Skip);
            Assert.Equal(3, query.Take);
            Assert.Equal(4, reversed.Skip);
            Assert.Equal(6, reversed.Take);
            Assert.Equal(7, skipOnly.Skip);
            Assert.Null(skipOnly.Take);
            Assert.Equal(0, Fixture.ExecuteWql("Number > 1 take 0").ToQuery().Take);
        }

        /// <summary>
        /// Tests that the query folds skip and take exactly like the reverse index path, by
        /// comparing the results of both paths with each other.
        /// </summary>
        /// <param name="partitioning">The skip and take functions.</param>
        [Theory]
        [InlineData("skip 2 take 3")]
        [InlineData("take 6 skip 4")]
        [InlineData("take 3")]
        [InlineData("skip 4")]
        [InlineData("skip 2 skip 3")]
        public void PagingBothPaths(string partitioning)
        {
            var statement = Fixture.ExecuteWql($"Number < 100 order by Number {partitioning}");

            var indexed = Fixture.RetrieveIndexed(statement);
            var queried = Fixture.RetrieveQueried(statement);

            Assert.NotEmpty(indexed);
            Assert.Equal(indexed, queried);
        }

        /// <summary>
        /// Tests results beyond the 10,000 items the reverse index returns by default, also
        /// within a complement.
        /// </summary>
        [Fact]
        public void LargeResults()
        {
            Fixture.AssertPaths("Text ~ 'bulk'", x => x.Number >= 1000);
            Fixture.AssertPaths("Number >= 1000", x => x.Number >= 1000);
            Fixture.AssertPaths("Text != 'bulk'", x => x.Number < 1000);
            Fixture.AssertPaths("Text ~ 'bulk' and Active = true", x => x.Number >= 1000 && x.Active);
            Fixture.AssertPaths("Text ~ 'bulk' or Number < 3", x => x.Number >= 1000 || x.Number < 3);

            Assert.True(Fixture.RetrieveIndexed(Fixture.ExecuteWql("Text ~ 'bulk'")).Count > 10000);
        }

        /// <summary>
        /// Tests that a statement with errors is rejected by both paths.
        /// </summary>
        [Theory]
        [InlineData("Nonexistent = 1")]
        [InlineData("Number = 5.5")]
        [InlineData("Number > 5 ~80")]
        [InlineData("Text ~ 'a' order by Nonexistent")]
        [InlineData("Text ~ 'unterminated")]
        public void ErroneousStatement(string wql)
        {
            var statement = Fixture.ExecuteWql(wql);
            var document = Fixture.IndexManager.GetIndexDocument<UnitTestIndexTestDocumentG>();

            Assert.True(statement.HasErrors);
            Assert.Throws<InvalidOperationException>(() => statement.Apply(document));
            Assert.Throws<InvalidOperationException>(() => statement.ToQuery());
            Assert.Empty(Fixture.IndexManager.Retrieve(statement));
        }
    }
}
