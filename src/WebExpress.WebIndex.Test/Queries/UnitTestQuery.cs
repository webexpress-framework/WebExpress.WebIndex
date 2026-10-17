using System.Globalization;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;
using WebExpress.WebIndex.Test.Data;

namespace WebExpress.WebIndex.Test.Queries
{
    /// <summary>
    /// Provides unit tests for the Query class.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestQuery
    {
        /// <summary>
        /// Verifies that a newly instance has the expected default property values.
        /// </summary>
        [Fact]
        public void DefaultQuery()
        {
            // act
            var q = new Query<IndexItem>();

            // validation
            Assert.Empty(q.Filters);
            Assert.Empty(q.OrderBys);
            Assert.Null(q.Skip);
            Assert.Null(q.Take);
        }

        /// <summary>
        /// Verifies that the class implements immutable behavior when applying filters.
        /// </summary>
        [Fact]
        public void Immutable()
        {
            // arrange
            var q1 = new Query<IndexItem>();

            // act
            var q2 = q1.Where(x => x.Value > 10);

            // validation
            Assert.NotSame(q1, q2);
            Assert.Empty(q1.Filters);
            Assert.Single(q2.Filters);
            Assert.NotEqual(q1.Filters, q2.Filters);
        }

        /// <summary>
        /// Verifies that the Where method adds the specified filter expression to the 
        /// query's filter collection.
        /// </summary>
        [Fact]
        public void Where()
        {
            // arrange
            Expression<Func<IndexItem, bool>> filter = x => x.Value > 5;

            // act
            var q = new Query<IndexItem>()
                .Where(filter);

            // validation
            Assert.Single(q.Filters);
            Assert.Equal(filter, q.Filters.First());
        }

        /// <summary>
        /// Verifies that the WhereEquals method adds a filter that matches items 
        /// with the specified property value.
        /// </summary>
        [Fact]
        public void WhereEquals()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Name;
            var value = "Test";

            // act
            var q = new Query<IndexItem>()
                .WhereEquals(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First();
            var compiled = filter.Compile();

            Assert.True(compiled(new IndexItem { Name = "Test" }));
            Assert.False(compiled(new IndexItem { Name = "Other" }));
        }

        /// <summary>
        /// Verifies that the WhereEqualsIgnoreCase method adds a filter that performs a 
        /// case-insensitive equality comparison on the specified property.
        /// </summary>
        [Fact]
        public void WhereEqualsIgnoreCase()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Name;
            var value = "test";

            // act
            var q = new Query<IndexItem>()
                .WhereEqualsIgnoreCase(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Name = "TEST" }));
            Assert.True(filter(new IndexItem { Name = "test" }));
            Assert.False(filter(new IndexItem { Name = "nope" }));
        }

        /// <summary>
        /// Verifies that the WhereContains method adds a filter that matches items whose 
        /// Description contains the specified value.
        /// </summary>
        [Fact]
        public void WhereContains()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Description;
            var value = "cloud";

            // act
            var q = new Query<IndexItem>()
                .WhereContains(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Description = "cloud computing" }));
            Assert.False(filter(new IndexItem { Description = "other" }));
        }

        /// <summary>
        /// Verifies that the WhereContains method adds a filter that correctly matches 
        /// items containing the specified value in the selected collection property.
        /// </summary>
        [Fact]
        public void WhereContainsEnumerable()
        {
            // arrange
            Expression<Func<IndexItem, IEnumerable<string>>> selector =
                x => x.Tags;
            var value = "cloud";

            var item1 = new IndexItem { Tags = ["cloud", "api"] };
            var item2 = new IndexItem { Tags = ["other", "stuff"] };

            // act
            var q = new Query<IndexItem>()
                .WhereContains(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(item1));
            Assert.False(filter(item2));
        }

        /// <summary>
        /// Verifies that the WhereContainsIgnoreCase method adds a filter that performs a 
        /// case-insensitive containment check on the specified property.
        /// </summary>
        [Fact]
        public void WhereContainsIgnoreCase()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Description;
            var value = "cloud";

            // act
            var q = new Query<IndexItem>()
                .WhereContainsIgnoreCase(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Description = "CLOUD services" }));
            Assert.True(filter(new IndexItem { Description = "cloud services" }));
            Assert.False(filter(new IndexItem { Description = "other" }));
        }

        /// <summary>
        /// Verifies that the WhereContainsIgnoreCase method adds a filter that matches 
        /// items whose tag collection contains the specified value, ignoring case.
        /// </summary>
        [Fact]
        public void WhereContainsIgnoreCaseEnumerable()
        {
            // arrange
            Expression<Func<IndexItem, IEnumerable<string>>> selector =
                x => x.Tags;
            var value = "cloud";

            var item1 = new IndexItem { Tags = ["CLOUD", "api"] };
            var item2 = new IndexItem { Tags = ["cloud", "services"] };
            var item3 = new IndexItem { Tags = ["other", "stuff"] };

            var list = new[] { item1, item2, item3 };

            // act
            var q = new Query<IndexItem>()
                .WhereContainsIgnoreCase(selector, value);

            var q1 = new Query<IndexItem>()
                .WhereContainsIgnoreCase(x => x.Tags.Select(x => $"_{x}_"), value);

            var res = q1.Apply(list.AsQueryable()).ToList();

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(item1));   // uppercase
            Assert.True(filter(item2));   // lowercase
            Assert.False(filter(item3));  // no match

            Assert.Equal(2, res.Count);
        }

        /// <summary>
        /// Verifies that the WhereStartsWith method adds a filter that matches items 
        /// whose Path property starts with the specified value.
        /// </summary>
        [Fact]
        public void WhereStartsWith()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Path;
            var value = "/api";

            // act
            var q = new Query<IndexItem>()
                .WhereStartsWith(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Path = "/api/workspaces" }));
            Assert.False(filter(new IndexItem { Path = "/web" }));
        }

        /// <summary>
        /// Verifies that the WhereStartsWithIgnoreCase method adds a filter that matches 
        /// string values starting with the specified prefix, ignoring case.
        /// </summary>
        [Fact]
        public void WhereStartsWithIgnoreCase()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Path;
            var value = "/api";

            // act
            var q = new Query<IndexItem>()
                .WhereStartsWithIgnoreCase(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Path = "/API/workspaces" }));
            Assert.True(filter(new IndexItem { Path = "/api/workspaces" }));
            Assert.False(filter(new IndexItem { Path = "/web" }));
        }

        /// <summary>
        /// Verifies that the WhereEndsWith method adds a filter that matches items 
        /// whose Email property ends with the specified value.
        /// </summary>
        [Fact]
        public void WhereEndsWith()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Email;
            var value = "@domain.com";

            // act
            var q = new Query<IndexItem>()
                .WhereEndsWith(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Email = "user@domain.com" }));
            Assert.False(filter(new IndexItem { Email = "user@other.com" }));
        }

        /// <summary>
        /// Verifies that the WhereEndsWithIgnoreCase method adds a filter that matches 
        /// strings ending with the specified value, using a case-insensitive comparison.
        /// </summary>
        [Fact]
        public void WhereEndsWithIgnoreCase()
        {
            // arrange
            Expression<Func<IndexItem, string>> selector = x => x.Email;
            var value = "@domain.com";

            // act
            var q = new Query<IndexItem>()
                .WhereEndsWithIgnoreCase(selector, value);

            // validation
            Assert.Single(q.Filters);

            var filter = q.Filters.First().Compile();

            Assert.True(filter(new IndexItem { Email = "USER@DOMAIN.COM" }));
            Assert.True(filter(new IndexItem { Email = "user@domain.com" }));
            Assert.False(filter(new IndexItem { Email = "user@other.com" }));
        }

        /// <summary>
        /// Verifies that the `And` method correctly combines filters using a logical AND operation.
        /// </summary>
        [Fact]
        public void And()
        {
            // arrange
            Expression<Func<IndexItem, bool>> filter1 = x => x.IsActive;
            Expression<Func<IndexItem, bool>> filter2 = x => x.Name == "Test";

            // act
            var query = new Query<IndexItem>()
                .And(filter1)
                .And(filter2);

            // validation
            Assert.Single(query.Filters);

            var combinedFilter = query.Filters.First().Compile();

            Assert.True(combinedFilter(new IndexItem { IsActive = true, Name = "Test" }));
            Assert.False(combinedFilter(new IndexItem { IsActive = false, Name = "Test" }));
            Assert.False(combinedFilter(new IndexItem { IsActive = true, Name = "Nope" }));
        }

        /// <summary>
        /// Verifies that the `Or` method correctly combines filters using a logical OR operation.
        /// </summary>
        [Fact]
        public void Or()
        {
            // arrange
            Expression<Func<IndexItem, bool>> filter1 = x => x.IsActive;
            Expression<Func<IndexItem, bool>> filter2 = x => x.Name == "Test";

            // act
            var query = new Query<IndexItem>()
                .Or(filter1)
                .Or(filter2);

            // validation
            Assert.Single(query.Filters);

            var combinedFilter = query.Filters.First().Compile();

            Assert.True(combinedFilter(new IndexItem { IsActive = true, Name = "Nope" }));
            Assert.True(combinedFilter(new IndexItem { IsActive = false, Name = "Test" }));
            Assert.False(combinedFilter(new IndexItem { IsActive = false, Name = "Nope" }));
        }

        /// <summary>
        /// Verifies that calling OrderByAsc on a Query<IndexItem> correctly sets the 
        /// OrderBy property to the specified key expression and leaves OrderByDescending unset.
        /// </summary>
        [Fact]
        public void OrderByAsc()
        {
            // arrange
            Expression<Func<IndexItem, object>> key = x => x.Name;

            // act
            var q = new Query<IndexItem>()
                .OrderByAsc(key);

            // validation
            Assert.Equal(key, q.OrderBys.First().KeySelector);
            Assert.False(q.OrderBys.First().Descending);
            Assert.Single(q.OrderBys);
        }

        /// <summary>
        /// Verifies that calling OrderByDesc on a Query sets the OrderByDescending 
        /// property and clears the OrderBy property.
        /// </summary>
        [Fact]
        public void OrderByDesc()
        {
            // arrange
            Expression<Func<IndexItem, object>> key = x => x.Value;

            // act
            var q = new Query<IndexItem>()
                .OrderByDesc(key);

            // validation
            Assert.Equal(key, q.OrderBys.FirstOrDefault().KeySelector);
            Assert.True(q.OrderBys.FirstOrDefault().Descending);
        }

        /// <summary>
        /// Verifies that calling ThenByAsc on a Query<IndexItem> correctly sets the ThenBy 
        /// property and leaves ThenByDescending unset.
        /// </summary>
        [Fact]
        public void ThenByAsc()
        {
            // arrange
            Expression<Func<IndexItem, object>> key = x => x.Value;

            // act
            var q = new Query<IndexItem>()
                .ThenByAsc(key);

            // validation
            Assert.Equal(key, q.OrderBys.Last().KeySelector);
            Assert.False(q.OrderBys.Last().Descending);
        }

        /// <summary>
        /// Verifies that calling ThenByDesc sets the ThenByDescending property and clears the 
        /// ThenBy property on a Query<IndexItem> instance.
        /// </summary>
        [Fact]
        public void ThenByDesc()
        {
            // arrange
            Expression<Func<IndexItem, object>> key = x => x.Name;

            // act
            var q = new Query<IndexItem>()
                .ThenByDesc(key);

            // validation
            Assert.Equal(key, q.OrderBys.Last().KeySelector);
            Assert.True(q.OrderBys.Last().Descending);
        }

        /// <summary>
        /// Verifies that the WithPaging method correctly sets the Skip and Take properties 
        /// on a Query instance.
        /// </summary>
        [Fact]
        public void WithPaging()
        {
            // act
            var q = new Query<IndexItem>()
                .WithPaging(10, 20);

            // validation
            Assert.Equal(10, q.Skip);
            Assert.Equal(20, q.Take);
        }

        /// <summary>
        /// Verifies that the WithPaging method throws an ArgumentOutOfRangeException when provided with invalid paging
        /// parameters.
        /// </summary>
        [Fact]
        public void WithPagingInvalidValues()
        {
            // act & validation
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Query<IndexItem>().WithPaging(-1, 10));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Query<IndexItem>().WithPaging(0, 0));
        }

        /// <summary>
        /// Executes the test to verify that the 'Apply' method of the 'Query<IndexItem>' 
        /// class correctly filters, orders, and pages the provided data source.
        /// </summary>
        [Fact]
        public void Apply()
        {
            // arrange
            var data = new List<IndexItem>
            {
                new() { Name = "Charlie", Value = 30 },
                new() { Name = "Alpha",   Value = 10 },
                new() { Name = "Bravo",   Value = 20 }
            }.AsQueryable();

            var query = new Query<IndexItem>()
                .Where(x => x.Value >= 20)
                .OrderByAsc(x => x.Name)
                .WithPaging(0, 1);

            // act
            var result = query.Apply(data).ToList();

            // validation
            Assert.Single(result);
            Assert.Equal("Bravo", result[0].Name);
        }

        /// <summary>
        /// Verifies that the filters of a query are kept as a conjunction when it is combined
        /// using OR, so that a + b or c means (a and b) or c and not a or b or c.
        /// </summary>
        [Fact]
        public void OrKeepsConjunction()
        {
            // act
            var query = new Query<IndexItem>()
                .Where(x => x.IsActive)
                .Where(x => x.Value > 10)
                .Or(x => x.Name == "c");

            // validation
            Assert.True(Matches(query, new IndexItem { IsActive = true, Value = 20 }));
            Assert.True(Matches(query, new IndexItem { Name = "c" }));
            Assert.False(Matches(query, new IndexItem { IsActive = true, Value = 5 }));
            Assert.False(Matches(query, new IndexItem { IsActive = false, Value = 20 }));
        }

        /// <summary>
        /// Verifies that a foreign query with several filters joins as one conjunction, for
        /// both AND and OR.
        /// </summary>
        [Fact]
        public void CombineWithMultipartQuery()
        {
            // arrange
            var other = new Query<IndexItem>()
                .Where(x => x.Value > 10)
                .Where(x => x.Name == "b");

            // act
            var or = new Query<IndexItem>().Where(x => x.IsActive).Or(other);
            var and = new Query<IndexItem>().Where(x => x.IsActive).And(other);

            // validation
            Assert.True(Matches(or, new IndexItem { IsActive = true }));
            Assert.True(Matches(or, new IndexItem { Value = 20, Name = "b" }));
            Assert.False(Matches(or, new IndexItem { Value = 20, Name = "a" }));
            Assert.False(Matches(or, new IndexItem { Value = 5, Name = "b" }));

            Assert.True(Matches(and, new IndexItem { IsActive = true, Value = 20, Name = "b" }));
            Assert.False(Matches(and, new IndexItem { IsActive = true, Value = 20, Name = "a" }));
            Assert.False(Matches(and, new IndexItem { IsActive = false, Value = 20, Name = "b" }));
        }

        /// <summary>
        /// Verifies that an empty side carries no condition and is ignored, for every
        /// combination of empty left, right and both sides.
        /// </summary>
        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        public void CombineWithEmptySide(bool useAnd, bool leftEmpty, bool rightEmpty)
        {
            // arrange
            var data = Enumerable.Range(0, 20)
                .Select(i => new IndexItem { Value = i, IsActive = i % 2 == 0 })
                .ToList();
            Func<IndexItem, bool> left = x => x.Value > 10;
            Func<IndexItem, bool> right = x => x.IsActive;

            var leftQuery = leftEmpty ? new Query<IndexItem>() : new Query<IndexItem>().Where(x => x.Value > 10);
            var rightQuery = rightEmpty ? new Query<IndexItem>() : new Query<IndexItem>().Where(x => x.IsActive);

            // act
            var combined = useAnd ? leftQuery.And(rightQuery) : leftQuery.Or(rightQuery);
            var result = combined.Apply(data.AsQueryable()).ToList();

            // validation
            var expected = (leftEmpty, rightEmpty) switch
            {
                (true, true) => data,
                (true, false) => data.Where(right).ToList(),
                (false, true) => data.Where(left).ToList(),
                _ => throw new InvalidOperationException()
            };

            Assert.Equal(leftEmpty && rightEmpty ? 0 : 1, combined.Filters.Count());
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that combined filters are rebound to a single parameter instead of being
        /// wrapped in invocations, which query providers often cannot translate.
        /// </summary>
        [Fact]
        public void CombineWithoutInvoke()
        {
            // arrange
            var other = new Query<IndexItem>()
                .Where(y => y.Value > 10)
                .Where(z => z.Name == "b");

            // act
            var query = new Query<IndexItem>()
                .Where(x => x.IsActive)
                .WhereContains(x => x.Tags, "a")
                .Or(other)
                .And(w => w.Value < 100)
                .Not();

            var inspector = new ExpressionInspector();
            inspector.Visit(query.Filters.Single());

            // validation
            Assert.Equal(0, inspector.Invocations);
            Assert.Single(inspector.Parameters.Where(x => x.Type == typeof(IndexItem)).Distinct());
        }

        /// <summary>
        /// Verifies that one-sided paging and a limit of zero can be expressed and that
        /// negative values are rejected.
        /// </summary>
        [Fact]
        public void WithSkipAndWithTake()
        {
            // arrange
            var data = Enumerable.Range(0, 10).Select(i => new IndexItem { Value = i }).AsQueryable();

            // act
            var skipOnly = new Query<IndexItem>().WithSkip(7);
            var takeOnly = new Query<IndexItem>().WithTake(3);
            var takeNone = new Query<IndexItem>().WithTake(0);
            var both = new Query<IndexItem>().WithTake(2).WithSkip(4);

            // validation
            Assert.Equal(7, skipOnly.Skip);
            Assert.Null(skipOnly.Take);
            Assert.Equal(new[] { 7, 8, 9 }, skipOnly.Apply(data).Select(x => x.Value));
            Assert.Null(takeOnly.Skip);
            Assert.Equal(new[] { 0, 1, 2 }, takeOnly.Apply(data).Select(x => x.Value));
            Assert.Empty(takeNone.Apply(data));
            Assert.Equal(new[] { 4, 5 }, both.Apply(data).Select(x => x.Value));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Query<IndexItem>().WithSkip(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Query<IndexItem>().WithTake(-1));
        }

        /// <summary>
        /// Verifies that the string filters reject an item with a null property instead of
        /// throwing, and that equality with null matches exactly such items.
        /// </summary>
        [Fact]
        public void StringFiltersWithNullProperty()
        {
            // arrange
            var item = new IndexItem { Name = null };
            var query = new Query<IndexItem>();

            // validation
            Assert.False(Matches(query.WhereEquals(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereEqualsIgnoreCase(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereContains(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereContainsIgnoreCase(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereStartsWith(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereStartsWithIgnoreCase(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereEndsWith(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereEndsWithIgnoreCase(x => x.Name, "a"), item));
            Assert.False(Matches(query.WhereContains(x => x.Name, ""), item));
            Assert.True(Matches(query.WhereEquals(x => x.Name, null), item));
            Assert.False(Matches(query.WhereEquals(x => x.Name, null), new IndexItem { Name = "a" }));
        }

        /// <summary>
        /// Verifies that the collection filters treat a missing collection as empty and skip
        /// null elements.
        /// </summary>
        [Fact]
        public void CollectionFiltersWithNullValues()
        {
            // arrange
            var query = new Query<IndexItem>();
            var missing = new IndexItem { Tags = null };
            var withNull = new IndexItem { Tags = [null, "Cloud"] };
            var onlyNull = new IndexItem { Tags = [null] };

            // validation
            foreach (var filter in new[]
            {
                query.WhereContains(x => x.Tags, "Cl"),
                query.WhereContainsIgnoreCase(x => x.Tags, "cl"),
                query.WhereElementEquals(x => x.Tags, "Cloud"),
                query.WhereElementEqualsIgnoreCase(x => x.Tags, "cloud")
            })
            {
                Assert.False(Matches(filter, missing));
                Assert.True(Matches(filter, withNull));
                Assert.False(Matches(filter, onlyNull));
            }
        }

        /// <summary>
        /// Verifies that invalid arguments are rejected when the query is built and not
        /// first when it is executed.
        /// </summary>
        [Fact]
        public void ArgumentValidationAtBuildTime()
        {
            // arrange
            var query = new Query<IndexItem>();

            // validation
            Assert.Throws<ArgumentNullException>(() => query.WhereEquals((Expression<Func<IndexItem, string>>)null, "a"));
            Assert.Throws<ArgumentNullException>(() => query.WhereEquals((Expression<Func<IndexItem, Guid>>)null, Guid.Empty));
            Assert.Throws<ArgumentNullException>(() => query.WhereEqualsIgnoreCase(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereContains(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereContains((Expression<Func<IndexItem, string>>)null, "a"));
            Assert.Throws<ArgumentNullException>(() => query.WhereContainsIgnoreCase(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereStartsWith(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereStartsWithIgnoreCase(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereEndsWith(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereEndsWithIgnoreCase(x => x.Name, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereContains(x => x.Tags, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereElementEquals(x => x.Tags, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereIn(x => x.Value, null));
            Assert.Throws<ArgumentNullException>(() => query.WhereGreaterThan<int>(null, 1));
            Assert.Throws<ArgumentNullException>(() => query.WhereIsNullOrEmpty(null));
            Assert.Throws<ArgumentNullException>(() => query.WhereNot(null));
            Assert.Throws<ArgumentNullException>(() => query.Where(null));
            Assert.Throws<ArgumentException>(() => query.Where(x => x.IsActive, null));
        }

        /// <summary>
        /// Verifies that a case-insensitive filter built under one culture still matches when
        /// it is executed under another. The Turkish culture lowercases an uppercase I to a
        /// dotless i, which a culture-sensitive comparison would no longer match.
        /// </summary>
        [Fact]
        public void IgnoreCaseIsCultureInvariant()
        {
            // arrange
            var culture = CultureInfo.CurrentCulture;
            var item = new IndexItem { Name = "TITLE", Tags = ["INFO"] };

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                var equals = new Query<IndexItem>().WhereEqualsIgnoreCase(x => x.Name, "title");
                var contains = new Query<IndexItem>().WhereContainsIgnoreCase(x => x.Name, "itl");
                var element = new Query<IndexItem>().WhereElementEqualsIgnoreCase(x => x.Tags, "info");

                // act
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                // validation
                Assert.True(Matches(equals, item));
                Assert.True(Matches(contains, item));
                Assert.True(Matches(element, item));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        /// <summary>
        /// Verifies that the filter and sort lists cannot be changed by casting the exposed
        /// collections back to a mutable type.
        /// </summary>
        [Fact]
        public void FiltersAreReadOnly()
        {
            // arrange
            var query = new Query<IndexItem>()
                .Where(x => x.IsActive)
                .OrderByAsc(x => x.Name);

            // act & validation
            Assert.Null(query.Filters as List<Expression<Func<IndexItem, bool>>>);
            Assert.Throws<NotSupportedException>(() => ((ICollection<Expression<Func<IndexItem, bool>>>)query.Filters).Clear());
            Assert.Throws<NotSupportedException>(() => ((ICollection<(Expression<Func<IndexItem, object>>, bool)>)query.OrderBys).Clear());
            Assert.Single(query.Filters);
            Assert.Single(query.OrderBys);
            Assert.Same(query.Filters, query.Filters);
        }

        /// <summary>
        /// Verifies that OrderBy replaces an existing sort order while ThenBy appends to it.
        /// </summary>
        [Fact]
        public void OrderByReplacesThenByAppends()
        {
            // arrange
            Expression<Func<IndexItem, object>> name = x => x.Name;
            Expression<Func<IndexItem, object>> value = x => x.Value;
            Expression<Func<IndexItem, object>> path = x => x.Path;
            Expression<Func<IndexItem, object>> email = x => x.Email;
            var data = new List<IndexItem>
            {
                new() { Path = "b", Email = "2", Name = "a" },
                new() { Path = "a", Email = "1", Name = "b" },
                new() { Path = "b", Email = "1", Name = "c" }
            }.AsQueryable();

            // act
            var query = new Query<IndexItem>()
                .OrderByAsc(name)
                .ThenByDesc(value)
                .OrderByDesc(path)
                .ThenByAsc(email);

            // validation
            Assert.Equal(new[] { (path, true), (email, false) }, query.OrderBys);
            Assert.Equal(new[] { "c", "a", "b" }, query.Apply(data).Select(x => x.Name));
        }

        /// <summary>
        /// Verifies that Not negates the conjunction of all filters and leaves a query without
        /// filters unchanged.
        /// </summary>
        [Fact]
        public void Not()
        {
            // act
            var query = new Query<IndexItem>()
                .Where(x => x.IsActive)
                .Where(x => x.Value > 10)
                .Not();
            var empty = new Query<IndexItem>().Not();

            // validation
            Assert.True(Matches(query, new IndexItem { IsActive = true, Value = 5 }));
            Assert.False(Matches(query, new IndexItem { IsActive = true, Value = 20 }));
            Assert.Empty(empty.Filters);
            Assert.True(Matches(empty, new IndexItem()));
        }

        /// <summary>
        /// Verifies that WhereNot adds the negation of a predicate to the existing filters.
        /// </summary>
        [Fact]
        public void WhereNot()
        {
            // act
            var query = new Query<IndexItem>()
                .Where(x => x.Value > 10)
                .WhereNot(x => x.IsActive);

            // validation
            Assert.True(Matches(query, new IndexItem { IsActive = false, Value = 20 }));
            Assert.False(Matches(query, new IndexItem { IsActive = true, Value = 20 }));
            Assert.False(Matches(query, new IndexItem { IsActive = false, Value = 5 }));
        }

        /// <summary>
        /// Verifies that WhereNotEquals on a string property matches differing values,
        /// including null properties, and that a null value matches non-null properties.
        /// </summary>
        [Fact]
        public void WhereNotEqualsString()
        {
            // act
            var query = new Query<IndexItem>().WhereNotEquals(x => x.Name, "Test");
            var notNull = new Query<IndexItem>().WhereNotEquals(x => x.Name, null);

            // validation
            Assert.True(Matches(query, new IndexItem { Name = "Other" }));
            Assert.False(Matches(query, new IndexItem { Name = "Test" }));
            Assert.True(Matches(query, new IndexItem { Name = "test" }));
            Assert.True(Matches(query, new IndexItem { Name = null }));
            Assert.True(Matches(notNull, new IndexItem { Name = "" }));
            Assert.False(Matches(notNull, new IndexItem { Name = null }));
        }

        /// <summary>
        /// Verifies that WhereEquals and WhereNotEquals on a Guid property are complementary.
        /// </summary>
        [Fact]
        public void WhereNotEqualsGuid()
        {
            // arrange
            var item = new IndexItem();

            // act
            var equals = new Query<IndexItem>().WhereEquals(x => x.Id, item.Id);
            var notEquals = new Query<IndexItem>().WhereNotEquals(x => x.Id, item.Id);
            var notEmpty = new Query<IndexItem>().WhereNotEquals(x => x.Id, Guid.Empty);

            // validation
            Assert.True(Matches(equals, item));
            Assert.False(Matches(notEquals, item));
            Assert.True(Matches(notEquals, new IndexItem()));
            Assert.True(Matches(notEmpty, item));
        }

        /// <summary>
        /// Verifies that WhereIn matches the given values, works on a copy taken when the
        /// filter is built and matches nothing for an empty set.
        /// </summary>
        [Fact]
        public void WhereIn()
        {
            // arrange
            var values = new List<string> { "a", "b" };

            // act
            var query = new Query<IndexItem>().WhereIn(x => x.Name, values);
            var none = new Query<IndexItem>().WhereIn(x => x.Value, []);
            values.Add("c");

            // validation
            Assert.True(Matches(query, new IndexItem { Name = "b" }));
            Assert.False(Matches(query, new IndexItem { Name = "c" }));
            Assert.False(Matches(query, new IndexItem { Name = null }));
            Assert.False(Matches(none, new IndexItem { Value = 0 }));
        }

        /// <summary>
        /// Verifies WhereGreaterThan, including a type without comparison operators.
        /// </summary>
        [Fact]
        public void WhereGreaterThan()
        {
            // act
            var query = new Query<IndexItem>().WhereGreaterThan(x => x.Value, 10);
            var flag = new Query<IndexItem>().WhereGreaterThan(x => x.IsActive, false);

            // validation
            Assert.True(Matches(query, new IndexItem { Value = 11 }));
            Assert.False(Matches(query, new IndexItem { Value = 10 }));
            Assert.False(Matches(query, new IndexItem { Value = 9 }));
            Assert.True(Matches(flag, new IndexItem { IsActive = true }));
            Assert.False(Matches(flag, new IndexItem { IsActive = false }));
        }

        /// <summary>
        /// Verifies WhereGreaterThanOrEqual, including a type without comparison operators.
        /// </summary>
        [Fact]
        public void WhereGreaterThanOrEqual()
        {
            // act
            var query = new Query<IndexItem>().WhereGreaterThanOrEqual(x => x.Value, 10);
            var letter = new Query<IndexItem>().WhereGreaterThanOrEqual(x => x.Name[0], 'm');

            // validation
            Assert.True(Matches(query, new IndexItem { Value = 11 }));
            Assert.True(Matches(query, new IndexItem { Value = 10 }));
            Assert.False(Matches(query, new IndexItem { Value = 9 }));
            Assert.True(Matches(letter, new IndexItem { Name = "m" }));
            Assert.False(Matches(letter, new IndexItem { Name = "a" }));
        }

        /// <summary>
        /// Verifies WhereLessThan, including the boundary and a non-numeric type.
        /// </summary>
        [Fact]
        public void WhereLessThan()
        {
            // arrange
            var bound = new Guid("50000000-0000-0000-0000-000000000000");

            // act
            var query = new Query<IndexItem>().WhereLessThan(x => x.Value, 10);
            var reference = new Query<IndexItem>().WhereLessThan(x => new Guid(x.Name), bound);

            // validation
            Assert.True(Matches(query, new IndexItem { Value = 9 }));
            Assert.False(Matches(query, new IndexItem { Value = 10 }));
            Assert.True(Matches(query, new IndexItem { Value = int.MinValue }));
            Assert.True(Matches(reference, new IndexItem { Name = "10000000-0000-0000-0000-000000000000" }));
            Assert.False(Matches(reference, new IndexItem { Name = "60000000-0000-0000-0000-000000000000" }));
        }

        /// <summary>
        /// Verifies WhereLessThanOrEqual, including the boundary.
        /// </summary>
        [Fact]
        public void WhereLessThanOrEqual()
        {
            // act
            var query = new Query<IndexItem>().WhereLessThanOrEqual(x => x.Value, 10);
            var flag = new Query<IndexItem>().WhereLessThanOrEqual(x => x.IsActive, false);

            // validation
            Assert.True(Matches(query, new IndexItem { Value = 10 }));
            Assert.True(Matches(query, new IndexItem { Value = 9 }));
            Assert.False(Matches(query, new IndexItem { Value = 11 }));
            Assert.True(Matches(flag, new IndexItem { IsActive = false }));
            Assert.False(Matches(flag, new IndexItem { IsActive = true }));
        }

        /// <summary>
        /// Verifies that WhereBetween includes both bounds, excludes values outside and
        /// rejects an inverted range.
        /// </summary>
        [Fact]
        public void WhereBetween()
        {
            // act
            var query = new Query<IndexItem>().WhereBetween(x => x.Value, 10, 20);
            var point = new Query<IndexItem>().WhereBetween(x => x.Value, 5, 5);

            // validation
            Assert.True(Matches(query, new IndexItem { Value = 10 }));
            Assert.True(Matches(query, new IndexItem { Value = 20 }));
            Assert.False(Matches(query, new IndexItem { Value = 9 }));
            Assert.False(Matches(query, new IndexItem { Value = 21 }));
            Assert.True(Matches(point, new IndexItem { Value = 5 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Query<IndexItem>().WhereBetween(x => x.Value, 20, 10));
        }

        /// <summary>
        /// Verifies that WhereElementEquals matches whole elements only, unlike WhereContains.
        /// </summary>
        [Fact]
        public void WhereElementEquals()
        {
            // act
            var query = new Query<IndexItem>().WhereElementEquals(x => x.Tags, "cloud");

            // validation
            Assert.True(Matches(query, new IndexItem { Tags = ["api", "cloud"] }));
            Assert.False(Matches(query, new IndexItem { Tags = ["cloudy"] }));
            Assert.False(Matches(query, new IndexItem { Tags = ["Cloud"] }));
            Assert.False(Matches(query, new IndexItem { Tags = [] }));
        }

        /// <summary>
        /// Verifies that WhereElementEqualsIgnoreCase matches whole elements regardless of case.
        /// </summary>
        [Fact]
        public void WhereElementEqualsIgnoreCase()
        {
            // act
            var query = new Query<IndexItem>().WhereElementEqualsIgnoreCase(x => x.Tags, "cloud");

            // validation
            Assert.True(Matches(query, new IndexItem { Tags = ["CLOUD"] }));
            Assert.False(Matches(query, new IndexItem { Tags = ["CLOUDY"] }));
            Assert.False(Matches(query, new IndexItem { Tags = [""] }));
        }

        /// <summary>
        /// Verifies that WhereIsNullOrEmpty matches null and empty values but not whitespace.
        /// </summary>
        [Fact]
        public void WhereIsNullOrEmpty()
        {
            // act
            var query = new Query<IndexItem>().WhereIsNullOrEmpty(x => x.Name);

            // validation
            Assert.True(Matches(query, new IndexItem { Name = null }));
            Assert.True(Matches(query, new IndexItem { Name = "" }));
            Assert.False(Matches(query, new IndexItem { Name = " " }));
            Assert.False(Matches(query, new IndexItem { Name = "a" }));
        }

        /// <summary>
        /// Verifies that WhereIsNotNullOrEmpty is the complement of WhereIsNullOrEmpty.
        /// </summary>
        [Fact]
        public void WhereIsNotNullOrEmpty()
        {
            // act
            var query = new Query<IndexItem>().WhereIsNotNullOrEmpty(x => x.Name);

            // validation
            Assert.True(Matches(query, new IndexItem { Name = "a" }));
            Assert.True(Matches(query, new IndexItem { Name = " " }));
            Assert.False(Matches(query, new IndexItem { Name = "" }));
            Assert.False(Matches(query, new IndexItem { Name = null }));
        }

        /// <summary>
        /// Runs the query against a single item.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="item">The item.</param>
        /// <returns>True if the query returns the item, false otherwise.</returns>
        private static bool Matches(IQuery<IndexItem> query, IndexItem item)
        {
            return query.Apply(new[] { item }.AsQueryable()).Any();
        }

        /// <summary>
        /// Records the invocations and parameters of an expression tree.
        /// </summary>
        private sealed class ExpressionInspector : ExpressionVisitor
        {
            /// <summary>
            /// Returns the number of invocation nodes.
            /// </summary>
            public int Invocations { get; private set; }

            /// <summary>
            /// Returns every parameter that occurs in the tree.
            /// </summary>
            public List<ParameterExpression> Parameters { get; } = [];

            /// <summary>
            /// Counts an invocation node.
            /// </summary>
            /// <param name="node">The visited node.</param>
            /// <returns>The node.</returns>
            protected override Expression VisitInvocation(InvocationExpression node)
            {
                Invocations++;

                return base.VisitInvocation(node);
            }

            /// <summary>
            /// Records a parameter.
            /// </summary>
            /// <param name="node">The visited parameter.</param>
            /// <returns>The parameter.</returns>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                Parameters.Add(node);

                return base.VisitParameter(node);
            }
        }
    }
}
