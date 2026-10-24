using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// The base of a WQL function that computes one value from its arguments. The value is
    /// computed by a single method, <see cref="Evaluate"/>, which feeds both
    /// <see cref="Execute"/> and the expression tree; the reverse index and a query built from
    /// the statement therefore cannot compute different values. The expression tree calls the
    /// method when the query runs, so that time-dependent functions such as <c>now()</c> are
    /// not frozen when the statement is parsed.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterFunctionScalar<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private static readonly MethodInfo InvokeMethod = typeof(WqlExpressionNodeFilterFunctionScalar<TIndexItem>)
            .GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly int _minArguments;
        private readonly int _maxArguments;

        /// <summary>
        /// Returns the smallest number of arguments the function accepts.
        /// </summary>
        public override int MinArguments => _minArguments;

        /// <summary>
        /// Returns the largest number of arguments the function accepts.
        /// </summary>
        public override int MaxArguments => _maxArguments;

        /// <summary>
        /// Returns the type of the computed value. A concrete type lets conditions compare the
        /// value with the operator of that type; it must be able to hold null if
        /// <see cref="Evaluate"/> may return null.
        /// </summary>
        public virtual Type ReturnType => typeof(object);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="minArguments">The smallest number of arguments.</param>
        /// <param name="maxArguments">The largest number of arguments.</param>
        protected WqlExpressionNodeFilterFunctionScalar(string name, int minArguments, int maxArguments)
            : base(name)
        {
            _minArguments = minArguments;
            _maxArguments = maxArguments;
        }

        /// <summary>
        /// Computes the value of the function from the values of its arguments.
        /// </summary>
        /// <param name="arguments">The argument values in call order; a missing argument is absent from the list.</param>
        /// <returns>The value.</returns>
        protected abstract object Evaluate(IReadOnlyList<object> arguments);

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public sealed override object Execute()
        {
            return Evaluate([.. (Parameters ?? []).Select(x => x.GetValue())]);
        }

        /// <summary>
        /// Builds the expression that computes the value of the function per item. The
        /// arguments are evaluated per item, so attributes and nested functions are read when
        /// the query runs.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>An expression of the <see cref="ReturnType"/>.</returns>
        public sealed override Expression ToExpression(ParameterExpression param)
        {
            var arguments = (Parameters ?? [])
                .Select(x => x.ToExpression(param))
                .Select(x => x.Type == typeof(object) ? x : Expression.Convert(x, typeof(object)));

            var call = Expression.Call
            (
                Expression.Constant(this),
                InvokeMethod,
                Expression.NewArrayInit(typeof(object), arguments)
            );

            return ReturnType == typeof(object) ? call : Expression.Convert(call, ReturnType);
        }

        /// <summary>
        /// Returns an argument, or null if the call has fewer arguments.
        /// </summary>
        /// <param name="arguments">The argument values.</param>
        /// <param name="index">The zero-based position of the argument.</param>
        /// <returns>The argument value.</returns>
        protected static object Argument(IReadOnlyList<object> arguments, int index)
        {
            return arguments is not null && index < arguments.Count ? arguments[index] : null;
        }

        /// <summary>
        /// Converts an argument into a number.
        /// </summary>
        /// <param name="value">The argument value.</param>
        /// <returns>The number, or null if the value is null or no number.</returns>
        protected static double? ToNumber(object value)
        {
            return WqlValueConverter.TryConvert(value, typeof(double), CultureInfo.InvariantCulture, out var number)
                ? (double?)number
                : null;
        }

        /// <summary>
        /// Converts an argument into a text.
        /// </summary>
        /// <param name="value">The argument value.</param>
        /// <returns>The text, or null for a null value.</returns>
        protected static string ToText(object value)
        {
            return WqlValueConverter.ToText(value);
        }

        /// <summary>
        /// Converts an argument into a point in time.
        /// </summary>
        /// <param name="value">The argument value.</param>
        /// <returns>The point in time, or null if the value is null or no date.</returns>
        protected static DateTime? ToDateTime(object value)
        {
            return WqlValueConverter.TryConvert(value, typeof(DateTime), CultureInfo.InvariantCulture, out var date)
                ? (DateTime?)date
                : null;
        }

        /// <summary>
        /// The entry point of the expression tree. It forwards to <see cref="Evaluate"/>, which
        /// derived classes keep protected.
        /// </summary>
        /// <param name="arguments">The argument values.</param>
        /// <returns>The value.</returns>
        private object Invoke(object[] arguments)
        {
            return Evaluate(arguments);
        }
    }
}
