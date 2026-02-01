using System;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Queries
{
    /// <summary>
    /// Static helper class for building and combining predicate expressions efficiently.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Returns a predicate that always evaluates to true.
        /// </summary>
        public static Expression<Func<TIndexItem, bool>> True<TIndexItem>()
            where TIndexItem : IIndexItem => x => true;

        /// <summary>
        /// Returns a predicate that always evaluates to false.
        /// </summary>
        public static Expression<Func<TIndexItem, bool>> False<TIndexItem>()
            where TIndexItem : IIndexItem => x => false;

        /// <summary>
        /// Creates a predicate expression that represents the logical negation of the 
        /// specified predicate.
        /// </summary>
        /// <remarks>
        /// Use this method to invert the logic of an existing predicate expression, such as when
        /// building dynamic queries with LINQ providers that support expression trees.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="predicate">
        /// The predicate expression to negate. Cannot be null.
        /// </param>
        /// <returns>
        /// An expression that evaluates to the logical negation of the specified predicate.
        /// </returns>
        public static Expression<Func<TIndexItem, bool>> Not<TIndexItem>(this Expression<Func<TIndexItem, bool>> predicate)
            where TIndexItem : IIndexItem
        {
            var param = predicate.Parameters[0];
            var body = Expression.Not(predicate.Body);

            return Expression.Lambda<Func<TIndexItem, bool>>(body, param);
        }

        /// <summary>
        /// Combines two predicate expressions into a single expression that evaluates to true 
        /// only if both predicates are true.
        /// </summary>
        /// <remarks>
        /// Use this method to compose complex filter criteria by combining multiple predicate
        /// expressions. The resulting expression can be used in LINQ queries or other scenarios 
        /// that accept expression trees.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="first">
        /// The first predicate expression to combine. Cannot be null.
        /// </param>
        /// <param name="second">
        /// The second predicate expression to combine. Cannot be null.
        /// </param>
        /// <returns>
        /// An expression representing the logical AND of the two specified predicate expressions.
        /// </returns>
        public static Expression<Func<TIndexItem, bool>> And<TIndexItem>(this Expression<Func<TIndexItem, bool>> first, Expression<Func<TIndexItem, bool>> second)
            where TIndexItem : IIndexItem
        {
            return Combine(first, second, Expression.AndAlso);
        }

        /// <summary>
        /// Combines two predicate expressions into a single expression that evaluates to 
        /// true if either predicate is true.
        /// </summary>
        /// <remarks>
        /// Use this method to build composite filter expressions for querying collections of
        /// <typeparamref name="TIndexItem"/> objects. The resulting expression can be used 
        /// in LINQ queries to match items that satisfy either of the original predicates.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="first">
        /// The first predicate expression to combine. Cannot be null.
        /// </param>
        /// <param name="second">
        /// The second predicate expression to combine. Cannot be null.
        /// </param>
        /// <returns>
        /// An expression representing the logical OR of the two specified predicate expressions.
        /// </returns>
        public static Expression<Func<TIndexItem, bool>> Or<TIndexItem>(this Expression<Func<TIndexItem, bool>> first, Expression<Func<TIndexItem, bool>> second)
            where TIndexItem : IIndexItem
        {
            return Combine(first, second, Expression.OrElse);
        }

        /// <summary>
        /// Creates a predicate expression that evaluates to true only if all specified predicates 
        /// are satisfied for a given index item.
        /// </summary>
        /// <remarks>
        /// This method is useful for dynamically composing multiple filter conditions into a
        /// single predicate expression, such as when building queries for LINQ providers.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="predicates">
        /// An array of predicate expressions to combine. Each predicate is applied to the 
        /// index item, and all must  return true for the combined expression to return true.
        /// </param>
        /// <returns>
        /// An expression representing the logical AND of all provided predicates. If no 
        /// predicates are specified, returns an expression that always evaluates to true.
        /// </returns>
        public static Expression<Func<TIndexItem, bool>> All<TIndexItem>(params Expression<Func<TIndexItem, bool>>[] predicates)
            where TIndexItem : IIndexItem
        {
            if (predicates is null || predicates.Length == 0)
            {
                return True<TIndexItem>();
            }

            var result = predicates[0];

            for (int i = 1; i < predicates.Length; i++)
            {
                result = result.And(predicates[i]);
            }

            return result;
        }

        /// <summary>
        /// Creates a predicate expression that evaluates to true if any of the specified 
        /// predicates are satisfied for a given index item.
        /// </summary>
        /// <remarks>
        /// This method combines the provided predicates using a logical OR operation. The
        /// resulting expression can be used in LINQ queries to filter items that match any of 
        /// the specified conditions.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="predicates">
        /// An array of predicate expressions to combine. Each predicate is applied to an index 
        /// item and returns a Boolean value.
        /// </param>
        /// <returns>
        /// An expression that returns true if at least one of the specified predicates 
        /// returns true for a given index item; otherwise, false. If no predicates are provided,
        /// returns an expression that always evaluates to false.
        /// </returns>
        public static Expression<Func<TIndexItem, bool>> Any<TIndexItem>(params Expression<Func<TIndexItem, bool>>[] predicates)
            where TIndexItem : IIndexItem
        {
            if (predicates is null || predicates.Length == 0)
            {
                return False<TIndexItem>();
            }

            var result = predicates[0];

            for (int i = 1; i < predicates.Length; i++)
            {
                result = result.Or(predicates[i]);
            }

            return result;
        }

        /// <summary>
        /// Combines two predicate expressions into a single expression using the specified 
        /// merge function.
        /// </summary>
        /// <remarks>
        /// The returned expression uses a unified parameter to ensure compatibility with LINQ
        /// providers. This method is useful for dynamically building complex query predicates 
        /// by combining simpler expressions.
        /// </remarks>
        /// <typeparam name="TIndexItem">
        /// The type of the parameter in the predicate expression.
        /// </typeparam>
        /// <param name="first">
        /// The first predicate expression to combine. Cannot be null.
        /// </param>
        /// <param name="second">
        /// The second predicate expression to combine. Cannot be null.
        /// </param>
        /// <param name="merge">
        /// A function that merges the bodies of the two expressions into a single binary 
        /// expression. Typically represents a logical operation such as AND or OR. Cannot 
        /// be null.
        /// </param>
        /// <returns>
        /// An expression representing the combination of the two input predicates using 
        /// the specified merge function.
        /// </returns>
        private static Expression<Func<TIndexItem, bool>> Combine<TIndexItem>
        (
            Expression<Func<TIndexItem, bool>> first,
            Expression<Func<TIndexItem, bool>> second,
            Func<Expression, Expression, BinaryExpression> merge
        )
            where TIndexItem : IIndexItem
        {
            // unify both parameters to one parameter (needed for LINQ providers)
            var parameter = first.Parameters[0];
            var secondBody = ParameterReplacer.Replace(second.Body, second.Parameters[0], parameter);

            var body = merge(first.Body, secondBody);
            return Expression.Lambda<Func<TIndexItem, bool>>(body, parameter);
        }

        /// <summary>
        /// Efficiently replaces all occurences of one parameter with another within an expression.
        /// </summary>
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            /// <summary>
            /// Initializes a new instance of the class with the specified source and 
            /// replacement parameters.
            /// </summary>
            /// <param name="oldParameter">
            /// The parameter expression to be replaced in the expression tree. Cannot be null.
            /// </param>
            /// <param name="newParameter">
            /// The parameter expression to use as a replacement. Cannot be null.
            /// </param>
            private ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            /// <summary>
            /// Replaces all occurrences of a specified parameter expression within an 
            /// expression tree with a new parameter expression.
            /// </summary>
            /// <remarks>
            /// This method is useful when reusing or transforming expression trees that
            /// require parameter substitution, such as when combining or composing lambda 
            /// expressions.
            /// </remarks>
            /// <param name="expression">
            /// The expression tree in which to replace the parameter.
            /// </param>
            /// <param name="oldParameter">
            /// The parameter expression to be replaced.
            /// </param>
            /// <param name="newParameter">
            /// The parameter expression to substitute in place of the old parameter.
            /// </param>
            /// <returns>
            /// A new expression tree with all instances of the old parameter replaced by 
            /// the new parameter.
            /// </returns>
            public static Expression Replace(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                return new ParameterReplacer(oldParameter, newParameter)
                    .Visit(expression);
            }

            /// <summary>
            /// Visits the specified parameter expression and replaces it with a new parameter 
            /// if it matches the target parameter.
            /// </summary>
            /// <param name="node">
            /// The parameter expression to visit and potentially replace.
            /// </param>
            /// <returns>
            /// The original parameter expression if it does not match the target; otherwise, 
            /// the replacement parameter expression.
            /// </returns>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == _oldParameter)
                {
                    return _newParameter;
                }

                return base.VisitParameter(node);
            }
        }
    }
}
