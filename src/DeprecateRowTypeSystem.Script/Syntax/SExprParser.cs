using System;
using System.Collections.Immutable;

namespace RowTypeSystem.Core.Syntax;

public sealed class SExprParser
{
    private readonly string _text;
    private int _position;
    private Token _current;

    public SExprParser(string text)
    {
        _text = text ?? string.Empty;
        _current = default;
        _position = 0;
    }

    public ImmutableArray<SExprNode> Parse()
    {
        var builder = ImmutableArray.CreateBuilder<SExprNode>();
        NextToken();

        while (_current.Kind != TokenKind.EndOfFile)
        {
            builder.Add(ParseAnnotatedNode());
        }

        return builder.ToImmutable();
    }

    private SExprNode ParseAnnotatedNode(bool allowTrailingAnnotations = true)
    {
        var prefixes = ImmutableArray.CreateBuilder<SExprNode>();
        while (_current.Kind == TokenKind.Bang)
        {
            NextToken();
            prefixes.Add(ParseAnnotatedNode());
        }

        var core = ParseCoreNode();

        var postfixes = ImmutableArray.CreateBuilder<SExprNode>();
        if (allowTrailingAnnotations)
        {
            while (_current.Kind == TokenKind.Caret)
            {
                NextToken();
                postfixes.Add(ParseAnnotatedNode(allowTrailingAnnotations: false));
            }
        }

        if (prefixes.Count == 0 && postfixes.Count == 0)
        {
            return core;
        }

        return core with
        {
            PrefixAnnotations = prefixes.Count == 0 ? core.PrefixAnnotations : prefixes.ToImmutable(),
            PostfixAnnotations = postfixes.Count == 0 ? core.PostfixAnnotations : postfixes.ToImmutable(),
        };
    }

    private SExprNode ParseCoreNode()
    {
        return _current.Kind switch
        {
            TokenKind.LParen => ParseList(),
            TokenKind.LBracket => ParseArray(),
            TokenKind.LBrace => ParseObject(),
            TokenKind.String => ParseString(),
            TokenKind.Identifier => ParseIdentifier(),
            _ => throw new FormatException($"Unexpected token '{_current.Text}' at position {_current.Position}."),
        };
    }

    private SExprNode ParseIdentifier()
    {
        var parts = _current.Text.Split("::", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new FormatException($"Identifier at position {_current.Position} is empty.");
        }

        var immutable = parts.ToImmutableArray();
        var node = new SExprIdentifier(immutable);
        NextToken();

        if (_current.Kind == TokenKind.Tilde)
        {
            NextToken();
            var annotation = ParseAnnotatedNode(allowTrailingAnnotations: false);
            node = node with { TypeAnnotation = annotation };
        }

        return node;
    }

    private SExprNode ParseString()
    {
        var node = new SExprString(_current.Text);
        NextToken();
        return node;
    }

    private SExprNode ParseList()
    {
        Expect(TokenKind.LParen);
        NextToken();

        var elements = ImmutableArray.CreateBuilder<SExprNode>();
        while (_current.Kind != TokenKind.RParen)
        {
            if (_current.Kind == TokenKind.EndOfFile)
            {
                throw new FormatException("Unterminated list: expected ')'.");
            }

            elements.Add(ParseAnnotatedNode());
        }

        Expect(TokenKind.RParen);
        NextToken();
        return new SExprList(elements.ToImmutable());
    }

    private SExprNode ParseArray()
    {
        Expect(TokenKind.LBracket);
        NextToken();

        var elements = ImmutableArray.CreateBuilder<SExprNode>();
        while (_current.Kind != TokenKind.RBracket)
        {
            if (_current.Kind == TokenKind.EndOfFile)
            {
                throw new FormatException("Unterminated array: expected ']'.");
            }

            elements.Add(ParseAnnotatedNode());
        }

        Expect(TokenKind.RBracket);
        NextToken();
        return new SExprArray(elements.ToImmutable());
    }

