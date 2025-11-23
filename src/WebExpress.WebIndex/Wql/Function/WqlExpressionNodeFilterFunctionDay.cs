using System;
using System.Linq;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// Describes the function expression of a wql statement.
    /// Returns the current date.
    /// </summary>
    public class WqlExpressionNodeFilterFunctionDay<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDay()
            : base("day")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public override object Execute()
        {
            var parameters = Parameters.Select(x => x.GetValue());
            var param = parameters.FirstOrDefault();

            if (param is not null)
            {
                return DateTime.Now.Date.AddDays(Convert.ToDouble(param));
            }

            return DateTime.Now.Date;
        }
    }
}