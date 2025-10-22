using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kon.Core.Node;
using Kon.Core.Node.Inner;
using Kon.Core.Util;

namespace Kon.Core.Converter;

public class KonParser
{
    private readonly List<KonLexer.Token> _tokens;
    private int _currentTokenIdx;

    public KonParser(string input)
    {
        var lexer = new KonLexer(input);
        _tokens = lexer.ScanTokens();
    }

    public static KnNode Parse(string input)
    {
        var parser = new KonParser(input);
        try
        {
            return parser.Value();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Parse error: {ex.Message}", ex);
        }
    }

    public static List<KnNode> ParseItems(string input)
    {
        var parser = new KonParser(input);
        try
        {
            var elements = new List<KnNode>();
            parser.SkipWhitespace();

            while (!parser.IsAtEnd())
            {
                elements.Add(parser.Value());
                parser.SkipWhitespace();
                if (parser.Check(KonLexer.TokenType.Comma))
                {
                    parser.Advance();
                    parser.SkipWhitespace();
                }
            }
            return elements;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Parse error: {ex.Message}", ex);
        }
    }

    private KnNode Value()
    {
        SkipWhitespace();
        var prefixes = ParsePrefixes();
        var token = Peek();
        KnNode value;

        switch (token.Type)
        {
            case KonLexer.TokenType.LeftBrace:
                value = Map();
                break;
            case KonLexer.TokenType.LeftBracket:
                value = Array();
                break;
            case KonLexer.TokenType.LeftParen:
                value = ChainNode();
                break;
            case KonLexer.TokenType.RawString:
                value = RawString();
                break;
            case KonLexer.TokenType.String:
                value = String();
                break;
            case KonLexer.TokenType.Number:
                value = Number();
                break;
            case KonLexer.TokenType.True:
                Advance();
                value = KnBoolean.True;
                break;
            case KonLexer.TokenType.False:
                Advance();
                value = KnBoolean.False;
                break;
            case KonLexer.TokenType.Null:
                Advance();
                value = KnNull.Null;
                break;
            case KonLexer.TokenType.Word:
                value = Word();
                break;
            case KonLexer.TokenType.BackQuote:
                {
                    var nextToken = Peek(1);
                    Advance();
                    if (nextToken.Type == KonLexer.TokenType.LeftParen)
                    {
                        var quotedValue = Value();
                        value = new KnQuoteNode(QuoteType.QuasiQuote, quotedValue);
                    }
                    else
                    {
                        var symbolWord = Word();
                        value = new KnSymbol(symbolWord.GetFullNameStr());
                        // value = new KnStackOp(symbolWord.GetFullNameStr());
                    }

                    break;
                }
            case KonLexer.TokenType.VerticalLine:
                value = InOutTable();
                break;
            default:
                throw Error(token, "Expected value");
        }

        if (value is SupportPrefixes supportPrefixes)
        {
            if (prefixes.AnnotationPrefix is not null)
            {
                supportPrefixes.AnnotationPrefixes = prefixes.AnnotationPrefix;
            }

            if (prefixes.WithEffectPrefix is not null)
            {
                supportPrefixes.WithEffectPrefix = prefixes.WithEffectPrefix;
            }

            if (prefixes.TypePrefix is not null)
            {
                supportPrefixes.TypePrefixes = prefixes.TypePrefix;
            }

            if (prefixes.UnboundTypes is not null)
            {
                supportPrefixes.UnboundTypes = prefixes.UnboundTypes;
            }
        }

        SkipWhitespace();
        var postfixes = new List<KnNode>();
        while (Check(KonLexer.TokenType.Percent))
        {
            Advance();
            if (Check(KonLexer.TokenType.LeftParen))
            {
                Advance();
                SkipWhitespace();
                while (!Check(KonLexer.TokenType.RightParen) && !IsAtEnd())
                {
                    postfixes.Add(Value());
                    SkipWhitespace();
                    if (Check(KonLexer.TokenType.Comma))
                    {
                        Advance();
                        SkipWhitespace();
                    }
                }

                Consume(KonLexer.TokenType.RightParen, "Expected ')'");
            }
            else
            {
                RewindOne();
                break;
            }
        }

        if (postfixes.Count > 0)
        {
            var postfixArray = new KnArray(postfixes);
            if (value is KnWord word)
            {
                word.Postfixes = postfixArray;
            }
            else if (value is KnChainNode chainNode)
            {
                chainNode.Postfixes = postfixArray;
            }
        }

        return value;
    }

