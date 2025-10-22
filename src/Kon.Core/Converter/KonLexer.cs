using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kon.Core.Converter;

public class KonLexer
{
    public enum TokenType
    {
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Colon, // :
        ContainerSubscript, // ::
        Semicolon,
        Comma,
        Dot, // .
        StaticSubscript, // .:
        LeftParen,
        RightParen,
        Exclamation,
        At,
        Percent,
        Dollar,
        Hash,
        VerticalLine,
        BackQuote,  // `
        Tilde,  // ~
        String,
        RawString,
        Number,
        True,
        False,
        Null,
        Word,
        Whitespace,
        Newline,
        Comment,
        Eof
    }

    public record Token(TokenType Type, string Lexeme, int Line, int Column);

    private readonly string _source;
    private readonly List<Token> _tokens = new();
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    private static readonly Regex NumberPattern = new("^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?", RegexOptions.Compiled);
    private static readonly Regex WordPattern = new("^(?:[a-zA-Z_][a-zA-Z0-9_]*[\\?]?|\\+[\\+]?|-[>-]?|\\*|/|>[=]?|<[=]?|=[=]?)", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new("^[ \\t\\r]+", RegexOptions.Compiled);
    private static readonly Regex NewlinePattern = new("^\\n", RegexOptions.Compiled);
    private static readonly Regex CommentPattern = new("^\\/\\/[^\\n]*", RegexOptions.Compiled);

    public KonLexer(string source)
    {
        _source = source ?? string.Empty;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.Eof, string.Empty, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        _start = _current;
        var c = Advance();
        switch (c)
        {
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case '[':
                AddToken(TokenType.LeftBracket);
                break;
            case ']':
                AddToken(TokenType.RightBracket);
                break;
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case '.':
                if (Peek() == ':')
                {
                    Advance();
                    AddToken(TokenType.StaticSubscript);
                }
                else
                {
                    AddToken(TokenType.Dot);
                }

                break;
            case '!':
                AddToken(TokenType.Exclamation);
                break;
            case '@':
                AddToken(TokenType.At);
                break;
            case '%':
                AddToken(TokenType.Percent);
                break;
            case ':':
                if (Peek() == ':')
                {
                    Advance();
                    AddToken(TokenType.ContainerSubscript);
                }
                else
                {
                    AddToken(TokenType.Colon);
                }
                break;
            case '$':
                AddToken(TokenType.Dollar);
                break;
            case '#':
                AddToken(TokenType.Hash);
                break;
            case '|':
                AddToken(TokenType.VerticalLine);
                break;
            case '`':
                AddToken(TokenType.BackQuote);
                break;
            case '~':
                AddToken(TokenType.Tilde);
                break;
            case '\'':
                RawString();
                break;
            case '"':
                String();
                break;
            case '\n':
                AddToken(TokenType.Newline);
                _line++;
                _column = 1;
                break;
            case '/':
                if (Peek() == '/')
                {
                    Advance();
                    while (Peek() != '\n' && !IsAtEnd())
                    {
                        Advance();
                    }

                    AddToken(TokenType.Comment);
                    break;
                }

                goto default;
            default:
                if (IsWhitespace(c))
                {
                    if (MatchPattern(WhitespacePattern))
                    {
                        AddToken(TokenType.Whitespace);
                    }
                }
                else if (IsOperatorStart(c))
                {
                    if (c == '-' && IsDigit(Peek()))
                    {
                        _current--;
                        _column--;
                        if (MatchPattern(NumberPattern))
                        {
                            AddToken(TokenType.Number);
                        }
                    }
                    else
                    {
                        _current--;
                        _column--;
                        if (MatchPattern(WordPattern))
                        {
                            AddToken(TokenType.Word);
                        }
                    }
                }
                else if (IsDigit(c))
                {
                    _current--;
                    _column--;
                    if (MatchPattern(NumberPattern))
                    {
                        AddToken(TokenType.Number);
                    }
                }
                else if (IsAlpha(c))
                {
                    _current--;
                    _column--;
                    if (MatchPattern(WordPattern))
                    {
                        var word = _source.Substring(_start, _current - _start);
                        switch (word)
                        {
                            case "true":
                                AddToken(TokenType.True);
                                break;
                            case "false":
                                AddToken(TokenType.False);
                                break;
                            case "null":
                                AddToken(TokenType.Null);
                                break;
                            default:
                                AddToken(TokenType.Word);
                                break;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected character: {c} at line {_line}, column {_column}");
                }

                break;
        }
    }

    private void RawString()
    {
        while (Peek() != '\'' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            throw new InvalidOperationException($"Unterminated string at line {_line}, column {_column}");
        }

        Advance();
        AddToken(TokenType.RawString);
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }
            else if (Peek() == '\\')
            {
                Advance();
            }

            Advance();
        }

        if (IsAtEnd())
        {
            throw new InvalidOperationException($"Unterminated string at line {_line}, column {_column}");
        }

        Advance();
        AddToken(TokenType.String);
    }

    private bool MatchPattern(Regex pattern)
    {
        var remaining = _source[_current..];
        var match = pattern.Match(remaining);
        if (match.Success && match.Index == 0)
        {
            _current += match.Length;
            _column += match.Length;
            return true;
        }

        return false;
    }

    private char Advance()
    {
        _current++;
        _column++;
        return _source[_current - 1];
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private bool IsAtEnd() => _current >= _source.Length;

    private void AddToken(TokenType type)
    {
        var text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, _line, _column - text.Length));
    }

    private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\r';

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsAlpha(char c) =>
        c is >= 'a' and <= 'z' ||
        c is >= 'A' and <= 'Z' ||
        c == '_';

    private static bool IsOperatorStart(char c) =>
        c is '+' or '-' or '*' or '/' or '>' or '<' or '=';
}
