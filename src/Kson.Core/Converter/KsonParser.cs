using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kson.Core.Node;
using Kson.Core.Util;

namespace Kson.Core.Converter;

public class KsonParser
{
    private readonly List<KsonLexer.Token> _tokens;
    private int _currentTokenIdx;

    public KsonParser(string input)
    {
        var lexer = new KsonLexer(input);
        _tokens = lexer.ScanTokens();
    }

    public static KsNode Parse(string input)
    {
        var parser = new KsonParser(input);
        try
        {
            return parser.Value();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Parse error: {ex.Message}", ex);
        }
    }

    public static List<KsNode> ParseItems(string input)
    {
        var parser = new KsonParser(input);
        try
        {
            var elements = new List<KsNode>();
            parser.SkipWhitespace();

            while (!parser.IsAtEnd())
            {
                elements.Add(parser.Value());
                parser.SkipWhitespace();
                if (parser.Check(KsonLexer.TokenType.Comma))
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

    private KsNode Value()
    {
        SkipWhitespace();
        var prefixes = ParsePrefixes();
        var token = Peek();
        KsNode value;

        switch (token.Type)
        {
            case KsonLexer.TokenType.LeftBrace:
                value = Map();
                break;
            case KsonLexer.TokenType.LeftBracket:
                value = Array();
                break;
            case KsonLexer.TokenType.LeftParen:
                value = ChainNode();
                break;
            case KsonLexer.TokenType.RawString:
                value = RawString();
                break;
            case KsonLexer.TokenType.String:
                value = String();
                break;
            case KsonLexer.TokenType.Number:
                value = Number();
                break;
            case KsonLexer.TokenType.True:
                Advance();
                value = KsBoolean.True;
                break;
            case KsonLexer.TokenType.False:
                Advance();
                value = KsBoolean.False;
                break;
            case KsonLexer.TokenType.Null:
                Advance();
                value = KsNull.Null;
                break;
            case KsonLexer.TokenType.Word:
                value = Word();
                break;
            case KsonLexer.TokenType.BackQuote:
                {
                    var nextToken = Peek(1);
                    Advance();
                    if (nextToken.Type == KsonLexer.TokenType.LeftParen)
                    {
                        var quotedValue = Value();
                        value = new KsQuoteNode(QuoteType.QuasiQuote, quotedValue);
                    }
                    else
                    {
                        var symbolWord = Word();
                        value = new KsSymbol(symbolWord.GetFullNameStr());
                        // value = new KsStackOp(symbolWord.GetFullNameStr());
                    }

                    break;
                }
            case KsonLexer.TokenType.VerticalLine:
                value = FuncInOutData();
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
        var postfixes = new List<KsNode>();
        while (Check(KsonLexer.TokenType.Percent))
        {
            Advance();
            if (Check(KsonLexer.TokenType.LeftParen))
            {
                Advance();
                SkipWhitespace();
                while (!Check(KsonLexer.TokenType.RightParen) && !IsAtEnd())
                {
                    postfixes.Add(Value());
                    SkipWhitespace();
                    if (Check(KsonLexer.TokenType.Comma))
                    {
                        Advance();
                        SkipWhitespace();
                    }
                }

                Consume(KsonLexer.TokenType.RightParen, "Expected ')'");
            }
            else
            {
                RewindOne();
                break;
            }
        }

        if (postfixes.Count > 0)
        {
            var postfixArray = new KsArray(postfixes);
            if (value is KsWord word)
            {
                word.Postfixes = postfixArray;
            }
            else if (value is KsChainNode chainNode)
            {
                chainNode.Postfixes = postfixArray;
            }
        }

        return value;
    }

    private KsWord Word()
    {
        var pathItems = new List<string>();
        var wordToken = Consume(KsonLexer.TokenType.Word, "Expected word");
        pathItems.Add(wordToken.Lexeme);

        while (Check(KsonLexer.TokenType.Dot))
        {
            Advance();
            var nextPart = Consume(KsonLexer.TokenType.Word, "Expected word after dot");
            pathItems.Add(nextPart.Lexeme);
        }

        return new KsWord(pathItems);
    }

    private KsMap Map()
    {
        Consume(KsonLexer.TokenType.LeftBrace, "Expected '{'");
        var map = new KsMap();
        SkipWhitespace();

        while (!Check(KsonLexer.TokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(KsonLexer.TokenType.Comma))
            {
                Advance();
                SkipWhitespace();

                if (Check(KsonLexer.TokenType.Percent))
                {
                    Advance();
                    SkipWhitespace();
                    var spreadValue = Value();
                    var quoteNode = spreadValue as KsQuoteNode ?? new KsQuoteNode(QuoteType.UnquoteMap, spreadValue);
                    map.AddSpread(quoteNode);
                    SkipWhitespace();
                    continue;
                }

                if (Check(KsonLexer.TokenType.RightBrace))
                {
                    break;
                }
            }

            string key;
            if (Check(KsonLexer.TokenType.String))
            {
                var stringKey = Advance();
                key = stringKey.Lexeme[1..^1];
            }
            else if (Check(KsonLexer.TokenType.Word))
            {
                key = Advance().Lexeme;
            }
            else
            {
                throw Error(Peek(), "Expected string or word as map key");
            }

            SkipWhitespace();
            Consume(KsonLexer.TokenType.Colon, "Expected ':' after property name");
            SkipWhitespace();
            var value = Value();
            map[key] = value;
            SkipWhitespace();
        }

        Consume(KsonLexer.TokenType.RightBrace, "Expected '}'");
        return map;
    }

    private KsArray Array() => Array(KsonLexer.TokenType.LeftBracket, KsonLexer.TokenType.RightBracket);

    private KsArray Array(KsonLexer.TokenType startToken, KsonLexer.TokenType endToken)
    {
        Consume(startToken, $"Expected '{TokenLiteral(startToken)}'");
        var elements = new List<KsNode>();
        SkipWhitespace();

        while (!Check(endToken) && !IsAtEnd())
        {
            elements.Add(Value());
            SkipWhitespace();
            if (Check(KsonLexer.TokenType.Comma))
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
        return new KsArray(elements);
    }

    private List<KsNode> ParseCallParams()
    {
        var result = new List<KsNode>();
        SkipWhitespace();
        while (!IsAtEnd()
               && !Check(KsonLexer.TokenType.RightParen)
               && !Check(KsonLexer.TokenType.Tilde)
               && !Check(KsonLexer.TokenType.Colon)
               && !Check(KsonLexer.TokenType.StaticSubscript)
               && !Check(KsonLexer.TokenType.ContainerSubscript)
               && !Check(KsonLexer.TokenType.Semicolon))
        {
            var item = Value();
            result.Add(item);
            SkipWhitespace();
        }
        SkipWhitespace();
        if (Check(KsonLexer.TokenType.Semicolon))
        {
            Advance();
        }
        return result;
    }

    private List<KsNode> ParseValuesUntil(string end)
    {
        var result = new List<KsNode>();
        while (!IsAtEnd()
               && !CheckNextTokenLexme(end))
        {
            var item = Value();
            result.Add(item);
            SkipWhitespace();
        }
        return result;
    }

    private KsFuncInOutData FuncInOutData()
    {
        var args = Array(KsonLexer.TokenType.VerticalLine, KsonLexer.TokenType.VerticalLine);
        return new KsFuncInOutData(args.GetItems());
    }

    private KsNode ChainNode()
    {
        Consume(KsonLexer.TokenType.LeftParen, "Expected '('");
        SkipWhitespace();
        var head = new KsChainNode();
        var currentChainNode = head;

        while (!Check(KsonLexer.TokenType.RightParen) && !IsAtEnd())
        {
            SkipWhitespace();
            if (Peek().Type == KsonLexer.TokenType.Hash)
            {
                Advance();
                if (Check(KsonLexer.TokenType.Word))
                {
                    if (currentChainNode.CallParams != null || currentChainNode.Name != null)
                    {
                        var nextNode = new KsChainNode();
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

            if (Peek().Type == KsonLexer.TokenType.Colon)
            {
                Advance();
                var nextToken = PeekNonWhitespace();
                var callType = ParseColonCallType(nextToken);

                if (callType == KsCallType.PrefixCall)
                {
                    if (currentChainNode.CallType != null)
                    {
                        var nextNode = new KsChainNode();
                        currentChainNode.Next = nextNode;
                        currentChainNode = nextNode;
                    }
                    currentChainNode.CallType = callType;
                    currentChainNode.CallParams = Array(KsonLexer.TokenType.VerticalLine, KsonLexer.TokenType.VerticalLine);
                    // parse return types if exists
                    SkipWhitespace();
                    var nextTokenAfterArgs = Peek();
                    if (nextTokenAfterArgs.Type == KsonLexer.TokenType.Word && nextTokenAfterArgs.Lexeme == "<")
                    {
                        Advance(); // skip <
                        var resultTypeValues = ParseValuesUntil(">");
                        currentChainNode.CallResults = new KsFuncInOutData(resultTypeValues);
                        Advance(); // skip >
                    }
                }
                else if (callType == KsCallType.PostfixCall)
                {
                    if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                    {
                        var nextNode = new KsChainNode();
                        currentChainNode.Next = nextNode;
                        currentChainNode = nextNode;
                    }

                    currentChainNode.CallType = callType;
                    var func = Value();
                    currentChainNode.Core = func;

                    var items = ParseCallParams();
                    currentChainNode.CallParams = items.Count > 0 ? new KsArray(items) : null;
                }
                else
                {
                    throw Error(nextToken, "illegal Colon");
                }
                continue;
            }

            if (Peek().Type == KsonLexer.TokenType.Tilde)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KsChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KsCallType.InstanceCall;
                var method = Word();
                currentChainNode.Core = method;
                var items = ParseCallParams();
                currentChainNode.CallParams = items.Count > 0 ? new KsArray(items) : null;
                continue;
            }
            if (Peek().Type == KsonLexer.TokenType.StaticSubscript)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KsChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KsCallType.StaticSubscript;
                var core = Value();
                currentChainNode.Core = core;
                continue;
            }
            if (Peek().Type == KsonLexer.TokenType.ContainerSubscript)
            {
                Advance();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KsChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.CallType = KsCallType.ContainerSubscript;
                var core = Value();
                currentChainNode.Core = core;
                continue;
            }

            if (Peek().Type == KsonLexer.TokenType.Comma)
            {
                Advance();
                var quoteKind = QuoteType.Unquote;
                if (Check(KsonLexer.TokenType.At))
                {
                    Advance();
                    quoteKind = QuoteType.UnquoteSplice;
                }
                else if (Check(KsonLexer.TokenType.Percent))
                {
                    Advance();
                    quoteKind = QuoteType.UnquoteMap;
                }

                SkipWhitespace();
                var value = Value();
                if (!currentChainNode.AcceptCore())
                {
                    var nextNode = new KsChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.Core = new KsQuoteNode(quoteKind, value);
                continue;
            }

            if (Peek().Type == KsonLexer.TokenType.At)
            {
                Advance();
                if (Check(KsonLexer.TokenType.Word))
                {
                    var attrName = Consume(KsonLexer.TokenType.Word, "Expected annotation name");
                    SkipWhitespace();
                    currentChainNode.Attr ??= new Dictionary<string, KsNode>();
                    if (Check(KsonLexer.TokenType.Colon))
                    {
                        Advance();
                        var attrValue = Value();
                        currentChainNode.Attr[attrName.Lexeme] = attrValue;
                    }
                    else
                    {
                        currentChainNode.Attr[attrName.Lexeme] = KsBoolean.True;
                    }

                    continue;
                }

                RewindOne();
            }

            if (Peek().Type == KsonLexer.TokenType.Percent)
            {
                Advance();
                if (Check(KsonLexer.TokenType.LeftBrace))
                {
                    currentChainNode.Conf = Map();
                    continue;
                }

                if (Check(KsonLexer.TokenType.LeftBracket))
                {
                    currentChainNode.Body = Array();
                    continue;
                }

                if (Check(KsonLexer.TokenType.Word))
                {
                    var blockName = Consume(KsonLexer.TokenType.Word, "Expected name");
                    SkipWhitespace();
                    Consume(KsonLexer.TokenType.Colon, "Expected separator");
                    SkipWhitespace();
                    if (Check(KsonLexer.TokenType.LeftBrace))
                    {
                        currentChainNode.NamedConf ??= new Dictionary<string, KsMap>();
                        currentChainNode.NamedConf[blockName.Lexeme] = Map();
                    }
                    else if (Check(KsonLexer.TokenType.LeftBracket))
                    {
                        currentChainNode.Sections ??= new Dictionary<string, KsArray>();
                        currentChainNode.Sections[blockName.Lexeme] = Array();
                    }
                    else if (Check(KsonLexer.TokenType.LeftParen))
                    {
                        currentChainNode.Slots ??= new Dictionary<string, KsChainNode>();
                        currentChainNode.Slots[blockName.Lexeme] = (KsChainNode)ChainNode();
                    }

                    continue;
                }

                RewindOne();
            }

            if (Peek().Type == KsonLexer.TokenType.Semicolon)
            {
                Advance();
                continue;
            }

            SkipWhitespace();
            if (!Check(KsonLexer.TokenType.RightParen)
                && !Check(KsonLexer.TokenType.Percent)
                && !IsAtEnd())
            {
                var core = Value();
                if (currentChainNode.CallType != null || !currentChainNode.AcceptCore())
                {
                    var nextNode = new KsChainNode();
                    currentChainNode.Next = nextNode;
                    currentChainNode = nextNode;
                }

                currentChainNode.Core = core;
                SkipWhitespace();
            }
        }

        Consume(KsonLexer.TokenType.RightParen, "Expected ')'");
        return head;
    }

    private PrefixContext ParsePrefixes()
    {
        var result = new PrefixContext();
        var prefixes = new List<KsNode>();
        while (Check(KsonLexer.TokenType.At))
        {
            Advance();
            if (Check(KsonLexer.TokenType.LeftParen))
            {
                Advance();
                SkipWhitespace();
                while (!Check(KsonLexer.TokenType.RightParen) && !IsAtEnd())
                {
                    prefixes.Add(Value());
                    SkipWhitespace();
                    if (Check(KsonLexer.TokenType.Comma))
                    {
                        Advance();
                        SkipWhitespace();
                    }
                }

                Consume(KsonLexer.TokenType.RightParen, "Expected ')'");
                result.AnnotationPrefix = prefixes.Count == 0 ? null : new KsArray(prefixes);
                SkipWhitespace();
            }
            else if (Check(KsonLexer.TokenType.LeftBrace))
            {
                var withEffect = Map();
                result.WithEffectPrefix = withEffect;
                SkipWhitespace();
            }
            else if (Check(KsonLexer.TokenType.LeftBracket))
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

    private KsArray? ParseTypePrefixes()
    {
        var prefixes = new List<KsNode>();
        while (Check(KsonLexer.TokenType.Exclamation))
        {
            Advance();
            SkipWhitespace();
            prefixes.Add(Value());
            SkipWhitespace();
        }

        return prefixes.Count == 0 ? null : new KsArray(prefixes);
    }

    private KsNode RawString()
    {
        var stringToken = Consume(KsonLexer.TokenType.RawString, "Expected raw string");
        var rawString = stringToken.Lexeme;
        var inner = rawString.Substring(1, rawString.Length - 2);
        return new KsString(inner);
    }

    private KsString String()
    {
        var stringToken = Consume(KsonLexer.TokenType.String, "Expected string");
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

        return new KsString(processed.ToString());
    }

    private char ParseUnicodeEscape(string content, ref int index, KsonLexer.Token token)
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

    private KsNode Number()
    {
        var token = Consume(KsonLexer.TokenType.Number, "Expected number");
        var value = token.Lexeme;
        if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
        {
            return new KsDouble(double.Parse(value, CultureInfo.InvariantCulture));
        }

        return new KsInt64(long.Parse(value, CultureInfo.InvariantCulture));
    }

    private static bool IsWhitespaceToken(KsonLexer.TokenType type) =>
        type is KsonLexer.TokenType.Whitespace or KsonLexer.TokenType.Newline or KsonLexer.TokenType.Comment;

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

    private KsonLexer.Token Consume(KsonLexer.TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private bool Check(KsonLexer.TokenType type)
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

    private KsonLexer.Token Advance()
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

    private bool IsAtEnd() => Peek().Type == KsonLexer.TokenType.Eof;

    private KsonLexer.Token Peek() => _tokens[_currentTokenIdx];

    private KsonLexer.Token Peek(int offset)
    {
        var index = _currentTokenIdx + offset;
        if (index >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    private KsonLexer.Token PeekNonWhitespace()
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

    private KsonLexer.Token Previous() => _tokens[_currentTokenIdx - 1];

    private InvalidOperationException Error(KsonLexer.Token token, string message) =>
        new($"Error at line {token.Line}, column {token.Column}: {message}");

    private static KsCallType ParseColonCallType(KsonLexer.Token nextToken)
    {
        // if (nextToken.Type == KsonLexer.TokenType.Word && nextToken.Lexeme == "<")
        // {
        //     return KsCallType.PrefixCall;
        // }
        if (nextToken.Type == KsonLexer.TokenType.VerticalLine)
        {
            return KsCallType.PrefixCall;
        }

        return KsCallType.PostfixCall;
    }

    private static string TokenLiteral(KsonLexer.TokenType tokenType) => tokenType switch
    {
        KsonLexer.TokenType.LeftBrace => "{",
        KsonLexer.TokenType.RightBrace => "}",
        KsonLexer.TokenType.LeftBracket => "[",
        KsonLexer.TokenType.RightBracket => "]",
        KsonLexer.TokenType.LeftParen => "(",
        KsonLexer.TokenType.RightParen => ")",
        KsonLexer.TokenType.Colon => ":",
        KsonLexer.TokenType.Semicolon => ";",
        KsonLexer.TokenType.Comma => ",",
        _ => tokenType.ToString()
    };

    private class PrefixContext
    {
        public KsArray? AnnotationPrefix { get; set; }
        public KsMap? WithEffectPrefix { get; set; }
        public KsArray? UnboundTypes { get; set; }
        public KsArray? TypePrefix { get; set; }
    }
}
