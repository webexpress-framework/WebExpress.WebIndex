using WebExpress.WebIndex.Wql.Condition;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Interface for parsing WQL (WebExpress Query Language) queries and managing condition and function expressions.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item that implements the IIndexItem interface.</typeparam>
    public interface IWqlParser<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Performs an incremental lookahead analysis (ILA) over the given
        /// wql input. The method determines which portion of the input is syntactically 
        /// valid so far and which tokens would be permissible at the current position.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <returns>
        /// An lookahead object that contains the results of the analysis, including 
        /// valid tokens and expected next tokens.
        /// </returns>
        IWqlLookahead Analyze(string input);

        /// <summary>
        /// Parses a given wql query.
        /// </summary>
        /// <param name="input">An input string that contains a wql query.</param>
        /// <param name="culture">The culture in which to run the wql.</param>
        /// <returns>A wql object that represents the structure of the query.</returns>
        IWqlStatement<TIndexItem> Parse(string input);

        /// <summary>
        /// Registers a condition expression.
        /// </summary>
        /// <typeparam name="TCondition">The type of the condition expression to register.</typeparam>
        /// <exception cref="WqlParseException">Thrown when the condition expression cannot be registered.</exception>
        void RegisterCondition<TCondition>()
            where TCondition : IWqlExpressionNodeFilterCondition<TIndexItem>, new();

        /// <summary>
        /// Registers a function expression.
        /// </summary>
        /// <typeparam name="TFunction">The type of the function expression to register.</typeparam>
        /// <exception cref="WqlParseException">Thrown when the function expression cannot be registered.</exception>
        void RegisterFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new();

        /// <summary>
        /// Removes a condition expression.
        /// </summary>
        /// <param name="op">The operator to be derisgistrated.</param>
        void RemoveCondition(string op);

        /// <summary>
        /// Removes a function expression.
        /// </summary>
        /// <param name="name">The function name to be derisgistrated.</param>
        void RemoveFunction(string name);
    }
}
