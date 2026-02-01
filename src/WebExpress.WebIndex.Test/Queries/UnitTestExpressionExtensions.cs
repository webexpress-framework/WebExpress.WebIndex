using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;
using WebExpress.WebIndex.Test.Data;

namespace WebExpress.WebIndex.Test.Queries
{
    /// <summary>
    /// Provides unit tests for the ExpressionExtensions class that compose and manipulate
    /// predicate expressions.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestExpressionExtensions
    {
        /// <summary>
        /// Verifies that the True<T> method returns a predicate that always evaluates to true
        /// for any input.
        /// </summary>
        [Fact]
        public void True()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "A", Value = 1, IsActive = true },
                new IndexItem { Name = "B", Value = 2, IsActive = false }
            }.AsQueryable();

            // act
            var pred = ExpressionExtensions.True<IndexItem>();
            var compiled = pred.Compile();

            // validation
            Assert.All(items, item => Assert.True(compiled(item)));
            Assert.Equal(2, items.Where(pred).Count());
        }

        /// <summary>
        /// Verifies that the False method returns a predicate expression that always evaluates 
        /// to false for any input.
        /// </summary>
        [Fact]
        public void False()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "A", Value = 1, IsActive = true },
                new IndexItem { Name = "B", Value = 2, IsActive = false }
            }.AsQueryable();

            // act
            var pred = ExpressionExtensions.False<IndexItem>();
            var compiled = pred.Compile();

            // validation
            Assert.All(items, item => Assert.False(compiled(item)));
            Assert.Empty(items.Where(pred));
        }

        /// <summary>
        /// Verifies that the Not extension method correctly negates a predicate expression.
        /// </summary>
        [Fact]
        public void Not()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "A", Value = 1, IsActive = true },
                new IndexItem { Name = "B", Value = 2, IsActive = false }
            }.AsQueryable();

            // act
            Expression<Func<IndexItem, bool>> pred = x => x.IsActive;
            var negated = pred.Not();
            var compiled = negated.Compile();

            // validation
            Assert.True(compiled(items.First(i => !i.IsActive)));
            Assert.False(compiled(items.First(i => i.IsActive)));
        }

        /// <summary>
        /// Verifies that the And extension method correctly composes two predicate 
        /// expressions using a logical AND operation.
        /// </summary>
        [Fact]
        public void And()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "Ax", Value = 10, IsActive = true },
                new IndexItem { Name = "Bx", Value = 10, IsActive = false },
                new IndexItem { Name = "Ay", Value = 20, IsActive = true }
            }.AsQueryable();

            // act
            Expression<Func<IndexItem, bool>> isActive = x => x.IsActive;
            Expression<Func<IndexItem, bool>> valueTen = x => x.Value == 10;
            var andPred = isActive.And(valueTen);

            // validation
            Assert.Single(items.Where(andPred));
            Assert.Equal("Ax", items.Where(andPred).First().Name);
        }

        /// <summary>
        /// Verifies that the Or extension method correctly composes two predicate 
        /// expressions using a logical OR operation.
        /// </summary>
        [Fact]
        public void Or()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "Ax", Value = 10, IsActive = true },
                new IndexItem { Name = "Bx", Value = 20, IsActive = false },
                new IndexItem { Name = "Ay", Value = 30, IsActive = false }
            }.AsQueryable();

            // act
            Expression<Func<IndexItem, bool>> value10 = x => x.Value == 10;
            Expression<Func<IndexItem, bool>> value20 = x => x.Value == 20;
            var pred = value10.Or(value20);

            // validation
            var result = items.Where(pred).ToList();
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Ax");
            Assert.Contains(result, x => x.Name == "Bx");
        }

        /// <summary>
        /// Verifies that the All method returns a predicate representing the 
        /// logical AND of all provided predicates, and that it returns a predicate 
        /// that always evaluates to true when no predicates are supplied.
        /// </summary>
        [Fact]
        public void All()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "Ax", Value = 10, IsActive = true },
                new IndexItem { Name = "Bx", Value = 10, IsActive = false },
                new IndexItem { Name = "Ay", Value = 20, IsActive = true }
            }.AsQueryable();

            // act
            var pred = ExpressionExtensions.All<IndexItem>(
                x => x.Value == 10,
                x => x.IsActive);

            // validation
            var result = items.Where(pred).ToList();
            Assert.Single(result);
            Assert.Equal("Ax", result[0].Name);

            // when empty - should always return true
            var predTrue = ExpressionExtensions.All<IndexItem>();
            Assert.All(items, item => Assert.True(predTrue.Compile()(item)));
        }

        /// <summary>
        /// Verifies that the Any method returns a predicate representing the 
        /// logical OR of the provided predicates.
        /// </summary>
        [Fact]
        public void Any()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "Ax", Value = 10, IsActive = true },
                new IndexItem { Name = "Bx", Value = 20, IsActive = false },
                new IndexItem { Name = "Ay", Value = 30, IsActive = true }
            }.AsQueryable();

            // act
            var pred = ExpressionExtensions.Any<IndexItem>(
                x => x.Value == 10,
                x => x.Value == 20);

            // validation
            var result = items.Where(pred).ToList();
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Ax");
            Assert.Contains(result, x => x.Name == "Bx");

            // when empty - should always return false
            var predFalse = ExpressionExtensions.Any<IndexItem>();
            Assert.All(items, item => Assert.False(predFalse.Compile()(item)));
        }

        /// <summary>
        /// Verifies that combining two predicates using the And extension method 
        /// produces a merged predicate that filters items matching both conditions.
        /// </summary>
        [Fact]
        public void Combine()
        {
            // arrange
            var items = new[]
            {
                new IndexItem { Name = "A", Value = 7, IsActive = true },
                new IndexItem { Name = "B", Value = 8, IsActive = false },
                new IndexItem { Name = "C", Value = 8, IsActive = true }
            }.AsQueryable();

            Expression<Func<IndexItem, bool>> pred1 = x => x.Value == 8;
            Expression<Func<IndexItem, bool>> pred2 = x => x.IsActive;

            // act
            // test internal Combine via And (public)
            var combined = pred1.And(pred2);

            // validation
            var result = items.Where(combined).ToList();
            Assert.Single(result);
            Assert.Equal("C", result[0].Name);
        }
    }
}
