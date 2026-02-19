using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// Describes the function expression of a wql statement.
    /// </summary>
    public abstract class WqlExpressionNodeFilterFunction<TIndexItem> : IWqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the function name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the parameter expressions.
        /// </summary>
        public IEnumerable<WqlExpressionNodeParameter<TIndexItem>> Parameters { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The function name</param>
        protected WqlExpressionNodeFilterFunction(string name)
        {
            Name = name.ToLower();
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public abstract object Execute();

        /// <summary> 
        /// Converts this function node into a LINQ expression. The default implementation 
        /// creates a method call expression by delegating to the derived class. 
        /// </summary> 
        /// <param name="param"> 
        /// The parameter expression representing the index item in the generated 
        /// expression tree (e.g., <c>x</c> in <c>x => function(x.Property)</c>). 
        /// </param> 
        /// <returns> 
        /// A method call expression or any other expression produced by the derived 
        //// function implementation. 
        /// </returns> 
        public abstract Expression ToExpression(ParameterExpression param);

        /// <summary>
        /// Converts the function expression to a string.
        /// </summary>
        /// <returns>The function expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0}({1})", Name, string.Join(", ", Parameters)).Trim();
        }
    }
}