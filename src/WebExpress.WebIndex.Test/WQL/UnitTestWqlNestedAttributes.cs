using System.Linq.Expressions;
using WebExpress.WebIndex.Test.Document;
using WebExpress.WebIndex.Test.Fixture;
using WebExpress.WebIndex.Wql;
using Xunit;
namespace WebExpress.WebIndex.Test.WQL
{
    /// <summary>
    /// Tests for nested attribute access (dot-separated paths) in WQL.
    /// </summary>
    public class UnitTestWqlNestedAttributes(UnitTestIndexFixtureWqlB fixture, ITestOutputHelper output) : IClassFixture<UnitTestIndexFixtureWqlB>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; } = output;

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureWqlB Fixture { get; set; } = fixture;

        /// <summary>
        /// Verifies that a nested attribute query (Address.City) parses without errors.
        /// </summary>
        [Fact]
        public void ParseNestedAttribute()
        {
            // act
            var wql = Fixture.ExecuteWql("Address.City ~ 'Berlin'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that a nested attribute query (Address.Street) parses without errors.
        /// </summary>
        [Fact]
        public void ParseNestedAttributeStreet()
        {
            // act
            var wql = Fixture.ExecuteWql("Address.Street ~ 'Main'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that a nested attribute query generates correct ToQuery expression.
        /// </summary>
        [Fact]
        public void NestedAttributeToQuery()
        {
            // arrange
            var wql = Fixture.ExecuteWql("Address.Street ~ 'Main'");
            var data = Fixture.TestData;

            // act
            var query = wql?.ToQuery();
            var res = query.Apply(data.AsQueryable());

            // validation
            Assert.NotNull(res);
            Assert.True(res.All(x => x.Address != null && x.Address.Street != null && x.Address.Street.Contains("Main", System.StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Verifies that nested attribute is represented correctly in ToString().
        /// </summary>
        [Fact]
        public void NestedAttributeToString()
        {
            // act
            var wql = Fixture.ExecuteWql("Address.Street ~ 'Main'");

            // validation
            Assert.Contains("Address.Street", wql.ToString());
        }

        /// <summary>
        /// Verifies that order by nested attribute parses correctly.
        /// </summary>
        [Fact]
        public void OrderByNestedAttribute()
        {
            // act
            var wql = Fixture.ExecuteWql("name ~ 'a' order by Address.City");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
        }

        /// <summary>
        /// Verifies that order by nested attribute with direction parses correctly.
        /// </summary>
        [Fact]
        public void OrderByNestedAttributeDesc()
        {
            // act
            var wql = Fixture.ExecuteWql("name ~ 'a' order by Address.City desc");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Order);
        }

        /// <summary>
        /// Verifies that nested attribute in ToExpression returns correct MemberExpression chain.
        /// </summary>
        [Fact]
        public void NestedAttributeToExpression()
        {
            // arrange
            var attr = new WqlExpressionNodeAttribute<UnitTestIndexTestDocumentB>
            {
                Name = "Address.City"
            };
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentB), "x");

            // act
            var expr = attr.ToExpression(param);

            // validation
            Assert.NotNull(expr);
            Assert.True(expr is MemberExpression);
            Assert.Equal(typeof(string), expr.Type);
        }

        /// <summary>
        /// Verifies that single-level attribute in ToExpression still works.
        /// </summary>
        [Fact]
        public void SingleAttributeToExpression()
        {
            // arrange
            var attr = new WqlExpressionNodeAttribute<UnitTestIndexTestDocumentB>
            {
                Name = "Name"
            };
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentB), "x");

            // act
            var expr = attr.ToExpression(param);

            // validation
            Assert.NotNull(expr);
            Assert.True(expr is MemberExpression);
            Assert.Equal(typeof(string), expr.Type);
        }

        /// <summary>
        /// Verifies that invalid attribute name throws InvalidOperationException.
        /// </summary>
        [Fact]
        public void InvalidAttributeThrows()
        {
            // arrange
            var attr = new WqlExpressionNodeAttribute<UnitTestIndexTestDocumentB>
            {
                Name = "NonExistent"
            };
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentB), "x");

            // act & validation
            Assert.Throws<InvalidOperationException>(() => attr.ToExpression(param));
        }

        /// <summary>
        /// Verifies that null attribute name throws InvalidOperationException.
        /// </summary>
        [Fact]
        public void NullAttributeNameThrows()
        {
            // arrange
            var attr = new WqlExpressionNodeAttribute<UnitTestIndexTestDocumentB>
            {
                Name = null
            };
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentB), "x");

            // act & validation
            Assert.Throws<InvalidOperationException>(() => attr.ToExpression(param));
        }

        /// <summary>
        /// Verifies that an invalid nested path throws when ToExpression is called.
        /// </summary>
        [Fact]
        public void InvalidNestedPathThrowsOnExpression()
        {
            // arrange
            var attr = new WqlExpressionNodeAttribute<UnitTestIndexTestDocumentB>
            {
                Name = "Address.NonExistent"
            };
            var param = Expression.Parameter(typeof(UnitTestIndexTestDocumentB), "x");

            // act & validation
            Assert.Throws<InvalidOperationException>(() => attr.ToExpression(param));
        }

        /// <summary>
        /// Verifies that WqlPropertyPath can parse and resolve a nested path.
        /// </summary>
        [Fact]
        public void PropertyPathParse()
        {
            // act
            var path = WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse("Address.City");

            // validation
            Assert.NotNull(path);
            Assert.Equal(2, path.Segments.Count());
            Assert.Equal("Address.City", path.ToString());
        }

        /// <summary>
        /// Verifies that WqlPropertyPath can resolve a PropertyInfo for nested path.
        /// </summary>
        [Fact]
        public void PropertyPathResolve()
        {
            // arrange
            var path = WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse("Address.City");

            // act
            var prop = path.Resolve(typeof(UnitTestIndexTestDocumentB));

            // validation
            Assert.NotNull(prop);
            Assert.Equal("City", prop.Name);
            Assert.Equal(typeof(string), prop.PropertyType);
        }

        /// <summary>
        /// Verifies that WqlPropertyPath can resolve value from an instance.
        /// </summary>
        [Fact]
        public void PropertyPathResolveValue()
        {
            // arrange
            var path = WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse("Address.City");
            var doc = new UnitTestIndexTestDocumentB
            {
                Address = new UnitTestIndexTestDocumentB.AddressClass
                {
                    City = "Berlin"
                }
            };

            // act
            var value = path.ResolveValue(doc);

            // validation
            Assert.Equal("Berlin", value);
        }

        /// <summary>
        /// Verifies that WqlPropertyPath handles null intermediate object.
        /// </summary>
        [Fact]
        public void PropertyPathResolveValueNullIntermediate()
        {
            // arrange
            var path = WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse("Address.City");
            var doc = new UnitTestIndexTestDocumentB
            {
                Address = null
            };

            // act
            var value = path.ResolveValue(doc);

            // validation
            Assert.Null(value);
        }

        /// <summary>
        /// Verifies that WqlPropertyPath.Parse throws on null input.
        /// </summary>
        [Fact]
        public void PropertyPathParseNullThrows()
        {
            Assert.Throws<ArgumentException>(() => WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse(null));
        }

        /// <summary>
        /// Verifies that WqlPropertyPath.Parse throws on empty input.
        /// </summary>
        [Fact]
        public void PropertyPathParseEmptyThrows()
        {
            Assert.Throws<ArgumentException>(() => WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse(""));
        }

        /// <summary>
        /// Verifies that WqlPropertyPath.Parse throws on invalid property.
        /// </summary>
        [Fact]
        public void PropertyPathParseInvalidPropertyThrows()
        {
            Assert.Throws<ArgumentException>(() => WqlPropertyPath<UnitTestIndexTestDocumentB>.Parse("Address.NonExistent"));
        }

        /// <summary>
        /// Verifies that a nested attribute query combined with AND operator works.
        /// </summary>
        [Fact]
        public void NestedAttributeWithAnd()
        {
            // act
            var wql = Fixture.ExecuteWql("Address.City ~ 'Berlin' and name ~ 'a'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that equal operator works with nested attributes.
        /// </summary>
        [Fact]
        public void NestedAttributeEquals()
        {
            // act
            var wql = Fixture.ExecuteWql("Address.City = 'Berlin'");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
        }

        /// <summary>
        /// Verifies that nested attribute combined with order by nested attribute works.
        /// </summary>
        [Fact]
        public void NestedFilterAndNestedOrder()
        {
            // act
            var wql = Fixture.ExecuteWql("name ~ 'a' order by Address.City asc");

            // validation
            Assert.False(wql.HasErrors);
            Assert.NotNull(wql.Filter);
            Assert.NotNull(wql.Order);
        }
    }
}