    private KnWord Word()
    {
        var pathItems = new List<string>();
        var wordToken = Consume(KonLexer.TokenType.Word, "Expected word");
        pathItems.Add(wordToken.Lexeme);

        while (Check(KonLexer.TokenType.Dot))
        {
            Advance();
            var nextPart = Consume(KonLexer.TokenType.Word, "Expected word after dot");
            pathItems.Add(nextPart.Lexeme);
        }

        return new KnWord(pathItems);
    }

    private KnMap Map()
    {
        Consume(KonLexer.TokenType.LeftBrace, "Expected '{'");
        var map = new KnMap();
        SkipWhitespace();

        while (!Check(KonLexer.TokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(KonLexer.TokenType.Comma))
            {
                Advance();
                SkipWhitespace();

                if (Check(KonLexer.TokenType.Percent))
                {
                    Advance();
                    SkipWhitespace();
                    var spreadValue = Value();
                    var quoteNode = spreadValue as KnQuoteNode ?? new KnQuoteNode(QuoteType.UnquoteMap, spreadValue);
                    map.AddSpread(quoteNode);
                    SkipWhitespace();
                    continue;
                }

                if (Check(KonLexer.TokenType.RightBrace))
                {
                    break;
                }
            }

            string key;
            if (Check(KonLexer.TokenType.String))
            {
                var stringKey = Advance();
                key = stringKey.Lexeme[1..^1];
            }
            else if (Check(KonLexer.TokenType.Word))
            {
                key = Advance().Lexeme;
            }
            else
            {
                throw Error(Peek(), "Expected string or word as map key");
            }

            SkipWhitespace();
            Consume(KonLexer.TokenType.Colon, "Expected ':' after property name");
            SkipWhitespace();
            var value = Value();
            map[key] = value;
            SkipWhitespace();
        }

        Consume(KonLexer.TokenType.RightBrace, "Expected '}'");
        return map;
    }

    private KnArray Array() => Array(KonLexer.TokenType.LeftBracket, KonLexer.TokenType.RightBracket);

    private KnArray Array(KonLexer.TokenType startToken, KonLexer.TokenType endToken)
    {
        Consume(startToken, $"Expected '{TokenLiteral(startToken)}'");
        var elements = new List<KnNode>();
        SkipWhitespace();

        while (!Check(endToken) && !IsAtEnd())
        {
            elements.Add(Value());
            SkipWhitespace();
            if (Check(KonLexer.TokenType.Comma))
            {
                Advance();
                SkipWhitespace();
            }

            if (Check(endToken))
            {
                break;
            }
        }

        Consume(endToken, $"Expected '{TokenLiteral(endToken)}'");
        return new KnArray(elements);
    }

    private List<KnNode> ParseCallParams()
    {
        var result = new List<KnNode>();
        SkipWhitespace();
        while (!IsAtEnd()
               && !Check(KonLexer.TokenType.RightParen)
               && !Check(KonLexer.TokenType.Tilde)
               && !Check(KonLexer.TokenType.Colon)
               && !Check(KonLexer.TokenType.StaticSubscript)
               && !Check(KonLexer.TokenType.ContainerSubscript)
               && !Check(KonLexer.TokenType.Semicolon))
        {
            var item = Value();
            result.Add(item);
            SkipWhitespace();
        }
        SkipWhitespace();
        if (Check(KonLexer.TokenType.Semicolon))
        {
            Advance();
        }
        return result;
    }

    private List<KnNode> ParseValuesUntil(string end)
    {
        var result = new List<KnNode>();
        while (!IsAtEnd()
               && !CheckNextTokenLexme(end))
        {
            var item = Value();
            result.Add(item);
            SkipWhitespace();
        }
        return result;
    }

    // TODO 支持参数表中的默认值
    private KnInOutTable InOutTable()
    {
        KnInOutTableType type = KnInOutTableType.NoOutput;
        List<KnInOutTableItem> inputs = new();
        List<KnInOutTableItem> outputs = new();
        bool parseOutput = false;

        Consume(KonLexer.TokenType.VerticalLine, "Expected '|'");
        SkipWhitespace();

        while (!IsAtEnd()
            && !Check(KonLexer.TokenType.VerticalLine))
        {
            KnNode node = Value();
            if (node is KnWord knWord)
            {
                string wordInner = knWord.Value;
                if ("--".Equals(wordInner))
                {
                    parseOutput = true;
                    type = KnInOutTableType.NameAndTypeOutput;
                    SkipWhitespace();
                    continue;
                }
                else if ("->".Equals(wordInner))
                {
                    parseOutput = true;
                    type = KnInOutTableType.OnlyTypeOutput;
                    SkipWhitespace();
                    continue;
                }
            }
            if (parseOutput)
            {
                outputs.Add(new KnInOutTableItem
                {
                    Value = node
                });
            }
            else
            {
                inputs.Add(new KnInOutTableItem
                {
                    Value = node
                });
            }
            SkipWhitespace();
        }

        if (Check(KonLexer.TokenType.VerticalLine))
        {
            Advance();
            SkipWhitespace();
        }


        return new KnInOutTable
        {
            Type = type,
            Inputs = inputs,
            Outputs = outputs
        };
    }

