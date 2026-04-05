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
    /// <Filter>                   ::= "(" <Filter> ")" | <Filter> <LogicalOperator> <Filter> | <Condition> | ε
    /// <Condition>                ::= <Attribute> <BinaryOperator> <Parameter> <ParameterOptions> | <Attribute> <SetOperator> "(" <Parameter> <ParameterNext> ")"
    /// <LogicalOperator>          ::= "and" | "or" | "&" | "||"
    /// <Attribute>                ::= <Name> | <Name> "." <Name>
    /// <Parameter>                ::= <Function> | <DoubleValue> | """ <StringValue> """ | "'" <StringValue> "'" | <StringValue>
    /// <ParameterOptions>         ::= <ParameterFuzzyOptions> | <ParameterDistanceOptions> | <ParameterFuzzyOptions> <ParameterDistanceOptions> | <ParameterDistanceOptions> <ParameterFuzzyOptions> | ε
    /// <ParameterFuzzyOptions>    ::= "~" <Number>
    /// <ParameterDistanceOptions> ::= ":" <Number>
    /// <Function>                 ::= <Name> "(" <Parameter> <ParameterNext> ")" | <Name> "(" ")"
    /// <ParameterNext>            ::= "," <Parameter> <ParameterNext> | ε
    /// <BinaryOperator>           ::= "=" | ">" | "<" | ">=" | "<=" | "!=" | "~" | "is" | "is not"
    /// <SetOperator>              ::= "in" | "not in"
    /// <Order>                    ::= "order" "by" <Attribute> <DescendingOrder> <OrderNext> | ε
    /// <OrderNext>                ::= "," <Attribute> <DescendingOrder> <OrderNext> | ε
    /// <DescendingOrder>          ::= "asc" | "desc" | ε
    /// <Partitioning>             ::= <Partitioning> <Partitioning> | <PartitioningOperator> <Number> | ε
    /// <PartitioningOperator>     ::= "take" | "skip"
    /// <Name>                     ::= [A-Za-z_][A-Za-z0-9_]+
    /// <StringValue>              ::= [A-Za-z0-9_@<>=~$%/!+.,;:\-]+
    /// <DoubleValue>              ::= [+-]?[0-9]*[.]?[0-9]+
    /// <Number>                   ::= [0-9]+
    /// ]]>
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
            Attributes = GetFieldData(typeof(TIndexItem));

            RegisterCondition<WqlExpressionNodeFilterConditionBinaryEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLike<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryGreaterThan<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryGreaterThanOrEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLessThan<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionBinaryLessThanOrEqual<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionSetIn<TIndexItem>>();
            RegisterCondition<WqlExpressionNodeFilterConditionSetNotIn<TIndexItem>>();

            RegisterFunction<WqlExpressionNodeFilterFunctionDay<TIndexItem>>();
            RegisterFunction<WqlExpressionNodeFilterFunctionNow<TIndexItem>>();
        }

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
        public IWqlLookahead Analyze(string input)
        {
            var ilaQueue = new Queue<WqlLookaheadToken>();

            try
            {
                var tokens = Tokenize(input);

                // try parsing the current slice.
                Parse(input, tokens, ilaQueue);
            }
            catch (WqlParseException)
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
        /// Parses an input WQL string into an abstract syntax tree representation.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <returns>The parsed statement with possible error information.</returns>
        public IWqlStatement<TIndexItem> Parse(string input)
        {
            var ilaQueue = new Queue<WqlLookaheadToken>();

            try
            {
                var tokens = Tokenize(input);

                return Parse(input, tokens, ilaQueue);
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
        }

        /// <summary>
        /// Parses an input WQL string into an abstract syntax tree representation.
        /// </summary>
        /// <param name="input">The input WQL string.</param>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The parsed statement with possible error information.</returns>
        private IWqlStatement<TIndexItem> Parse(string input, Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
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

            wql.Filter = ParseFilter(tokenQueue, ilaQueue);
            wql.Order = ParseOrder(tokenQueue, ilaQueue);
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
        /// <returns>The filter node or null.</returns>
        private WqlExpressionNodeFilter<TIndexItem> ParseFilter(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, int deep = 0)
        {
            if (PeekToken(tokenQueue, "order") ||
                PeekToken(tokenQueue, "orderby") ||
                PeekToken(tokenQueue, "take") ||
                PeekToken(tokenQueue, "skip"))
            {
                return null;
            }

            if (PeekToken(tokenQueue, "("))
            {
                var openToken = ReadToken(tokenQueue, "(");
                ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.OpenParenthesis)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
                });

                var filter = ParseFilter(tokenQueue, ilaQueue, deep++);

                var closeToken = ReadToken(tokenQueue, ")")
                    ?? throw new WqlParseException
                    (
                        "webexpress.webindex:wql.wql.expected_close_parenthesis",
                        []
                    );

                ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                {
                    ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.PartitioningOperator]
                });

                if (PeekToken(tokenQueue, "and") ||
                    PeekToken(tokenQueue, "&") ||
                    PeekToken(tokenQueue, "or") ||
                    PeekToken(tokenQueue, "||"))
                {
                    var logicalToken = PeekToken(tokenQueue);
                    var logicalOperator = ParseLogicalOperator(tokenQueue, ilaQueue);

                    ilaQueue.Enqueue(new WqlLookaheadToken(logicalToken, WqlExpressionType.LogicalOperator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
                    });

                    return new WqlExpressionNodeFilterBinary<TIndexItem>
                    {
                        LeftFilter = filter,
                        LogicalOperator = logicalOperator,
                        RightFilter = ParseFilter(tokenQueue, ilaQueue)
                    };
                }

                return filter;
            }

            var condition = ParseCondition(tokenQueue, ilaQueue);

            if (condition is not null)
            {
                if (PeekToken(tokenQueue, "and") ||
                    PeekToken(tokenQueue, "&") ||
                    PeekToken(tokenQueue, "or") ||
                    PeekToken(tokenQueue, "||"))
                {
                    var logicalToken = PeekToken(tokenQueue);
                    var logicalOperator = ParseLogicalOperator(tokenQueue, ilaQueue);

                    ilaQueue.Enqueue(new WqlLookaheadToken(logicalToken, WqlExpressionType.LogicalOperator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Attribute, WqlExpressionType.OpenParenthesis]
                    });

                    return new WqlExpressionNodeFilterBinary<TIndexItem>
                    {
                        LeftFilter = new WqlExpressionNodeFilter<TIndexItem> { Condition = condition },
                        LogicalOperator = logicalOperator,
                        RightFilter = ParseFilter(tokenQueue, ilaQueue)
                    };
                }

                return new WqlExpressionNodeFilter<TIndexItem>
                {
                    Condition = condition
                };
            }

            var leftFilter = ParseFilter(tokenQueue, ilaQueue);
            if (leftFilter is not null)
            {
                var logicalOperator = ParseLogicalOperator(tokenQueue, ilaQueue);
                var rightFilter = ParseFilter(tokenQueue, ilaQueue);

                return new WqlExpressionNodeFilterBinary<TIndexItem>
                {
                    LeftFilter = leftFilter,
                    LogicalOperator = logicalOperator,
                    RightFilter = rightFilter
                };
            }

            return null;
        }

        /// <summary>
        /// Parses a single condition expression.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The condition node.</returns>
        private WqlExpressionNodeFilterCondition<TIndexItem> ParseCondition(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var attribute = ParseAttribute(tokenQueue, ilaQueue);

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
                var instance = Activator.CreateInstance(condition.Value) as WqlExpressionNodeFilterCondition<TIndexItem>;

                if (instance is WqlExpressionNodeFilterConditionBinary<TIndexItem> binary)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(new WqlTokenCombine(operationTokens), WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    binary.Culture = Culture;
                    binary.Attribute = attribute;

                    binary.Parameter = ParseParameter(tokenQueue, ilaQueue, true);
                    binary.Options = ParseParameterOptions(tokenQueue, ilaQueue);

                    return binary;
                }
                else if (instance is WqlExpressionNodeFilterConditionSet<TIndexItem> set)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(new WqlTokenCombine(operationTokens), WqlExpressionType.Operator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.OpenParenthesis]
                    });

                    var parameters = new List<WqlExpressionNodeParameter<TIndexItem>>();
                    var openToken = ReadToken(tokenQueue, "(");

                    ilaQueue.Enqueue(new WqlLookaheadToken(openToken, WqlExpressionType.OpenParenthesis)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Parameter]
                    });

                    parameters.Add(ParseParameter(tokenQueue, ilaQueue, true));

                    while (PeekToken(tokenQueue, ","))
                    {
                        var separatorToken = ReadToken(tokenQueue, ",");

                        ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                        {
                            ExpectedNextTokens = [WqlExpressionType.Parameter, WqlExpressionType.Separator]
                        });

                        parameters.Add(ParseParameter(tokenQueue, ilaQueue, false));
                    }

                    var closeToken = ReadToken(tokenQueue, ")");

                    ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                    {
                        ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.Partitioning]
                    });

                    set.Culture = Culture;
                    set.Attribute = attribute;
                    set.Parameters = parameters;

                    return set;
                }

                throw new WqlParseException
                (
                    "webexpress.webindex:wql.expected_binary_or_set_condition",
                    operationTokens
                );
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
        /// Parses an attribute reference.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The attribute node.</returns>
        private WqlExpressionNodeAttribute<TIndexItem> ParseAttribute(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            var attributeToken = PeekToken(tokenQueue);

            try
            {
                if (attributeToken is null)
                {
                    throw new WqlParseException
                    (
                        "webexpress.webindex:wql.attribute_unknown",
                        attributeToken
                    );
                }

                ReadToken(tokenQueue);

                var path = WqlPropertyPath<TIndexItem>.Parse(attributeToken.Value);
                var property = path.Resolve(typeof(TIndexItem));

                if (property is not null)
                {
                    ilaQueue.Enqueue(new WqlLookaheadToken(attributeToken, WqlExpressionType.Attribute)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Operator],
                    });

                    return new WqlExpressionNodeAttribute<TIndexItem>
                    {
                        Name = path.ToString()
                    };
                }

                throw new WqlParseException
                (
                    "webexpress.webindex:wql.attribute_unknown",
                    attributeToken
                );
            }
            catch (WqlParseException)
            {
                throw;
            }
            catch (Exception)
            {
                ilaQueue.Enqueue(new WqlLookaheadToken(attributeToken, WqlExpressionType.Attribute)
                {
                    ExpectedNextTokens = [WqlExpressionType.Operator],
                });

                return new WqlExpressionNodeAttribute<TIndexItem>
                {
                    Name = attributeToken.Value
                };
            }
        }

        /// <summary>
        /// Parses a parameter node which can be a function call, a number, or a string.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">The lookahead token queue.</param>
        /// <param name="isScalar"> 
        /// Indicates whether the parameter is expected to be a scalar value. 
        /// If false, the parser may accept or construct a list of values. 
        /// </param>
        /// <returns>The parameter node.</returns>
        private WqlExpressionNodeParameter<TIndexItem> ParseParameter(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue, bool isScalar)
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
                    Function = ParseFunction(tokenQueue, ilaQueue)
                };
            }
            else if (PeekToken(tokenQueue, DoubleRegex()))
            {
                return new WqlExpressionNodeParameter<TIndexItem>
                {
                    Value = new WqlExpressionNodeValue<TIndexItem>()
                    {
                        Culture = Culture,
                        NumberValue = ParseDoubleValue(tokenQueue)
                    }
                };
            }
            else if (functionOrValueToken?.Value == "\"")
            {
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
                        StringValue = value
                    }
                };

                ilaQueue.Enqueue(new WqlLookaheadToken(valueToken, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = [WqlExpressionType.Quotation]
                });

                if (!PeekToken(tokenQueue, "\""))
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
                    ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.Partitioning]
                });

                return parameter;
            }
            else if (functionOrValueToken?.Value == "'")
            {
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
                        StringValue = value
                    }
                };

                ilaQueue.Enqueue(new WqlLookaheadToken(valueToken, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = [WqlExpressionType.Quotation]
                });

                if (!PeekToken(tokenQueue, "'"))
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
                    ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.Partitioning]
                });

                return parameter;
            }
            else if (functionOrValueToken?.Value == "(")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else if (functionOrValueToken?.Value == ")")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else if (functionOrValueToken?.Value == "and")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else if (functionOrValueToken?.Value == "&")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else if (functionOrValueToken?.Value == "or")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else if (functionOrValueToken?.Value == "||")
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.unexpected_token",
                    functionOrValueToken
                );
            }
            else
            {
                var stringToken = PeekToken(tokenQueue);
                var value = ParseStringValue(tokenQueue);

                ilaQueue.Enqueue(new WqlLookaheadToken(stringToken, WqlExpressionType.Parameter)
                {
                    ExpectedNextTokens = [WqlExpressionType.LogicalOperator]
                });

                return new WqlExpressionNodeParameter<TIndexItem>
                {
                    Value = new WqlExpressionNodeValue<TIndexItem>()
                    {
                        Culture = Culture,
                        StringValue = value
                    }
                };
            }
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

            // fuzzy can be written as "~" <number> or as a single token "~<number>"
            if (PeekToken(tokenQueue, FuzzyRegex()))
            {
                options.Similarity = (uint)ParseFuzzyValue(tokenQueue);
            }
            else if (PeekToken(tokenQueue, "~"))
            {
                var token = ReadToken(tokenQueue);

                if (PeekToken(tokenQueue, NumberRegex()))
                {
                    options.Similarity = (uint)ParseNumberValue(tokenQueue);
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
                options.Distance = (uint)ParseDistanceValue(tokenQueue);
            }
            else if (PeekToken(tokenQueue, ":"))
            {
                var token = ReadToken(tokenQueue);

                if (PeekToken(tokenQueue, NumberRegex()))
                {
                    options.Distance = (uint)ParseNumberValue(tokenQueue);
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

            return options;
        }

        /// <summary>
        /// Parses a function invocation and its parameters.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The function node.</returns>
        private WqlExpressionNodeFilterFunction<TIndexItem> ParseFunction(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
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
                        ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.Partitioning]
                    });
                }
                else
                {
                    parameters.Add(ParseParameter(tokenQueue, ilaQueue, true));

                    while (PeekToken(tokenQueue, ","))
                    {
                        var separatorToken = ReadToken(tokenQueue, ",");
                        tokenList.Add(separatorToken);

                        ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                        {
                            ExpectedNextTokens = [WqlExpressionType.Parameter, WqlExpressionType.Function]
                        });

                        parameters.Add(ParseParameter(tokenQueue, ilaQueue, true));
                    }

                    var closeToken = ReadToken(tokenQueue, ")");
                    tokenList.Add(closeToken);

                    ilaQueue.Enqueue(new WqlLookaheadToken(closeToken, WqlExpressionType.CloseParenthesis)
                    {
                        ExpectedNextTokens = [WqlExpressionType.LogicalOperator, WqlExpressionType.Order, WqlExpressionType.PartitioningOperator]
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
        /// <returns>The order node or null.</returns>
        private WqlExpressionNodeOrder<TIndexItem> ParseOrder(Queue<WqlToken> tokenQueue, Queue<WqlLookaheadToken> ilaQueue)
        {
            if (PeekToken(tokenQueue, "order", "by"))
            {
                var attributes = new List<WqlExpressionNodeOrderAttribute<TIndexItem>>();
                var i = 0;

                var orderToken = ReadToken(tokenQueue, "order");
                var byToken = ReadToken(tokenQueue, "by");

                ilaQueue.Enqueue(new WqlLookaheadToken(new WqlTokenCombine(orderToken, byToken), WqlExpressionType.Order)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute]
                });

                attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));

                while (PeekToken(tokenQueue, ","))
                {
                    var separatorToken = ReadToken(tokenQueue, ",");

                    ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Attribute]
                    });

                    attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));
                }

                return new WqlExpressionNodeOrder<TIndexItem> { Attributes = attributes };
            }
            else if (PeekToken(tokenQueue, "orderby"))
            {
                var attributes = new List<WqlExpressionNodeOrderAttribute<TIndexItem>>();
                var i = 0;

                var orderbyToken = ReadToken(tokenQueue, "orderby");

                ilaQueue.Enqueue(new WqlLookaheadToken(orderbyToken, WqlExpressionType.Order)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute]
                });

                attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));

                while (PeekToken(tokenQueue, ","))
                {
                    var separatorToken = ReadToken(tokenQueue, ",");

                    ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Attribute]
                    });

                    attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));
                }

                return new WqlExpressionNodeOrder<TIndexItem> { Attributes = attributes };
            }
            else if (PeekToken(tokenQueue, "order"))
            {
                var attributes = new List<WqlExpressionNodeOrderAttribute<TIndexItem>>();
                var i = 0;

                var orderbyToken = ReadToken(tokenQueue, "order");

                ilaQueue.Enqueue(new WqlLookaheadToken(orderbyToken, WqlExpressionType.Order)
                {
                    ExpectedNextTokens = [WqlExpressionType.Attribute]
                });

                attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));

                while (PeekToken(tokenQueue, ","))
                {
                    var separatorToken = ReadToken(tokenQueue, ",");

                    ilaQueue.Enqueue(new WqlLookaheadToken(separatorToken, WqlExpressionType.Separator)
                    {
                        ExpectedNextTokens = [WqlExpressionType.Attribute]
                    });

                    attributes.Add(ParseOrderAttribute(tokenQueue, i++, ilaQueue));
                }

                return new WqlExpressionNodeOrder<TIndexItem> { Attributes = attributes };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a single order attribute with optional direction.
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <param name="position">The attribute position in the order list.</param>
        /// <param name="ilaQueue">
        /// The incremental lookahead analysis stack to record the attribute token.
        /// </param>
        /// <returns>The order attribute node.</returns>
        private WqlExpressionNodeOrderAttribute<TIndexItem> ParseOrderAttribute(Queue<WqlToken> tokenQueue, int position, Queue<WqlLookaheadToken> ilaQueue)
        {
            var attributeToken = PeekToken(tokenQueue);

            if (attributeToken is null)
            {
                throw new WqlParseException
                (
                    "webexpress.webindex:wql.attribute_unknown",
                    attributeToken
                );
            }

            var path = WqlPropertyPath<TIndexItem>.Parse(attributeToken.Value);
            var property = path.Resolve(typeof(TIndexItem));

            ReadToken(tokenQueue);

            if (property is not null)
            {
                ilaQueue.Enqueue(new WqlLookaheadToken(attributeToken, WqlExpressionType.Attribute)
                {
                    ExpectedNextTokens = [WqlExpressionType.Separator, WqlExpressionType.OrderDirection, WqlExpressionType.PartitioningOperator],
                });

                var descending = ParseDescendingOrder(tokenQueue, ilaQueue);

                return new WqlExpressionNodeOrderAttribute<TIndexItem>
                {
                    Attribute = new WqlExpressionNodeAttribute<TIndexItem>()
                    {
                        Name = path.ToString()
                    },
                    Descending = descending,
                    Position = position
                };
            }

            throw new WqlParseException
            (
                "webexpress.webindex:wql.attribute_unknown",
                attributeToken
            );
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
        private static int ParseNumberValue(Queue<WqlToken> tokenQueue)
        {
            var token = ReadToken(tokenQueue, NumberRegex());
            return int.Parse(token?.Value);
        }

        /// <summary>
        /// Parses a fuzzy similarity token "~<number>".
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The similarity value.</returns>
        private static int ParseFuzzyValue(Queue<WqlToken> tokenQueue)
        {
            var token = ReadToken(tokenQueue, FuzzyRegex());
            return int.Parse(token?.Value[1..]);
        }

        /// <summary>
        /// Parses a distance token ":<number>".
        /// </summary>
        /// <param name="tokenQueue">The token queue.</param>
        /// <returns>The distance value.</returns>
        private static int ParseDistanceValue(Queue<WqlToken> tokenQueue)
        {
            var token = ReadToken(tokenQueue, DistanceRegex());
            return int.Parse(token?.Value[1..]);
        }

        /// <summary>
        /// Tokenizes the input string into a queue of WQL tokens.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The token queue.</returns>
        private static Queue<WqlToken> Tokenize(string input)
        {
            var tokens = new Queue<WqlToken>();
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
                    i++;

                    if (!currentToken.IsEmpty)
                    {
                        tokens.Enqueue(currentToken);
                        currentToken = new WqlToken() { Offset = i + 1 };
                    }

                    // opening quote token
                    currentToken.Append(c);
                    tokens.Enqueue(currentToken);
                    currentToken = new WqlToken() { Offset = i + 1 };

                    // read content until closing quote
                    while (i < input.Length && input[i] != startChar)
                    {
                        currentToken.Append(input[i]);
                        i++;
                    }

                    if (i < input.Length)
                    {
                        tokens.Enqueue(currentToken);
                        currentToken = new WqlToken() { Offset = i + 1 };

                        // closing quote token
                        currentToken.Append(input[i]);
                        tokens.Enqueue(currentToken);
                        currentToken = new WqlToken() { Offset = i + 1 };
                    }
                    else
                    {
                        // ignore
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
            return tokenQueue.Count > 0 && regex.IsMatch(tokenQueue.Peek().Value?.ToLower());
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
        }
    }
}