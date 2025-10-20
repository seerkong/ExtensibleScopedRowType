using System;
using System.Collections.Immutable;

namespace RowLang.Core.Syntax;

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

    private SExprNode ParseAnnotatedNode()
    {
        var prefixes = ImmutableArray.CreateBuilder<SExprNode>();
        while (_current.Kind == TokenKind.Bang)
        {
            NextToken();
            prefixes.Add(ParseAnnotatedNode());
        }

        var core = ParseCoreNode();

        var postfixes = ImmutableArray.CreateBuilder<SExprNode>();
        while (_current.Kind == TokenKind.Caret)
        {
            NextToken();
            postfixes.Add(ParseAnnotatedNode());
        }

        if (prefixes.Count == 0 && postfixes.Count == 0)
        {
            return core;
        }

        return core with
        {
            PrefixAnnotations = prefixes.ToImmutable(),
            PostfixAnnotations = postfixes.ToImmutable(),
        };
    }

    private SExprNode ParseCoreNode()
    {
        return _current.Kind switch
        {
            TokenKind.LParen => ParseList(),
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
            case '!':
                _current = new Token(TokenKind.Bang, "!", _position++);
                return;
            case '^':
                _current = new Token(TokenKind.Caret, "^", _position++);
                return;
        }

        var start = _position;
        while (_position < _text.Length)
        {
            ch = _text[_position];
            if (char.IsWhiteSpace(ch) || ch is '(' or ')' or '!' or '^')
            {
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

            break;
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
        Identifier,
        Bang,
        Caret,
        EndOfFile,
    }
}