    private SExprNode ParseObject()
    {
        Expect(TokenKind.LBrace);
        NextToken();

        var properties = ImmutableArray.CreateBuilder<SExprObjectProperty>();
        while (_current.Kind != TokenKind.RBrace)
        {
            if (_current.Kind == TokenKind.EndOfFile)
            {
                throw new FormatException("Unterminated object: expected '}'.");
            }

            var key = ParseAnnotatedNode();
            Expect(TokenKind.Equals);
            NextToken();
            var value = ParseAnnotatedNode();
            properties.Add(new SExprObjectProperty(key, value));
        }

        Expect(TokenKind.RBrace);
        NextToken();
        return new SExprObject(properties.ToImmutable());
    }

    private void NextToken()
    {
        SkipWhitespace();
        if (_position >= _text.Length)
        {
            _current = new Token(TokenKind.EndOfFile, string.Empty, _position);
            return;
        }

        var ch = _text[_position];
        switch (ch)
        {
            case '(':
                _current = new Token(TokenKind.LParen, "(", _position++);
                return;
            case ')':
                _current = new Token(TokenKind.RParen, ")", _position++);
                return;
            case '[':
                _current = new Token(TokenKind.LBracket, "[", _position++);
                return;
            case ']':
                _current = new Token(TokenKind.RBracket, "]", _position++);
                return;
            case '{':
                _current = new Token(TokenKind.LBrace, "{", _position++);
                return;
            case '}':
                _current = new Token(TokenKind.RBrace, "}", _position++);
                return;
            case ':':
                _current = new Token(TokenKind.Colon, ":", _position++);
                return;
            case '=':
                _current = new Token(TokenKind.Equals, "=", _position++);
                return;
            case '!':
                _current = new Token(TokenKind.Bang, "!", _position++);
                return;
            case '^':
                _current = new Token(TokenKind.Caret, "^", _position++);
                return;
            case '~':
                _current = new Token(TokenKind.Tilde, "~", _position++);
                return;
            case '"':
                _current = ParseStringToken();
                return;
        }

        var start = _position;
        while (_position < _text.Length)
        {
            ch = _text[_position];
            if (char.IsWhiteSpace(ch) || ch is '(' or ')' or '[' or ']' or '{' or '}' or '!' or '^' or '"' or '=' or '~')
            {
                break;
            }

            if (ch == ':')
            {
                if (_position + 1 < _text.Length && _text[_position + 1] == ':')
                {
                    _position += 2;
                    continue;
                }

                break;
            }

            _position++;
        }

        if (start == _position)
        {
            throw new FormatException($"Unexpected character '{_text[start]}' at position {start}.");
        }

        var text = _text.Substring(start, _position - start);
        _current = new Token(TokenKind.Identifier, text, start);
    }

    private Token ParseStringToken()
    {
        var start = _position;
        _position++; // Skip opening quote

        var builder = new System.Text.StringBuilder();
        var escaped = false;
        while (_position < _text.Length)
        {
            var ch = _text[_position++];
            if (escaped)
            {
                builder.Append(ch switch
                {
                    '\\' => '\\',
                    '"' => '"',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw new FormatException($"Unsupported escape sequence '\\{ch}' at position {_position - 1}."),
                });
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                return new Token(TokenKind.String, builder.ToString(), start);
            }

            builder.Append(ch);
        }

        throw new FormatException("Unterminated string literal.");
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length)
        {
            var ch = _text[_position];
            if (char.IsWhiteSpace(ch))
            {
                _position++;
                continue;
            }

            if (ch == '/' && _position + 1 < _text.Length && _text[_position + 1] == '/')
            {
                SkipLineComment();
                continue;
            }

            break;
        }
    }

    private void SkipLineComment()
    {
        _position += 2;
        while (_position < _text.Length)
        {
            var ch = _text[_position++];
            if (ch is '\n' or '\r')
            {
                break;
            }
        }
    }

    private void Expect(TokenKind kind)
    {
        if (_current.Kind != kind)
        {
            throw new FormatException($"Expected token '{kind}' but found '{_current.Text}' at position {_current.Position}.");
        }
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Position);

    private enum TokenKind
    {
        LParen,
        RParen,
        LBracket,
        RBracket,
        LBrace,
        RBrace,
        Identifier,
        String,
        Colon,
        Equals,
        Bang,
        Caret,
        Tilde,
        EndOfFile,
    }
}
