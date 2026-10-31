using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WebExpress.WebIndex.Wql.Condition;
using WebExpress.WebIndex.Wql.Function;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Implements a parser for the WQL query language. The parser reads an input string that
    /// contains a WQL query and returns a WQL object that represents the structure of the
    /// query. To use the parser, call the Parse method with the string to be parsed to get
    /// a WQL object. This object contains the structure of the WQL query and can be used
    /// to evaluate or process the query.
    /// </summary>
    /// <remarks>
    /// Grammar (BNF-like):
    /// <![CDATA[
    /// <WQL>                      ::= <Filter> <Order> <Partitioning> | ε
    /// <Filter>                   ::= <OrFilter> | ε
    /// <OrFilter>                 ::= <AndFilter> | <OrFilter> <OrOperator> <AndFilter>
    /// <AndFilter>                ::= <NotFilter> | <AndFilter> <AndOperator> <NotFilter>
    /// <NotFilter>                ::= "not" <NotFilter> | <PrimaryFilter>
    /// <PrimaryFilter>            ::= "(" <OrFilter> ")" | <Condition>
    /// <OrOperator>               ::= "or" | "||"
    /// <AndOperator>              ::= "and" | "&"
    /// <Condition>                ::= <Operand> <BinaryOperator> <Parameter> <ParameterOptions> | <Operand> <SetOperator> "(" <Parameter> <ParameterNext> ")" | <Operand> <RangeOperator> <Parameter> "and" <Parameter>
    /// <Operand>                  ::= <Attribute> | <Function>
    /// <Attribute>                ::= <Name> | <Name> "." <Attribute>
    /// <Parameter>                ::= <Function> | <Literal> | <Attribute>
    /// <Literal>                  ::= <DoubleValue> | "null" | "true" | "false" | """ <QuotedValue> """ | "'" <QuotedValue> "'" | <StringValue>
    /// <ParameterOptions>         ::= <ParameterFuzzyOptions> | <ParameterDistanceOptions> | <ParameterFuzzyOptions> <ParameterDistanceOptions> | <ParameterDistanceOptions> <ParameterFuzzyOptions> | ε
    /// <ParameterFuzzyOptions>    ::= "~" <Number>
    /// <ParameterDistanceOptions> ::= ":" <Number>
    /// <Function>                 ::= <Name> "(" <Parameter> <ParameterNext> ")" | <Name> "(" ")"
    /// <ParameterNext>            ::= "," <Parameter> <ParameterNext> | ε
    /// <BinaryOperator>           ::= "=" | ">" | "<" | ">=" | "<=" | "!=" | "~" | "!~" | "is" | "is not" | "starts with" | "ends with" | "contains"
    /// <SetOperator>              ::= "in" | "not in"
    /// <RangeOperator>            ::= "between" | "not between"
    /// <Order>                    ::= "order" "by" <Attribute> <DescendingOrder> <OrderNext> | ε
    /// <OrderNext>                ::= "," <Attribute> <DescendingOrder> <OrderNext> | ε
    /// <DescendingOrder>          ::= "asc" | "desc" | ε
    /// <Partitioning>             ::= <Partitioning> <Partitioning> | <PartitioningOperator> <Number> | ε
    /// <PartitioningOperator>     ::= "take" | "skip"
    /// <Name>                     ::= [A-Za-z_][A-Za-z0-9_]+
    /// <StringValue>              ::= [A-Za-z0-9_@<>=~$%/!+.,;:\-]+
    /// <QuotedValue>              ::= any characters, the enclosing quotation mark written twice
    /// <DoubleValue>              ::= [+-]?[0-9]*[.]?[0-9]+
    /// <Number>                   ::= [0-9]+
    /// ]]>
    /// "and" binds stronger than "or", and chains of the same operator group from the left.
    /// The unquoted literals null, true and false are typed values; a quoted value is always
    /// a string. An unquoted word that names an attribute refers to that attribute.
    /// </remarks>
    public partial class WqlParser<TIndexItem> : IWqlParser<TIndexItem>
        where TIndexItem : IIndexItem
    {
        [GeneratedRegex("^[0-9]+$", RegexOptions.Compiled)]
        private static partial Regex NumberRegex();

        [GeneratedRegex("^[+-]?[0-9]*[.]?[0-9]+$", RegexOptions.Compiled)]
        private static partial Regex DoubleRegex();

        // require tilde prefix to avoid ambiguity with plain numbers
        [GeneratedRegex("^~[0-9]+$", RegexOptions.Compiled)]
        private static partial Regex FuzzyRegex();

        // require colon prefix to avoid ambiguity and ensure correct slicing
        [GeneratedRegex("^:[0-9]+$", RegexOptions.Compiled)]
        private static partial Regex DistanceRegex();

        [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled)]
        private static partial Regex NameRegex();

        /// <summary>
        /// Returns registered conditions keyed by their operator.
        /// </summary>
        private IDictionary<string, Type> Conditions { get; set; } = new SortedDictionary<string, Type>(new WqlParserLengthComparer());

        /// <summary>
        /// Returns registered functions keyed by their name.
        /// </summary>
        private IDictionary<string, Type> Functions { get; set; } = new SortedDictionary<string, Type>(new WqlParserLengthComparer());

        /// <summary>
        /// Returns the culture used for parsing numeric values.
        /// </summary>
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// Returns available index attributes.
        /// </summary>
        protected IEnumerable<IndexFieldData> Attributes { get; private set; }

        /// <summary>
        /// Returns the index document abstraction.
        /// </summary>
        protected IIndexDocument<TIndexItem> IndexDocument { get; private set; }

        /// <summary>
        /// Initializes a new instance of the parser and registers default conditions and functions.
        /// </summary>
        public WqlParser()
        {
            // materialize once; the deferred enumeration would re-run the
            // reflection walk on every use
            Attributes = GetFieldData(typeof(TIndexItem)).ToList();

            RegisterCondition<WqlExpressionNodeFilterConditionBinaryEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLike<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryNotLike<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryGreaterThan<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryGreaterThanOrEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLessThan<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLessThanOrEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryNotEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryIs<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryIsNot<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryStartsWith<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryEndsWith<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryContains<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionSetIn<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionSetNotIn<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionRangeBetween<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionRangeNotBetween<TIndexItem>>();

            RegisterFunction<WqlExpressionNodeFilterFunctionDay<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionNow<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionUpper<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionLower<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionLen<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionTrim<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionYear<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionMonth<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionConcat<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionReplace<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionSubstring<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionCoalesce<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionNullIf<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionAbs<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionRound<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionFloor<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionCeil<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionDate<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionDateAdd<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionDateDiff<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionYearOf<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionMonthOf<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionDayOf<TIndexItem>>();
        }

        /// <summary>
        /// Performs an incremental lookahead analysis (ILA) over the given
        /// wql input. The method determines which portion of the input is syntactically
        /// valid so far and which tokens would be permissible at the current position.
        /// Semantic errors, such as an unknown attribute, do not stop the analysis, since a
        /// statement being typed passes through them on its way to a valid one.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <returns>
        /// An lookahead object that contains the results of the analysis, including
        /// valid tokens and expected next tokens.
        /// </returns>
        public IWqlLookahead Analyze(string input)
        {
            var ilaQueue = new Queue<WqlLookaheadToken>();

            try
            {
                var tokens = Tokenize(input);

                // try parsing the current slice.
                Parse(input, tokens, ilaQueue, []);
            }
            catch (Exception)
            {
                // parsing failed at token i → return lookahead info.
                return new WqlLookahead
                {
                    Items = ilaQueue,
                    IsValidSoFar = false
                };
            }

            return new WqlLookahead()
            {
                Items = ilaQueue,
                IsValidSoFar = true
            };
        }

        /// <summary>
        /// Parses an input WQL string into an abstract syntax tree representation. The method
        /// never throws; a syntax or semantic error is returned as part of the statement,
        /// whose execution is then rejected.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <returns>The parsed statement with possible error information.</returns>
        public IWqlStatement<TIndexItem> Parse(string input)
        {
            var ilaQueue = new Queue<WqlLookaheadToken>();
            var errors = new List<WqlExpressionError>();

            try
            {
                var tokens = Tokenize(input);
                var statement = Parse(input, tokens, ilaQueue, errors);

                if (errors.Count > 0)
                {
                    statement.Error = errors[0];
                }

                return statement;
            }
            catch (WqlParseException ex)
            {
                return new WqlStatement<TIndexItem>(input)
                {
                    Culture = Culture,
                    Error = new WqlExpressionError()
                    {
                        Culture = Culture,
                        Message = ex.Message,
                        Position = ex.Token.FirstOrDefault()?.Offset ?? 0,
                        Length = ex.Token.FirstOrDefault()?.Length ?? 0
                    }
                };
            }
            catch (Exception)
            {
                // a registered condition or function may fail in ways the parser cannot
                // foresee; the caller still receives a statement and not an exception
                return new WqlStatement<TIndexItem>(input)
                {
                    Culture = Culture,
                    Error = new WqlExpressionError()
                    {
                        Culture = Culture,
                        Message = "webexpress.webindex:wql.parse_failed"
                    }
                };
            }
        }

        /// <summary>
        /// Parses an input WQL string into an abstract syntax tree representation.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <returns>The parsed statement with possible error information.</returns>
        private WqlStatement<TIndexItem> Parse(string input, Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors)
        {
            var wql = new WqlStatement<TIndexItem>(input)
            {
                Culture = Culture
            };

            if (string.IsNullOrWhiteSpace(input))
            {
                ilaQueue.Enqueue(new WqlLookaheadToken(new WqlToken() { Value = "" }, WqlExpressionType.None)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis, WqlExpressionType.Order, WqlExpressionType.PartitioningOperator]
                });

                return wql;
            }

            wql.Filter = ParseFilter(tokenQueue, ilaQueue, errors);
            wql.Order = ParseOrder(tokenQueue, ilaQueue, errors);
            wql.Partitioning = ParsePartitioning(tokenQueue, ilaQueue);

            if (tokenQueue.Count != 0)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    PeekToken(tokenQueue)
                );
            }

            return wql;
        }

        /// <summary>
        /// Parses a filter expression.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <returns>The filter node or null.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParseFilter(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors)
        {
            if (PeekToken(tokenQueue, "order") ||
                PeekToken(tokenQueue, "orderby") ||
                PeekToken(tokenQueue, "take") ||
                PeekToken(tokenQueue, "skip"))
            {
                return null;
            }

            return ParseOr(tokenQueue, ilaQueue, errors, 0);
        }

        /// <summary>
        /// Parses a chain of filters joined by "or". It binds weaker than "and", so it
        /// parses its operands as "and" chains and groups them from the left.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <param name="deep">
        /// The number of enclosing parentheses, which decides whether a completed condition
        /// may be followed by a closing parenthesis or by the order and partitioning clauses.
        /// </param>
        /// <returns>The filter node.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParseOr(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors, int deep)
        {
            var filter = ParseAnd(tokenQueue, ilaQueue, errors, deep);

            while (PeekToken(tokenQueue, "or") || PeekToken(tokenQueue, "||"))
            {
                filter = new WqlExpressionNodeFilterBinary<TIndexItem>
                {
                    LeftFilter = filter,
                    LogicalOperator = ParseLogicalOperatorWithLookahead(tokenQueue, ilaQueue),
                    RightFilter = ParseAnd(tokenQueue, ilaQueue, errors, deep)
                };
            }

            return filter;
        }

        /// <summary>
        /// Parses a chain of filters joined by "and", grouped from the left.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <param name="deep">The number of enclosing parentheses.</param>
        /// <returns>The filter node.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParseAnd(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors, int deep)
        {
            var filter = ParseNot(tokenQueue, ilaQueue, errors, deep);

            while (PeekToken(tokenQueue, "and") || PeekToken(tokenQueue, "&"))
            {
                filter = new WqlExpressionNodeFilterBinary<TIndexItem>
                {
                    LeftFilter = filter,
                    LogicalOperator = ParseLogicalOperatorWithLookahead(tokenQueue, ilaQueue),
                    RightFilter = ParseNot(tokenQueue, ilaQueue, errors, deep)
                };
            }

            return filter;
        }

        /// <summary>
        /// Parses a filter that may be negated by a leading "not".
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <param name="deep">The number of enclosing parentheses.</param>
        /// <returns>The filter node.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParseNot(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors, int deep)
        {
            if (!PeekToken(tokenQueue, "not"))
            {
                return ParsePrimary(tokenQueue, ilaQueue, errors, deep);
            }

            var notToken = ReadToken(tokenQueue);

            ilaQueue.Enqueue(new WqlLookaheadToken(notToken, WqlExpressionType.LogicalOperator)
            {
                ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
            });

            return new WqlExpressionNodeFilterNot<TIndexItem>
            {
                NegatedFilter = ParseNot(tokenQueue, ilaQueue, errors, deep)
            };
        }

        /// <summary>
        /// Parses a group in parentheses or a single condition.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <param name="deep">The number of enclosing parentheses.</param>
        /// <returns>The filter node.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParsePrimary(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors, int deep)
        {
            if (PeekToken(tokenQueue, "("))
            {
                var openToken = ReadToken(tokenQueue, "(");
                ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.OpenParenthesis)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
                });

                var filter = ParseOr(tokenQueue, ilaQueue, errors, deep + 1);

                var closeToken = ReadToken(tokenQueue, ")")
                    ?? throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_close_parenthesis",
                        []
                    );

                ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                {
                    ExpectedNextTokens = FilterFollowers(deep)
                });

                return filter;
            }

            return new WqlExpressionNodeFilter<TIndexItem>
            {
                Condition = ParseCondition(tokenQueue, ilaQueue, errors, FilterFollowers(deep))
            };
        }

        /// <summary>
        /// Parses a logical operator and records it for the lookahead analysis.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <returns>The logical operator.</returns>
        private static WqlExpressionLogicalOperator ParseLogicalOperatorWithLookahead(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var logicalToken = PeekToken(tokenQueue);
            var logicalOperator = ParseLogicalOperator(tokenQueue, ilaQueue);

            ilaQueue.Enqueue(new WqlLookaheadToken(logicalToken, WqlExpressionType.LogicalOperator)
            {
                ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
            });

            return logicalOperator;
        }

        /// <summary>
        /// The token types that may follow a value inside a parenthesized list - the values
        /// of a set condition or the arguments of a function.
        /// </summary>
        private static readonly WqlExpressionType[] ListFollowers = [WqlExpressionType.Separator, WqlExpressionType.CloseParenthesis];

        /// <summary>
        /// Returns the token types that may follow a completed condition. Inside a group only
        /// another condition or the end of the group may follow, since the order and
        /// partitioning clauses belong to the whole statement.
        /// </summary>
        /// <param name="deep">The number of enclosing parentheses.</param>
        /// <returns>The permissible followers.</returns>
        private static WqlExpressionType[] FilterFollowers(int deep)
        {
            return deep > 0
                ? [WqlExpressionType.LogicalOperator, WqlExpressionType.CloseParenthesis]
                : [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.PartitioningOperator];
        }

        /// <summary>
        /// Parses a single condition expression.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <param name="followers">The token types that may follow the completed condition.</param>
        /// <returns>The condition node.</returns>
        private WqlExpressionNodeFilterCondition<TIndexItem> ParseCondition(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors, WqlExpressionType[] followers)
        {
            WqlExpressionNodeAttribute<TIndexItem> attribute = null;
            WqlExpressionNodeFilterFunction<TIndexItem> function = null;

            if (PeekFunctionCall(tokenQueue))
            {
                function = ParseFunction(tokenQueue, ilaQueue, [WqlExpressionType.Operator]);
            }
            else
            {
                attribute = ParseAttribute(tokenQueue, ilaQueue);
            }

            if (tokenQueue.Count == 0)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.expected_operator",
                    ReadToken(tokenQueue)
                );
            }

            var condition = Conditions
                .FirstOrDefault(x => PeekToken(tokenQueue, x.Key?.Split(' ')));

            try
            {
                if (condition.Value is null || string.IsNullOrWhiteSpace(condition.Key))
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(PeekToken(tokenQueue), WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Operator]
                    });

                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.condition_unknown",
                        ReadToken(tokenQueue)
                    );
                }

                var operationTokens = ReadToken(tokenQueue, condition.Key.Split(' ')).ToList();
                var operatorToken = new WqlTokenCombine(operationTokens);
                var instance = Activator.CreateInstance(condition.Value) as WqlExpressionNodeFilterCondition<TIndexItem>;

                if (instance is not null)
                {
                    instance.Culture = Culture;
                    instance.Attribute = attribute;
                    instance.Function = function;
                    instance.OperatorToken = operatorToken;
                }

                if (instance is WqlExpressionNodeFilterConditionBinary<TIndexItem> binary)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(operatorToken, WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    binary.Parameter = ParseParameter(tokenQueue, ilaQueue, true, followers);
                    binary.Options = ParseParameterOptions(tokenQueue, ilaQueue);
                }
                else if (instance is WqlExpressionNodeFilterConditionSet<TIndexItem> set)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(operatorToken, WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.OpenParenthesis]
                    });

                    var parameters = new List<WqlExpressionNodeParameter<TIndexItem>>();
                    var openToken = ReadToken(tokenQueue, "(");

                    ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.OpenParenthesis)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    parameters.Add(ParseParameter(tokenQueue, ilaQueue, true, ListFollowers));

                    while (PeekToken(tokenQueue, ","))
                    {
                        var separatorToken = ReadToken(tokenQueue, ",");

                        ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                        {
                            ExpectedNextTokens = [WqlExpressionType.Parameter]
                        });

                        parameters.Add(ParseParameter(tokenQueue, ilaQueue, false, ListFollowers));
                    }

                    var closeToken = ReadToken(tokenQueue, ")");

                    ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                    {
                        ExpectedNextTokens = followers
                    });

                    set.Parameters = parameters;
                }
                else if (instance is WqlExpressionNodeFilterConditionRange<TIndexItem> range)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(operatorToken, WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    // the "and" between the bounds belongs to the range and is consumed here,
                    // so it never reaches the logical operators
                    range.Lower = ParseParameter(tokenQueue, ilaQueue, true, [WqlExpressionType.LogicalOperator]);

                    var andToken = ReadToken(tokenQueue, "and");

                    ilaQueue.Enqueue(new WqlLookaheadToken(andToken, WqlExpressionType.LogicalOperator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    range.Upper = ParseParameter(tokenQueue, ilaQueue, true, followers);
                }
                else
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_binary_or_set_condition",
                        operationTokens
                    );
                }

                ValidateCondition(instance, errors);

                return instance;
            }
            catch (WqlParseException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.condition_unknown",
                    ReadToken(tokenQueue)
                );
            }
        }

        /// <summary>
        /// Parses a logical operator token into an enum value.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The logical operator.</returns>
        private static WqlExpressionLogicalOperator ParseLogicalOperator(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var logicalOperatorToken = PeekToken(tokenQueue);
            var value = logicalOperatorToken?.Value?.ToLower();

            if (value == "and")
            {
                ReadToken(tokenQueue, "and");
                return WqlExpressionLogicalOperator.And;
            }
            else if (value == "&")
            {
                ReadToken(tokenQueue, "&");
                return WqlExpressionLogicalOperator.And;
            }
            else if (value == "or")
            {
                ReadToken(tokenQueue, "or");
                return WqlExpressionLogicalOperator.Or;
            }
            else if (value == "||")
            {
                ReadToken(tokenQueue, "||");
                return WqlExpressionLogicalOperator.Or;
            }

            throw new WqlParseException
            (
                "webexpress.webindex:wql.expected_logicaloperator",
                logicalOperatorToken
            );
        }

        /// <summary>
        /// Parses an attribute reference. An unknown attribute is kept in the tree, so that
        /// the lookahead analysis can continue behind it; the semantic check reports it.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The attribute node.</returns>
        private WqlExpressionNodeAttribute<TIndexItem> ParseAttribute(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var attributeToken = PeekToken(tokenQueue)
                ?? throw new WqlParseException
                (
                    "webexpress.webindex:wql.attribute_unknown",
                    (IWqlToken)null
                );

            ReadToken(tokenQueue);

            ilaQueue.Enqueue(new WqlLookaheadToken(attributeToken, WqlExpressionType.Attribute)
            {
                ExpectedNextTokens = [WqlExpressionType.Operator],
            });

            return CreateAttribute(attributeToken);
        }

        /// <summary>
        /// Creates the node of an attribute, resolved against the index item if possible.
        /// </summary>
        /// <param name="token">The token that names the attribute.</param>
        /// <returns>The attribute node; its type is null if the name denotes no property.</returns>
        private static WqlExpressionNodeAttribute<TIndexItem> CreateAttribute(WqlToken token)
        {
            var attribute = new WqlExpressionNodeAttribute<TIndexItem>
            {
                Name = token.Value,
                Token = token
            };

            if (TryResolveAttribute(token.Value, out var name, out var type))
            {
                attribute.Name = name;
                attribute.Type = type;
            }

            return attribute;
        }

        /// <summary>
        /// Resolves an attribute path against the index item.
        /// </summary>
        /// <param name="raw">The path as written, in any casing.</param>
        /// <param name="name">The path in the casing of the members.</param>
        /// <param name="type">The type of the property the path ends in.</param>
        /// <returns>True if the path denotes a property, otherwise false.</returns>
        private static bool TryResolveAttribute(string raw, out string name, out Type type)
        {
            name = null;
            type = null;

            if (string.IsNullOrWhiteSpace(raw) || !raw.Split('.').All(x => NameRegex().IsMatch(x)))
            {
                return false;
            }

            try
            {
                var path = WqlPropertyPath<TIndexItem>.Parse(raw);
                var property = path.Resolve(typeof(TIndexItem));

                if (property is null)
                {
                    return false;
                }

                name = path.ToString();
                type = property.PropertyType;

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a parameter node which can be a function call, a number, a string, one of
        /// the literals null, true and false, or another attribute.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="isScalar">
        /// Indicates whether the parameter is expected to be a scalar value.
        /// If false, the parser may accept or construct a list of values.
        /// </param>
        /// <param name="followers">The token types that may follow the completed parameter.</param>
        /// <returns>The parameter node.</returns>
        private WqlExpressionNodeParameter<TIndexItem> ParseParameter(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, bool isScalar, WqlExpressionType[] followers)
        {
            var functionOrValueToken = PeekToken(tokenQueue);
            var function = Functions
                .FirstOrDefault(x => PeekToken(tokenQueue, x.Key));

            if (tokenQueue.Count == 0)
            {
                throw new WqlParseException("", []);
            }
            else if (PeekToken(tokenQueue, function.Key ?? functionOrValueToken?.Value, "("))
            {
                return new WqlExpressionNodeParameter<TIndexItem>
                {
                    Function = ParseFunction(tokenQueue, ilaQueue, followers)
                };
            }
            else if (PeekToken(tokenQueue, DoubleRegex()))
            {
                var parameter = new WqlExpressionNodeParameter<TIndexItem>
                {
                    Value = ParseNumberLiteral(tokenQueue)
                };

                // without its own lookahead entry a number would leave the prompt at the
                // token before it and offer another value instead of what may follow
                ilaQueue.Enqueue(new WqlLookaheadToken(functionOrValueToken, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = followers
                });

                return parameter;
            }
            else if (functionOrValueToken?.Value == "\"" || functionOrValueToken?.Value == "'")
            {
                var quote = functionOrValueToken.Value;
                var openToken = ReadToken(tokenQueue);
                ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.Quotation)
                {
                    ExpectedNextTokens = [WqlExpressionType.Parameter, WqlExpressionType.Quotation]
                });

                var valueToken = PeekToken(tokenQueue);
                var value = ParseStringValue(tokenQueue);

                if (valueToken is null)
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_terminated_string_token",
                        [openToken]
                    );
                }

                var parameter = new WqlExpressionNodeParameter<TIndexItem>
                {
                    Value = new WqlExpressionNodeValue<TIndexItem>()
                    {
                        Culture = Culture,
                        // an empty pair of quotation marks is the empty string, not null
                        StringValue = value ?? "",
                        Token = valueToken
                    }
                };

                ilaQueue.Enqueue(new WqlLookaheadToken(valueToken, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = [WqlExpressionType.Quotation]
                });

                if (!PeekToken(tokenQueue, quote))
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(new WqlTokenCombine(openToken, valueToken), WqlExpressionType.Parameter)
                    {
                        ExpectedNextTokens = isScalar
                            ? [WqlExpressionType.Parameter, WqlExpressionType.Separator]
                            : [WqlExpressionType.Parameter]
                    });

                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_terminated_string_token",
                        functionOrValueToken
                    );
                }

                var closeToken = ReadToken(tokenQueue);
                ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.Quotation)
                {
                    ExpectedNextTokens = followers
                });

                return parameter;
            }
            else if (functionOrValueToken?.Value is "(" or ")" ||
                functionOrValueToken?.Value?.ToLower() is "and" or "&" or "or" or "||")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else
            {
                var token = ReadToken(tokenQueue);

                ilaQueue.Enqueue(new WqlLookaheadToken(token, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = followers
                });

                return ParseUnquotedParameter(token);
            }
        }

        /// <summary>
        /// Interprets an unquoted value. The words null, true and false are typed literals, a
        /// word that names an attribute refers to that attribute, and everything else is a
        /// string - quoting a value keeps it a string in every case.
        /// </summary>
        /// <param name="token">The token of the value.</param>
        /// <returns>The parameter node.</returns>
        private WqlExpressionNodeParameter<TIndexItem> ParseUnquotedParameter(WqlToken token)
        {
            var value = new WqlExpressionNodeValue<TIndexItem>()
            {
                Culture = Culture,
                Token = token
            };

            switch (token.Value?.ToLower())
            {
                case "null":
                    return new WqlExpressionNodeParameter<TIndexItem> { Value = value };
                case "true":
                    value.BoolValue = true;
                    return new WqlExpressionNodeParameter<TIndexItem> { Value = value };
                case "false":
                    value.BoolValue = false;
                    return new WqlExpressionNodeParameter<TIndexItem> { Value = value };
            }

            if (TryResolveAttribute(token.Value, out _, out _))
            {
                return new WqlExpressionNodeParameter<TIndexItem>
                {
                    Attribute = CreateAttribute(token)
                };
            }

            value.StringValue = token.Value;

            return new WqlExpressionNodeParameter<TIndexItem> { Value = value };
        }

        /// <summary>
        /// Parses optional parameter options (similarity and distance).
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The options node.</returns>
        private static WqlExpressionNodeParameterOption<TIndexItem> ParseParameterOptions(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var options = new WqlExpressionNodeParameterOption<TIndexItem>();
            var tokens = new List<IWqlToken>();

            // fuzzy can be written as "~" <number> or as a single token "~<number>"
            if (PeekToken(tokenQueue, FuzzyRegex()))
            {
                var token = ReadToken(tokenQueue, FuzzyRegex());
                tokens.Add(token);
                options.Similarity = ParseSimilarity(token, token.Value[1..]);
            }
            else if (PeekToken(tokenQueue, "~"))
            {
                var token = ReadToken(tokenQueue);
                tokens.Add(token);

                if (PeekToken(tokenQueue, NumberRegex()))
                {
                    var number = ReadToken(tokenQueue, NumberRegex());
                    tokens.Add(number);
                    options.Similarity = ParseSimilarity(number, number.Value);
                }
                else
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_similarity",
                        token
                    );
                }
            }

            // distance can be written as ":" <number> or as a single token ":<number>"
            if (PeekToken(tokenQueue, DistanceRegex()))
            {
                var token = ReadToken(tokenQueue, DistanceRegex());
                tokens.Add(token);
                options.Distance = ParseDistance(token, token.Value[1..]);
            }
            else if (PeekToken(tokenQueue, ":"))
            {
                var token = ReadToken(tokenQueue);
                tokens.Add(token);

                if (PeekToken(tokenQueue, NumberRegex()))
                {
                    var number = ReadToken(tokenQueue, NumberRegex());
                    tokens.Add(number);
                    options.Distance = ParseDistance(number, number.Value);
                }
                else
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_distance",
                        token
                    );
                }
            }

            options.Tokens = tokens;

            return options;
        }

        /// <summary>
        /// Parses a function invocation and its parameters.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="followers">The token types that may follow the completed invocation.</param>
        /// <returns>The function node.</returns>
        private WqlExpressionNodeFilterFunction<TIndexItem> ParseFunction(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, WqlExpressionType[] followers)
        {
            var parameters = new List<WqlExpressionNodeParameter<TIndexItem>>();
            var function = Functions
                .FirstOrDefault(x => PeekToken(tokenQueue, x.Key));
            var nameToken = ReadToken(tokenQueue);
            var tokenList = new List<IWqlToken>()
            {
                nameToken
            };

            if (nameToken is null)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.expected_function",
                    []
                );
            }

            ilaQueue.Enqueue(new WqlLookaheadToken(nameToken, WqlExpressionType.Function)
            {
                ExpectedNextTokens = [WqlExpressionType.OpenParenthesis]
            });

            try
            {
                if (Activator.CreateInstance(function.Value) is not WqlExpressionNodeFilterFunction<TIndexItem> instance)
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_function",
                        []
                    );
                }

                instance.Token = nameToken;

                var openToken = ReadToken(tokenQueue, "(");
                tokenList.Add(openToken);

                ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.OpenParenthesis)
                {
                    ExpectedNextTokens = [WqlExpressionType.Parameter, WqlExpressionType.Quotation, WqlExpressionType.CloseParenthesis]
                });

                if (PeekToken(tokenQueue, ")"))
                {
                    var closeToken = ReadToken(tokenQueue, ")");
                    tokenList.Add(closeToken);

                    ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                    {
                        ExpectedNextTokens = followers
                    });
                }
                else
                {
                    parameters.Add(ParseParameter(tokenQueue, ilaQueue, true, ListFollowers));

                    while (PeekToken(tokenQueue, ","))
                    {
                        var separatorToken = ReadToken(tokenQueue, ",");
                        tokenList.Add(separatorToken);

                        ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                        {
                            ExpectedNextTokens = [WqlExpressionType.Parameter, WqlExpressionType.Function]
                        });

                        parameters.Add(ParseParameter(tokenQueue, ilaQueue, true, ListFollowers));
                    }

                    var closeToken = ReadToken(tokenQueue, ")");
                    tokenList.Add(closeToken);

                    ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                    {
                        ExpectedNextTokens = followers
                    });
                }

                instance.Parameters = parameters;

                return instance;
            }
            catch (WqlParseException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.function_unknown",
                    nameToken
                );
            }
        }

        /// <summary>
        /// Parses an order-by clause.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <returns>The order node or null.</returns>
        private WqlExpressionNodeOrder<TIndexItem> ParseOrder(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors)
        {
            IWqlToken orderToken;

            if (PeekToken(tokenQueue, "order", "by"))
            {
                orderToken = new WqlTokenCombine(ReadToken(tokenQueue, "order"), ReadToken(tokenQueue, "by"));
            }
            else if (PeekToken(tokenQueue, "orderby") || PeekToken(tokenQueue, "order"))
            {
                orderToken = ReadToken(tokenQueue);
            }
            else
            {
                return null;
            }

            var attributes = new List<WqlExpressionNodeOrderAttribute<TIndexItem>>();
            var i = 0;

            ilaQueue.Enqueue(new WqlLookaheadToken(orderToken, WqlExpressionType.Order)
            {
                ExpectedNextTokens = [WqlExpressionType.Attribute]
            });

            attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue, errors));

            while (PeekToken(tokenQueue, ","))
            {
                var separatorToken = ReadToken(tokenQueue, ",");

                ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute]
                });

                attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue, errors));
            }

            return new WqlExpressionNodeOrder<TIndexItem> { Attributes = attributes };
        }

        /// <summary>
        /// Parses a single order attribute with optional direction.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="position">The attribute position in the order list.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        /// <returns>The order attribute node.</returns>
        private WqlExpressionNodeOrderAttribute<TIndexItem> ParseOrderAttribute(Queue<WqlToken> tokenQueue, int position, Queue<WqlLookaheadToken> ilaQueue, List<WqlExpressionError> errors)
        {
            var attributeToken = PeekToken(tokenQueue)
                ?? throw new WqlParseException
                (
                    "webexpress.webindex:wql.attribute_unknown",
                    (IWqlToken)null
                );

            ReadToken(tokenQueue);

            var attribute = CreateAttribute(attributeToken);

            if (attribute.Type is null)
            {
                errors.Add(CreateError("webexpress.webindex:wql.attribute_unknown", attributeToken));
            }

            ilaQueue.Enqueue(new WqlLookaheadToken(attributeToken, WqlExpressionType.Attribute)
            {
                ExpectedNextTokens = [WqlExpressionType.Separator, WqlExpressionType.OrderDirection, WqlExpressionType.PartitioningOperator],
            });

            var descending = ParseDescendingOrder(tokenQueue, ilaQueue);

            return new WqlExpressionNodeOrderAttribute<TIndexItem>
            {
                Attribute = attribute,
                Descending = descending,
                Position = position
            };
        }

        /// <summary>
        /// Parses an optional descending/ascending direction.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <returns>True if descending, otherwise false (ascending/default).</returns>
        private static bool ParseDescendingOrder(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            if (PeekToken(tokenQueue, "asc"))
            {
                var orderToken = ReadToken(tokenQueue, "asc");

                ilaQueue.Enqueue(new WqlLookaheadToken(orderToken, WqlExpressionType.OrderDirection)
                {
                    ExpectedNextTokens = [WqlExpressionType.PartitioningOperator],
                });

                return false;
            }
            else if (PeekToken(tokenQueue, "desc"))
            {
                var orderToken = ReadToken(tokenQueue, "desc");

                ilaQueue.Enqueue(new WqlLookaheadToken(orderToken, WqlExpressionType.OrderDirection)
                {
                    ExpectedNextTokens = [WqlExpressionType.PartitioningOperator],
                });

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a partitioning clause (skip/take).
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The partitioning node or null.</returns>
        private static WqlExpressionNodePartitioning<TIndexItem> ParsePartitioning(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var function = new List<WqlExpressionNodePartitioningFunction<TIndexItem>>();

            if (!PeekToken(tokenQueue, "take") && !PeekToken(tokenQueue, "skip"))
            {
                return null;
            }

            while (PeekToken(tokenQueue, "take") || PeekToken(tokenQueue, "skip"))
            {
                var op = ParsePartitioningOperator(tokenQueue, ilaQueue);

                var valueToken = PeekToken(tokenQueue)
                    ?? throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_value",
                        []
                    );

                var number = ParseNumberValue(tokenQueue);

                ilaQueue.Enqueue(new WqlLookaheadToken(valueToken, WqlExpressionType.Partitioning)
                {
                    ExpectedNextTokens = [WqlExpressionType.PartitioningOperator],
                });

                function.Add(new WqlExpressionNodePartitioningFunction<TIndexItem>()
                {
                    Operator = op,
                    Value = number
                });
            }

            return new WqlExpressionNodePartitioning<TIndexItem>()
            {
                PartitioningFunctions = function
            };
        }

        /// <summary>
        /// Parses a partitioning operator token.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The partitioning operator.</returns>
        private static WqlExpressionNodePartitioningOperator ParsePartitioningOperator(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var partitioningOperatorToken = PeekToken(tokenQueue);

            if (partitioningOperatorToken?.Value == "take")
            {
                var operatorToken = ReadToken(tokenQueue, "take");

                ilaQueue.Enqueue(new WqlLookaheadToken(operatorToken, WqlExpressionType.PartitioningOperator)
                {
                    ExpectedNextTokens = [WqlExpressionType.Partitioning],
                });

                return WqlExpressionNodePartitioningOperator.Take;
            }
            else if (partitioningOperatorToken?.Value == "skip")
            {
                var operatorToken = ReadToken(tokenQueue, "skip");

                ilaQueue.Enqueue(new WqlLookaheadToken(operatorToken, WqlExpressionType.PartitioningOperator)
                {
                    ExpectedNextTokens = [WqlExpressionType.Partitioning],
                });

                return WqlExpressionNodePartitioningOperator.Skip;
            }

            throw new WqlParseException
            (
                "webexpress.webindex:wql.expected_skip_or_take",
                partitioningOperatorToken
            );
        }

        /// <summary>
        /// Parses a string literal or unquoted string token.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The string value.</returns>
        private static string ParseStringValue(Queue<WqlToken> tokenQueue)
        {
            var valueToken = ReadToken(tokenQueue);

            return valueToken?.Value;
        }

        /// <summary>
        /// Parses a number literal using the parser's culture. Besides the double, the number
        /// is kept as a decimal where possible, so that integers beyond the precision of a
        /// double convert exactly.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The value node.</returns>
        private WqlExpressionNodeValue<TIndexItem> ParseNumberLiteral(Queue<WqlToken> tokenQueue)
        {
            var token = PeekToken(tokenQueue);
            var number = ParseDoubleValue(tokenQueue);

            return new WqlExpressionNodeValue<TIndexItem>()
            {
                Culture = Culture,
                NumberValue = number,
                DecimalValue = decimal.TryParse(token.Value, NumberStyles.Float, Culture, out var exact) ? exact : null,
                Token = token
            };
        }

        /// <summary>
        /// Parses a double-precision number using the parser's culture.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The numeric value.</returns>
        private double ParseDoubleValue(Queue<WqlToken> tokenQueue)
        {
            var token = ReadToken(tokenQueue, DoubleRegex());

            try
            {
                return Convert.ToDouble(token.Value, Culture);
            }
            catch (Exception)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.parse.exception",
                    token
                );
            }
        }

        /// <summary>
        /// Parses an integer number.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The integer value.</returns>
        /// <exception cref="WqlParseException">The value is no number or exceeds the range of an integer.</exception>
        private static int ParseNumberValue(Queue<WqlToken> tokenQueue)
        {
            var token = ReadToken(tokenQueue, NumberRegex());

            if (!int.TryParse(token?.Value, out var number))
            {
                throw new WqlParseException("webexpress.webindex:wql.parse.exception", token);
            }

            return number;
        }

        /// <summary>
        /// Parses the value of a fuzzy similarity option. The similarity is a percentage: 1 to 99
        /// searches fuzzily, 0 and 100 match exactly. A value outside of that range has no
        /// meaning, and interpreting it - as an exact search, say - would hand the caller results
        /// for a query other than the one written, so it is refused.
        /// </summary>
        /// <param name="token">The token that carries the value, reported on failure.</param>
        /// <param name="digits">The digits of the value.</param>
        /// <returns>The similarity in percent.</returns>
        /// <exception cref="WqlParseException">The value lies outside of 0 to 100.</exception>
        private static uint ParseSimilarity(WqlToken token, string digits)
        {
            if (!uint.TryParse(digits, out var similarity) || similarity > 100)
            {
                throw new WqlParseException("webexpress.webindex:wql.invalid_similarity", token);
            }

            return similarity;
        }

        /// <summary>
        /// Parses the value of a distance option. The pattern of the token admits any number of
        /// digits, so a value beyond the range of a distance is refused as a syntax error rather
        /// than surfacing as an overflow the caller does not expect from a query.
        /// </summary>
        /// <param name="token">The token that carries the value, reported on failure.</param>
        /// <param name="digits">The digits of the value.</param>
        /// <returns>The distance in words.</returns>
        /// <exception cref="WqlParseException">The value exceeds the range of a distance.</exception>
        private static uint ParseDistance(WqlToken token, string digits)
        {
            if (!uint.TryParse(digits, out var distance))
            {
                throw new WqlParseException("webexpress.webindex:wql.invalid_distance", token);
            }

            return distance;
        }

        /// <summary>
        /// Checks a condition once it is built, for errors the grammar cannot express: an
        /// unknown attribute, a literal that does not convert into the type of the attribute,
        /// options on an operator without a text meaning, a value other than null, true or
        /// false after is and is not, and a function called with too few or too many
        /// arguments. Every error is reported with its position in the statement.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        private void ValidateCondition(WqlExpressionNodeFilterCondition<TIndexItem> condition, List<WqlExpressionError> errors)
        {
            if (condition.Attribute is not null && condition.Attribute.Type is null)
            {
                // the remaining checks depend on the type of the attribute
                errors.Add(CreateError("webexpress.webindex:wql.attribute_unknown", condition.Attribute.Token));

                return;
            }

            ValidateFunction(condition.Function, errors);

            var leftType = condition.LeftType;

            switch (condition)
            {
                case WqlExpressionNodeFilterConditionBinary<TIndexItem> binary:
                    {
                        if (binary.RequiresText && !IsTextType(leftType))
                        {
                            errors.Add(CreateError("webexpress.webindex:wql.text_required", binary.OperatorToken));
                        }

                        if ((binary.Options?.Similarity.HasValue == true || binary.Options?.Distance.HasValue == true) && !binary.AcceptsOptions)
                        {
                            errors.Add(CreateError("webexpress.webindex:wql.option_not_applicable", binary.Options.Tokens?.FirstOrDefault()));
                        }

                        if (binary is WqlExpressionNodeFilterConditionBinaryIs<TIndexItem> or WqlExpressionNodeFilterConditionBinaryIsNot<TIndexItem>)
                        {
                            ValidateIsValue(binary.Parameter, leftType, binary.OperatorToken, errors);
                        }
                        else
                        {
                            ValidateParameter(binary.Parameter, binary.RequiresText ? typeof(string) : leftType, errors);
                        }

                        break;
                    }
                case WqlExpressionNodeFilterConditionSet<TIndexItem> set:
                    {
                        foreach (var parameter in set.Parameters ?? [])
                        {
                            ValidateParameter(parameter, leftType, errors);
                        }

                        break;
                    }
                case WqlExpressionNodeFilterConditionRange<TIndexItem> range:
                    {
                        ValidateParameter(range.Lower, leftType, errors);
                        ValidateParameter(range.Upper, leftType, errors);

                        break;
                    }
            }
        }

        /// <summary>
        /// Checks the value after is or is not: null on every attribute, true and false on a
        /// boolean one.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="leftType">The type of the left-hand side.</param>
        /// <param name="operatorToken">The operator, reported if the value is missing.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        private void ValidateIsValue(WqlExpressionNodeParameter<TIndexItem> parameter, Type leftType, IWqlToken operatorToken, List<WqlExpressionError> errors)
        {
            var value = parameter?.Function is null && parameter?.Attribute is null ? parameter?.Value : null;
            var type = Nullable.GetUnderlyingType(leftType ?? typeof(object)) ?? leftType;
            var valid = value is not null &&
                (value.IsNull || value.BoolValue.HasValue && (type == typeof(bool) || type == typeof(object)));

            if (!valid)
            {
                errors.Add(CreateError("webexpress.webindex:wql.invalid_is_value", value?.Token ?? operatorToken));
            }
        }

        /// <summary>
        /// Checks that a literal converts into the type it is compared with, and checks the
        /// functions within the parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="targetType">The type of the left-hand side.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        private void ValidateParameter(WqlExpressionNodeParameter<TIndexItem> parameter, Type targetType, List<WqlExpressionError> errors)
        {
            if (parameter is null)
            {
                return;
            }

            if (parameter.Function is not null)
            {
                ValidateFunction(parameter.Function, errors);

                return;
            }

            if (parameter.Attribute is not null || parameter.Value is null || parameter.Value.IsNull ||
                targetType is null || targetType == typeof(object))
            {
                // a null literal compares with every type, since null propagates through paths
                return;
            }

            if (!WqlValueConverter.TryConvert(parameter.Value.GetExactValue(), targetType, Culture, out _))
            {
                errors.Add(CreateError("webexpress.webindex:wql.literal_not_convertible", parameter.Value.Token));
            }
        }

        /// <summary>
        /// Checks the number of arguments of a function and of the functions nested in it.
        /// </summary>
        /// <param name="function">The function, or null.</param>
        /// <param name="errors">The list that collects the semantic errors.</param>
        private void ValidateFunction(WqlExpressionNodeFilterFunction<TIndexItem> function, List<WqlExpressionError> errors)
        {
            if (function is null)
            {
                return;
            }

            var count = function.Parameters?.Count() ?? 0;

            if (count < function.MinArguments || count > function.MaxArguments)
            {
                errors.Add(CreateError("webexpress.webindex:wql.function_arity", function.Token));
            }

            foreach (var parameter in function.Parameters ?? [])
            {
                ValidateFunction(parameter.Function, errors);
            }
        }

        /// <summary>
        /// Determines whether a type is text, or a function value whose type is only known
        /// when it runs.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True for string and object, otherwise false.</returns>
        private static bool IsTextType(Type type)
        {
            return type == typeof(string) || type == typeof(object);
        }

        /// <summary>
        /// Creates a semantic error located at a token.
        /// </summary>
        /// <param name="message">The message key.</param>
        /// <param name="token">The token in error, or null.</param>
        /// <returns>The error.</returns>
        private WqlExpressionError CreateError(string message, IWqlToken token)
        {
            return new WqlExpressionError()
            {
                Culture = Culture,
                Message = message,
                Position = token?.Offset ?? 0,
                Length = token?.Length ?? 0
            };
        }

        /// <summary>
        /// Tokenizes the input string into a queue of WQL tokens.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The token queue.</returns>
        private static Queue<WqlToken> Tokenize(string input)
        {
            var tokens = new Queue<WqlToken>();

            if (string.IsNullOrEmpty(input))
            {
                return tokens;
            }

            var currentToken = new WqlToken();

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (char.IsWhiteSpace(c))
                {
                    if (!currentToken.IsEmpty)
                    {
                        tokens.Enqueue(currentToken);

                        // collapse consecutive spaces
                        while (i < input.Length - 1 && input[i + 1] == ' ')
                        {
                            i++;
                        }
                    }

                    currentToken = new WqlToken() { Offset = i + 1 };
                }
                else if (c == ',' || c == '(' || c == ')')
                {
                    if (!currentToken.IsEmpty)
                    {
                        tokens.Enqueue(currentToken);
                    }

                    tokens.Enqueue(new WqlToken() { Value = c.ToString(), Offset = i });
                    currentToken = new WqlToken() { Offset = i + 1 };
                }
                else if (c == '=' || c == '~' || c == '<' || c == '>' || c == '!' || c == '%' || c == ':' || c == '|' || c == '&')
                {
                    if (!currentToken.IsEmpty)
                    {
                        var lastCharacter = currentToken.Value.LastOrDefault();

                        if (!(lastCharacter == '=' ||
                              lastCharacter == '~' ||
                              lastCharacter == '<' ||
                              lastCharacter == '>' ||
                              lastCharacter == '!' ||
                              lastCharacter == '%' ||
                              lastCharacter == ':' ||
                              lastCharacter == '|' ||
                              lastCharacter == '&'))
                        {
                            tokens.Enqueue(currentToken);
                            currentToken = new WqlToken() { Offset = i + 1 };
                        }
                    }

                    currentToken.Append(c);
                }
                else if (c == '"' || c == '\'')
                {
                    var startChar = c;

                    if (!currentToken.IsEmpty)
                    {
                        tokens.Enqueue(currentToken);
                    }

                    // opening quote token, anchored at the quote character
                    currentToken = new WqlToken() { Offset = i };
                    currentToken.Append(c);
                    tokens.Enqueue(currentToken);

                    i++;
                    currentToken = new WqlToken() { Offset = i };

                    // read content until closing quote; a doubled quote stands for the quote
                    // itself, as in 'O''Brien'
                    while (i < input.Length && (input[i] != startChar || i + 1 < input.Length && input[i + 1] == startChar))
                    {
                        currentToken.Append(input[i]);
                        i += input[i] == startChar ? 2 : 1;
                    }

                    if (i < input.Length)
                    {
                        tokens.Enqueue(currentToken);

                        // closing quote token, anchored at the closing quote character
                        currentToken = new WqlToken() { Offset = i };
                        currentToken.Append(input[i]);
                        tokens.Enqueue(currentToken);
                        currentToken = new WqlToken() { Offset = i + 1 };
                    }
                    else
                    {
                        // unterminated string - enqueue whatever was collected so parsing
                        // can still produce a meaningful error message
                        if (!currentToken.IsEmpty)
                        {
                            tokens.Enqueue(currentToken);
                        }

                        currentToken = new WqlToken() { Offset = i };
                    }
                }
                else
                {
                    var lastCharacter = currentToken.Value?.LastOrDefault();

                    if (lastCharacter == '=' ||
                        lastCharacter == '~' ||
                        lastCharacter == '<' ||
                        lastCharacter == '>' ||
                        lastCharacter == '!' ||
                        lastCharacter == '%' ||
                        lastCharacter == ':' ||
                        lastCharacter == '|' ||
                        lastCharacter == '&')
                    {
                        tokens.Enqueue(currentToken);
                        currentToken = new WqlToken() { Offset = i + 1 };
                    }

                    currentToken.Append(c);
                }
            }

            if (!currentToken.IsEmpty)
            {
                tokens.Enqueue(currentToken);
            }

            return tokens;
        }

        /// <summary>
        /// Peeks whether the next tokens form a function call - a name followed by an opening
        /// parenthesis - which may stand on the left-hand side of a condition.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>True if a function call follows, otherwise false.</returns>
        private static bool PeekFunctionCall(Queue<WqlToken> tokenQueue)
        {
            return tokenQueue.Count > 1 &&
                NameRegex().IsMatch(tokenQueue.Peek().Value ?? "") &&
                tokenQueue.ElementAt(1).Value == "(";
        }

        /// <summary>
        /// Peeks whether the next token matches the given literal (case-insensitive).
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="currentToken">The literal to check.</param>
        /// <returns>True if matched; otherwise false.</returns>
        private static bool PeekToken(Queue<WqlToken> tokenQueue, string currentToken)
        {
            return tokenQueue.Count > 0 && tokenQueue.Peek().Value?.ToLower() == currentToken?.ToLower();
        }

        /// <summary>
        /// Peeks whether the next tokens match the given sequence of literals (order-sensitive, case-insensitive).
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="tokens">The literal sequence to check.</param>
        /// <returns>True if the sequence matches; otherwise false.</returns>
        private static bool PeekToken(Queue<WqlToken> tokenQueue, params string[] tokens)
        {
            if (tokenQueue.Count < tokens.Length)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                var val = tokenQueue.ElementAt(i).Value?.ToLower();
                if (val != tokens[i]?.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Peeks whether the next token matches the given regex.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="regex">The regular expression.</param>
        /// <returns>True if matched; otherwise false.</returns>
        private static bool PeekToken(Queue<WqlToken> tokenQueue, Regex regex)
        {
            return tokenQueue.Count > 0 && regex.IsMatch(tokenQueue.Peek().Value?.ToLower() ?? "");
        }

        /// <summary>
        /// Returns the next token without consuming it.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The next token or null.</returns>
        private static WqlToken PeekToken(Queue<WqlToken> tokenQueue)
        {
            return tokenQueue.Count != 0 ? tokenQueue.Peek() : null;
        }

        /// <summary>
        /// Reads the current token.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The token read or null.</returns>
        private static WqlToken ReadToken(Queue<WqlToken> tokenQueue)
        {
            return tokenQueue.Count != 0 ? tokenQueue.Dequeue() : null;
        }

        /// <summary>
        /// Reads and validates the current token against a literal.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="token">The expected literal.</param>
        /// <returns>The token read.</returns>
        private static WqlToken ReadToken(Queue<WqlToken> tokenQueue, string token)
        {
            if (PeekToken(tokenQueue, token))
            {
                return tokenQueue.Dequeue();
            }

            throw new WqlParseException
            (
                "webexpress.webindex:wql.expected_token",
                PeekToken(tokenQueue)
            );
        }

        /// <summary>
        /// Reads and validates a sequence of literal tokens in order.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="tokens">The expected sequence.</param>
        /// <returns>The tokens read.</returns>
        private static IEnumerable<WqlToken> ReadToken(Queue<WqlToken> tokenQueue, params string[] tokens)
        {
            foreach (var token in tokens)
            {
                if (PeekToken(tokenQueue, token))
                {
                    yield return tokenQueue.Dequeue();
                }
                else
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.expected_token",
                        PeekToken(tokenQueue)
                    );
                }
            }
        }

        /// <summary>
        /// Reads and validates the current token against a regex.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="regex">The regular expression.</param>
        /// <returns>The token read.</returns>
        private static WqlToken ReadToken(Queue<WqlToken> tokenQueue, Regex regex)
        {
            if (PeekToken(tokenQueue, regex))
            {
                return tokenQueue.Dequeue();
            }

            throw new WqlParseException
            (
                "webexpress.webindex:wql.expected_token_matching",
                PeekToken(tokenQueue)
            );
        }

        /// <summary>
        /// Registers a condition type by its operator token.
        /// </summary>
        /// <typeparam name="TCondition">The condition type.</typeparam>
        public void RegisterCondition<TCondition>()
            where TCondition : IWqlExpressionNodeFilterCondition<TIndexItem>, new()
        {
            var op = new TCondition().Operator;

            if (!Conditions.ContainsKey(op))
            {
                Conditions.Add(op, typeof(TCondition));
                return;
            }

            throw new Exception($"Condition '{op}' cannot be registered because it already exists.");
        }

        /// <summary>
        /// Registers a function type by its name token.
        /// </summary>
        /// <typeparam name="TFunction">The function type.</typeparam>
        public void RegisterFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new()
        {
            var name = new TFunction().Name?.ToLower();

            if (!Functions.ContainsKey(name))
            {
                Functions.Add(name, typeof(TFunction));
                return;
            }

            throw new Exception($"Function '{name}' cannot be registered because it already exists.");
        }

        /// <summary>
        /// Removes a registered condition by operator.
        /// </summary>
        /// <param name="op">The operator.</param>
        public void RemoveCondition(string op)
        {
            Conditions.Remove(op);
        }

        /// <summary>
        /// Removes a registered function by name.
        /// </summary>
        /// <param name="name">The function name.</param>
        public void RemoveFunction(string name)
        {
            Functions.Remove(name);
        }

        /// <summary>
        /// Recursively extracts field metadata from a type for attribute lookup.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="prefix">The parent prefix for nested properties.</param>
        /// <param name="processedTypes">A set to prevent cycles.</param>
        /// <returns>An enumeration of field metadata.</returns>
        private static IEnumerable<IndexFieldData> GetFieldData(Type type, string prefix = "", HashSet<Type> processedTypes = null)
        {
            processedTypes ??= [];

            if (processedTypes.Contains(type))
            {
                yield break;
            }

            processedTypes.Add(type);

            foreach (var property in type.GetProperties())
            {
                string propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                yield return new IndexFieldData
                {
                    Name = propertyName,
                    Type = property.PropertyType,
                    PropertyInfo = property
                };

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    foreach (var subProperty in GetFieldData(property.PropertyType, propertyName, processedTypes))
                    {
                        yield return subProperty;
                    }
                }
            }

            // path-based cycle detection: release the type so sibling properties
            // of the same type still contribute their nested fields
            processedTypes.Remove(type);
        }
    }
}