    private KnNode ChainNode()
    {
        Consume(KonLexer.TokenType.LeftParen, "Expected '('");
        SkipWhitespace();
        var head = new KnChainNode();
        var currentChainNode = head;

        while (!Check(KonLexer.TokenType.RightParen) && !IsAtEnd())
        {
            SkipWhitespace();
            if (Peek().Type == KonLexer.TokenType.Hash)
            {
                Advance();
                if (Check(KonLexer.TokenType.Word))
                {
                    if (currentChainNode.InOutTable != null || currentChainNode.Name != null)
                    {
                        var nextNode = new KnChainNode();
                        currentChainNode.Next = nextNode;
                        currentChainNode = nextNode;
                    }

                    currentChainNode.Name = Word();
                }
                else
                {
                    throw new NotImplementedException();
                }

                continue;
            }

            if (Peek().Type == KonLexer.TokenType.Colon)
            {
                Advance();
                var nextToken = PeekNonWhitespace();
                var callType = ParseColonCallType(nextToken);

                if (callType == KnCallType.PrefixCall)
                {
                    if (currentChainNode.CallType != null)
                    {
                        var nextNode = new KnChainNode();
                        currentChainNode.Next = nextNode;
                        currentChainNode = nextNode;
                    }
                    currentChainNode.CallType = callType;
                    currentChainNode.InOutTable = InOutTable();
                    // parse return types if exists
                    SkipWhitespace();
                    var nextTokenAfterArgs = Peek();
                    if (nextTokenAfterArgs.Type == KonLexer.TokenType.Word && nextTokenAfterArgs.Lexeme == "<")
                    {
                        Advance(); // skip <
                        var resultTypeValues = ParseValuesUntil(">");
                        currentChainNode.GenericParams = new KnArray(resultTypeValues);
                        Advance(); // skip >
                    }
                }
                else if (callType == KnCallType.PostfixCall)
                {
                    if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                    {
                        var nextNode = new KnChainNode();
                        currentChainNode.Next = nextNode;
                        currentChainNode = nextNode;
                    }

                    currentChainNode.CallType = callType;
                    var func = Value();
                    currentChainNode.Core = func;

                    var items = ParseCallParams();
                    if (items.Count > 0)
                    {
                        currentChainNode.InOutTable = KnInOutTable.MakeByInputNodes(items);
                    }
                }
                else
                {
                    throw Error(nextToken, "illegal Colon");
                }
                continue;
            }

            if (Peek().Type == KonLexer.TokenType.Tilde)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KnChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KnCallType.InstanceCall;
                var method = Word();
                currentChainNode.Core = method;
                var items = ParseCallParams();
                if (items.Count > 0)
                {
                    currentChainNode.InOutTable = KnInOutTable.MakeByInputNodes(items);
                }
                continue;
            }
            if (Peek().Type == KonLexer.TokenType.StaticSubscript)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KnChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KnCallType.StaticSubscript;
                var core = Value();
                currentChainNode.Core = core;
                continue;
            }
            if (Peek().Type == KonLexer.TokenType.ContainerSubscript)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KnChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KnCallType.ContainerSubscript;
                var core = Value();
                currentChainNode.Core = core;
                continue;
            }

            if (Peek().Type == KonLexer.TokenType.Comma)
            {
                Advance();
                var quoteKind = QuoteType.Unquote;
                if (Check(KonLexer.TokenType.At))
                {
                    Advance();
                    quoteKind = QuoteType.UnquoteSplice;
                }
                else if (Check(KonLexer.TokenType.Percent))
                {
                    Advance();
                    quoteKind = QuoteType.UnquoteMap;
                }

                SkipWhitespace();
                var value = Value();
                if (!currentChainNode.AcceptCore())
                {
                    var nextNode = new KnChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.Core = new KnQuoteNode(quoteKind, value);
                continue;
            }

            if (Peek().Type == KonLexer.TokenType.At)
            {
                Advance();
                if (Check(KonLexer.TokenType.Word))
                {
                    var attrName = Consume(KonLexer.TokenType.Word, "Expected annotation name");
                    SkipWhitespace();
                    currentChainNode.Attr ??= new Dictionary<string, KnNode>();
                    if (Check(KonLexer.TokenType.Colon))
                    {
                        Advance();
                        var attrValue = Value();
                        currentChainNode.Attr[attrName.Lexeme] = attrValue;
                    }
                    else
                    {
                        currentChainNode.Attr[attrName.Lexeme] = KnBoolean.True;
                    }

                    continue;
                }

                RewindOne();
            }

            if (Peek().Type == KonLexer.TokenType.Percent)
            {
                Advance();
                if (Check(KonLexer.TokenType.LeftBrace))
                {
                    currentChainNode.Conf = Map();
                    continue;
                }

                if (Check(KonLexer.TokenType.LeftBracket))
                {
                    currentChainNode.Body = Array();
                    continue;
                }

                if (Check(KonLexer.TokenType.Word))
                {
                    var blockName = Consume(KonLexer.TokenType.Word, "Expected name");
                    SkipWhitespace();
                    Consume(KonLexer.TokenType.Colon, "Expected separator");
                    SkipWhitespace();
                    if (Check(KonLexer.TokenType.LeftBrace))
                    {
                        currentChainNode.NamedConf ??= new Dictionary<string, KnMap>();
                        currentChainNode.NamedConf[blockName.Lexeme] = Map();
                    }
                    else if (Check(KonLexer.TokenType.LeftBracket))
                    {
                        currentChainNode.Sections ??= new Dictionary<string, KnArray>();
                        currentChainNode.Sections[blockName.Lexeme] = Array();
                    }
                    else if (Check(KonLexer.TokenType.LeftParen))
                    {
                        currentChainNode.Slots ??= new Dictionary<string, KnChainNode>();
                        currentChainNode.Slots[blockName.Lexeme] = (KnChainNode)ChainNode();
                    }

                    continue;
                }

                RewindOne();
            }

            if (Peek().Type == KonLexer.TokenType.Semicolon)
            {
                Advance();
                continue;
            }

            SkipWhitespace();
            if (!Check(KonLexer.TokenType.RightParen)
                && !Check(KonLexer.TokenType.Percent)
                && !IsAtEnd())
            {
                var core = Value();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KnChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.Core = core;
                SkipWhitespace();
            }
        }

        Consume(KonLexer.TokenType.RightParen, "Expected ')'");
        return head;
    }

    private PrefixContext ParsePrefixes()
    {
        var result = new PrefixContext();
        var prefixes = new List<KnNode>();
        while (Check(KonLexer.TokenType.At))
        {
            Advance();
            if (Check(KonLexer.TokenType.LeftParen))
            {
                Advance();
                SkipWhitespace();
                while (!Check(KonLexer.TokenType.RightParen) && !IsAtEnd())
                {
                    prefixes.Add(Value());
                    SkipWhitespace();
                    if (Check(KonLexer.TokenType.Comma))
                    {
                        Advance();
                        SkipWhitespace();
                    }
                }

                Consume(KonLexer.TokenType.RightParen, "Expected ')'");
                result.AnnotationPrefix = prefixes.Count == 0 ? null : new KnArray(prefixes);
                SkipWhitespace();
            }
            else if (Check(KonLexer.TokenType.LeftBrace))
            {
                var withEffect = Map();
                result.WithEffectPrefix = withEffect;
                SkipWhitespace();
            }
            else if (Check(KonLexer.TokenType.LeftBracket))
            {
                var unboundTypes = Array();
                result.UnboundTypes = unboundTypes;
                SkipWhitespace();
            }
            else
            {
                RewindOne();
                break;
            }
        }

        SkipWhitespace();
        var typePrefixes = ParseTypePrefixes();
        result.TypePrefix = typePrefixes;
        return result;
    }

    private KnArray? ParseTypePrefixes()
    {
        var prefixes = new List<KnNode>();
        while (Check(KonLexer.TokenType.Exclamation))
        {
            Advance();
            SkipWhitespace();
            prefixes.Add(Value());
            SkipWhitespace();
        }

        return prefixes.Count == 0 ? null : new KnArray(prefixes);
    }

    private KnNode RawString()
    {
        var stringToken = Consume(KonLexer.TokenType.RawString, "Expected raw string");
        var rawString = stringToken.Lexeme;
        var inner = rawString.Substring(1, rawString.Length - 2);
        return new KnString(inner);
    }

    private KnString String()
    {
        var stringToken = Consume(KonLexer.TokenType.String, "Expected string");
        var rawString = stringToken.Lexeme;
        var content = rawString.Substring(1, rawString.Length - 2);
        var processed = new StringBuilder();
        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (c == '\\')
            {
                if (i + 1 >= content.Length)
                {
                    throw Error(stringToken, "Incomplete escape sequence");
                }

                var next = content[++i];
                processed.Append(next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'b' => '\b',
                    'f' => '\f',
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'u' => ParseUnicodeEscape(content, ref i, stringToken),
                    _ => throw Error(stringToken, $"Invalid escape sequence: \\{next}")
                });
            }
            else
            {
                processed.Append(c);
            }
        }

        return new KnString(processed.ToString());
    }

    private char ParseUnicodeEscape(string content, ref int index, KonLexer.Token token)
    {
        if (index + 4 >= content.Length)
        {
            throw Error(token, "Incomplete Unicode escape sequence");
        }

        var hex = content.Substring(index + 1, 4);
        if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unicode))
        {
            throw Error(token, $"Invalid Unicode escape sequence: \\u{hex}");
        }

        index += 4;
        return (char)unicode;
    }

    private KnNode Number()
    {
        var token = Consume(KonLexer.TokenType.Number, "Expected number");
        var value = token.Lexeme;
        if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
        {
            return new KnDouble(double.Parse(value, CultureInfo.InvariantCulture));
        }

        return new KnInt64(long.Parse(value, CultureInfo.InvariantCulture));
    }

    private static bool IsWhitespaceToken(KonLexer.TokenType type) =>
        type is KonLexer.TokenType.Whitespace or KonLexer.TokenType.Newline or KonLexer.TokenType.Comment;

    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            var type = Peek().Type;
            if (IsWhitespaceToken(type))
            {
                Advance();
            }
            else
            {
                break;
            }
        }
    }

    private KonLexer.Token Consume(KonLexer.TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private bool Check(KonLexer.TokenType type)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return Peek().Type == type;
    }

    private bool CheckNextTokenLexme(string expect)
    {
        if (IsAtEnd())
        {
            return false;
        }
        return Peek().Lexeme.Equals(expect);
    }

    private KonLexer.Token Advance()
    {
        if (!IsAtEnd())
        {
            _currentTokenIdx++;
        }

        return Previous();
    }

    private void RewindOne()
    {
        if (_currentTokenIdx > 0)
        {
            _currentTokenIdx--;
        }
    }

    private bool IsAtEnd() => Peek().Type == KonLexer.TokenType.Eof;

    private KonLexer.Token Peek() => _tokens[_currentTokenIdx];

    private KonLexer.Token Peek(int offset)
    {
        var index = _currentTokenIdx + offset;
        if (index >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    private KonLexer.Token PeekNonWhitespace()
    {
        var index = _currentTokenIdx;
        while (index < _tokens.Count)
        {
            var token = _tokens[index];
            if (!IsWhitespaceToken(token.Type))
            {
                return token;
            }

            index++;
        }

        return _tokens[^1];
    }

    private KonLexer.Token Previous() => _tokens[_currentTokenIdx - 1];

    private InvalidOperationException Error(KonLexer.Token token, string message) =>
        new($"Error at line {token.Line}, column {token.Column}: {message}");

    private static KnCallType ParseColonCallType(KonLexer.Token nextToken)
    {
        // if (nextToken.Type == KonLexer.TokenType.Word && nextToken.Lexeme == "<")
        // {
        //     return KnChainNode.PrefixCall;
        // }
        if (nextToken.Type == KonLexer.TokenType.VerticalLine)
        {
            return KnCallType.PrefixCall;
        }

        return KnCallType.PostfixCall;
    }

    private static string TokenLiteral(KonLexer.TokenType tokenType) => tokenType switch
    {
        KonLexer.TokenType.LeftBrace => "{",
        KonLexer.TokenType.RightBrace => "}",
        KonLexer.TokenType.LeftBracket => "[",
        KonLexer.TokenType.RightBracket => "]",
        KonLexer.TokenType.LeftParen => "(",
        KonLexer.TokenType.RightParen => ")",
        KonLexer.TokenType.Colon => ":",
        KonLexer.TokenType.Semicolon => ";",
        KonLexer.TokenType.Comma => ",",
        _ => tokenType.ToString()
    };

    private class PrefixContext
    {
        public KnArray? AnnotationPrefix { get; set; }
        public KnMap? WithEffectPrefix { get; set; }
        public KnArray? UnboundTypes { get; set; }
        public KnArray? TypePrefix { get; set; }
    }
}
