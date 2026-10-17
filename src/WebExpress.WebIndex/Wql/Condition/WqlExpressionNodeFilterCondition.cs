using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Memory;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents an abstract base class for a WQL expression node filter condition.
    /// </summary>
    /// <remarks>
    /// A condition is evaluated in one of two ways. Where the reverse index of the attribute
    /// answers the question exactly - a text search on a term index, a comparison of numbers
    /// on a numeric index - the index is asked. Otherwise the items of the document store are
    /// scanned with the predicate that <see cref="ToExpression"/> builds, which is the same
    /// predicate a query built from the statement uses, so both paths agree by construction.
    /// </remarks>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterCondition<TIndexItem> : IWqlExpressionNodeFilterCondition<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private Func<TIndexItem, bool> _predicate;

        /// <summary>
        /// Returns the attribute expression.
        /// </summary>
        public WqlExpressionNodeAttribute<TIndexItem> Attribute { get; internal set; }

        /// <summary>
        /// Returns the function on the left-hand side, if the condition compares the value of
        /// a function instead of an attribute, as in <c>len(Name) &gt; 3</c>.
        /// </summary>
        public WqlExpressionNodeFilterFunction<TIndexItem> Function { get; internal set; }

        /// <summary>
        /// Returns the operator expression.
        /// </summary>
        public string Operator { get; internal set; }

        /// <summary>
        /// Returns a value indicating whether this operator expects a scalar value
        /// instead of a list of values.
        /// </summary>
        public bool IsScalar { get; internal set; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Returns the tokens of the operator, used to locate semantic errors.
        /// </summary>
        internal IWqlToken OperatorToken { get; set; }

        /// <summary>
        /// Returns the type of the left-hand side, or null if the attribute is unknown. A
        /// function whose type is only known when it runs yields <see cref="object"/>.
        /// </summary>
        public Type LeftType => Function switch
        {
            WqlExpressionNodeFilterFunctionScalar<TIndexItem> scalar => scalar.ReturnType,
            not null => typeof(object),
            _ => Attribute?.Type
        };

        /// <summary>
        /// Returns true if the left-hand side is an attribute of type string, the only
        /// operand to which the text search of WQL applies.
        /// </summary>
        protected bool IsTextAttribute => Function is null && Attribute?.Type == typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation. Multiple tokens are separated by spaces.</param>
        /// <param name="isScalar">Indicates whether the operator expects a scalar value.</param>
        protected WqlExpressionNodeFilterCondition(string token, bool isScalar = true)
        {
            Operator = token;
            IsScalar = isScalar;
        }

        /// <summary>
        /// Applies the filter condition to the index using the specified attribute
        /// and returns the matching data identifiers.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>
        /// A sequence of data identifiers that satisfy the filter condition.
        /// </returns>
        public abstract IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument);

        /// <summary>
        /// Builds the LINQ expression that represents this WQL node within the
        /// generated expression tree.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the resulting
        /// lambda expression (e.g., <c>x</c> in <c>x => ...</c>).
        /// </param>
        /// <returns>
        /// An expression that models the semantic meaning of this WQL expression node.
        /// </returns>
        public abstract Expression ToExpression(ParameterExpression param);

        /// <summary>
        /// Builds the expression of the left-hand side.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>The attribute access or the function call.</returns>
        protected Expression LeftToExpression(ParameterExpression param)
        {
            return Function is not null
                ? Function.ToExpression(param)
                : Attribute.ToExpression(param);
        }

        /// <summary>
        /// Evaluates the condition item by item over the document store, with the same
        /// predicate that <see cref="ToExpression"/> builds for a query.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The identifiers of the matching items.</returns>
        protected IEnumerable<Guid> Scan(IIndexDocument<TIndexItem> indexDocument)
        {
            _predicate ??= CompilePredicate();

            return indexDocument.All
                .Where(_predicate)
                .Select(x => x.Id);
        }

        /// <summary>
        /// Returns the identifiers of all items except the given ones.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="ids">The identifiers to leave out; they must be complete, since every one missing would turn up in the result.</param>
        /// <returns>The remaining identifiers.</returns>
        protected static IEnumerable<Guid> Complement(IIndexDocument<TIndexItem> indexDocument, IEnumerable<Guid> ids)
        {
            return indexDocument.All
                .Select(x => x.Id)
                .Except(ids);
        }

        /// <summary>
        /// Retrieves the items whose attribute equals a value, if the reverse index can
        /// answer that exactly: a phrase on a term index of a text, a number on a numeric
        /// index, or a boolean on the term index of a boolean.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="value">The value.</param>
        /// <param name="distance">The distance of a phrase.</param>
        /// <param name="similarity">The similarity of a phrase.</param>
        /// <param name="ids">The identifiers of the matching items.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveEqual(IIndexDocument<TIndexItem> indexDocument, object value, uint distance, uint similarity, out IEnumerable<Guid> ids)
        {
            ids = null;

            if (value is null)
            {
                return false;
            }

            var (index, kind) = FindReverseIndex(indexDocument);
            var type = Nullable.GetUnderlyingType(Attribute?.Type ?? typeof(object)) ?? Attribute?.Type;

            if (kind == IndexKind.Term && type == typeof(string))
            {
                ids = index.Retrieve(WqlValueConverter.ToText(value), Uncapped(IndexRetrieveMethod.Phrase, distance, similarity));

                return true;
            }

            if (kind == IndexKind.Term && type == typeof(bool) &&
                WqlValueConverter.TryConvert(value, typeof(bool), Culture, out var flag))
            {
                ids = index.Retrieve(WqlValueConverter.ToText(flag), Uncapped(IndexRetrieveMethod.Phrase, 0, 0));

                return true;
            }

            return TryRetrieveNumber(indexDocument, value, IndexRetrieveMethod.Phrase, out ids);
        }

        /// <summary>
        /// Retrieves the items whose attribute compares to a number as requested, if the
        /// attribute is numeric and has a numeric reverse index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="value">The value.</param>
        /// <param name="method">The comparison of the index.</param>
        /// <param name="ids">The identifiers of the matching items.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveNumber(IIndexDocument<TIndexItem> indexDocument, object value, IndexRetrieveMethod method, out IEnumerable<Guid> ids)
        {
            ids = null;

            var (index, kind) = FindReverseIndex(indexDocument);

            if (kind != IndexKind.Numeric || !WqlValueConverter.IsNumeric(Attribute.Type) ||
                !WqlValueConverter.TryConvert(value, typeof(decimal), Culture, out var number))
            {
                return false;
            }

            ids = index.Retrieve(number, Uncapped(method, 0, 0));

            return true;
        }

        /// <summary>
        /// Retrieves the items whose text attribute contains the words, if the attribute has
        /// a term index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="value">The words.</param>
        /// <param name="distance">The distance between the words.</param>
        /// <param name="similarity">The similarity of the words.</param>
        /// <param name="ids">The identifiers of the matching items.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveWords(IIndexDocument<TIndexItem> indexDocument, object value, uint distance, uint similarity, out IEnumerable<Guid> ids)
        {
            ids = null;

            var (index, kind) = FindReverseIndex(indexDocument);

            if (kind != IndexKind.Term || !IsTextAttribute)
            {
                return false;
            }

            ids = index.Retrieve(WqlValueConverter.ToText(value), Uncapped(IndexRetrieveMethod.Default, distance, similarity));

            return true;
        }

        /// <summary>
        /// Compiles the predicate of the condition once; it is reused by every scan.
        /// </summary>
        /// <returns>The compiled predicate.</returns>
        private Func<TIndexItem, bool> CompilePredicate()
        {
            var param = Expression.Parameter(typeof(TIndexItem), "x");

            return Expression.Lambda<Func<TIndexItem, bool>>(ToExpression(param), param).Compile();
        }

        /// <summary>
        /// Finds the reverse index of the attribute and determines its kind. A function on the
        /// left-hand side, an unknown or ignored attribute and an index of a kind the condition
        /// does not know have no usable index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The reverse index and its kind.</returns>
        private (IIndexReverse<TIndexItem> Index, IndexKind Kind) FindReverseIndex(IIndexDocument<TIndexItem> indexDocument)
        {
            if (Function is not null || Attribute?.Type is null)
            {
                return (null, IndexKind.None);
            }

            var field = indexDocument.Fields
                .FirstOrDefault(x => x.Name.Equals(Attribute.Name, StringComparison.OrdinalIgnoreCase));
            var index = field is not null ? indexDocument.GetReverseIndex(field) : null;

            return index switch
            {
                IndexMemoryReverseNumeric<TIndexItem> or IndexStorageReverseNumeric<TIndexItem> => (index, IndexKind.Numeric),
                IndexMemoryReverseTerm<TIndexItem> or IndexStorageReverseTerm<TIndexItem> => (index, IndexKind.Term),
                _ => (null, IndexKind.None)
            };
        }

        /// <summary>
        /// Creates retrieve options without a result limit. A condition hands its result to
        /// further set operations, where a capped result would silently drop matches - and in
        /// a complement turn them into false hits.
        /// </summary>
        /// <param name="method">The retrieve method.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="similarity">The similarity.</param>
        /// <returns>The options.</returns>
        private static IndexRetrieveOptions Uncapped(IndexRetrieveMethod method, uint distance, uint similarity)
        {
            return new IndexRetrieveOptions()
            {
                MaxResults = uint.MaxValue,
                Method = method,
                Distance = distance,
                Similarity = similarity
            };
        }

        /// <summary>
        /// The kinds of reverse index a condition can ask.
        /// </summary>
        private enum IndexKind
        {
            None,
            Term,
            Numeric
        }
    }
}
