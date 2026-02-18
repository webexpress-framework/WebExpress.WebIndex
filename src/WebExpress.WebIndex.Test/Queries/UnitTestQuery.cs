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
            Assert.Null(q.OrderBy);
            Assert.Null(q.OrderByDescending);
            Assert.Null(q.ThenBy);
            Assert.Null(q.ThenByDescending);
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
            Assert.Equal(key, q.OrderBy);
            Assert.Null(q.OrderByDescending);
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
            Assert.Equal(key, q.OrderByDescending);
            Assert.Null(q.OrderBy);
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
            Assert.Equal(key, q.ThenBy);
            Assert.Null(q.ThenByDescending);
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
            Assert.Equal(key, q.ThenByDescending);
            Assert.Null(q.ThenBy);
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
                new IndexItem { Name = "Charlie", Value = 30 },
                new IndexItem { Name = "Alpha",   Value = 10 },
                new IndexItem { Name = "Bravo",   Value = 20 }
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

    }
}
