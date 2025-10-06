using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Filtering
{
    public static class ODataFilterParser
    {
        public static FilterNode? Parse(string? filterExpression)
        {
            if (string.IsNullOrWhiteSpace(filterExpression))
            {
                return null;
            }

            var tokenizer = new Tokenizer(filterExpression);
            var tokens = tokenizer.Tokenize();
            if (tokens.Count == 0)
            {
                return null;
            }

            var parser = new Parser(tokens);
            return parser.ParseExpression();
        }

        private enum TokenType
        {
            Identifier,
            String,
            Number,
            OpenParen,
            CloseParen,
            Comma
        }

        private sealed record Token(TokenType Type, string Value);

        private sealed class Tokenizer
        {
            private readonly string _text;

            public Tokenizer(string text) => _text = text;

            public List<Token> Tokenize()
            {
                var result = new List<Token>();
                var span = _text.AsSpan();
                int index = 0;

                while (index < span.Length)
                {
                    var current = span[index];
                    if (char.IsWhiteSpace(current))
                    {
                        index++;
                        continue;
                    }

                    if (char.IsLetter(current) || current == '_')
                    {
                        var start = index;
                        index++;
                        while (index < span.Length && (char.IsLetterOrDigit(span[index]) || span[index] is '_'))
                        {
                            index++;
                        }

                        result.Add(new Token(TokenType.Identifier, span[start..index].ToString()));
                        continue;
                    }

                    if (char.IsDigit(current) || (current is '-' && index + 1 < span.Length && char.IsDigit(span[index + 1])))
                    {
                        var start = index;
                        index++;
                        while (index < span.Length && (char.IsDigit(span[index]) || span[index] is '.'))
                        {
                            index++;
                        }

                        result.Add(new Token(TokenType.Number, span[start..index].ToString()));
                        continue;
                    }

                    switch (current)
                    {
                        case '(':
                            result.Add(new Token(TokenType.OpenParen, "("));
                            index++;
                            break;
                        case ')':
                            result.Add(new Token(TokenType.CloseParen, ")"));
                            index++;
                            break;
                        case ',':
                            result.Add(new Token(TokenType.Comma, ","));
                            index++;
                            break;
                        case '\'':
                            result.Add(new Token(TokenType.String, ReadString(span, ref index)));
                            break;
                        default:
                            throw new FormatException($"Unexpected character '{current}' in filter expression.");
                    }
                }

                return result;
            }

            private static string ReadString(ReadOnlySpan<char> span, ref int index)
            {
                index++; // skip opening quote
                var buffer = new System.Text.StringBuilder();
                while (index < span.Length)
                {
                    var current = span[index];
                    if (current == '\'')
                    {
                        if (index + 1 < span.Length && span[index + 1] == '\'')
                        {
                            buffer.Append('\'');
                            index += 2;
                            continue;
                        }

                        index++;
                        return buffer.ToString();
                    }

                    buffer.Append(current);
                    index++;
                }

                throw new FormatException("Unterminated string literal in filter expression.");
            }
        }

        private sealed class Parser
        {
            private readonly IReadOnlyList<Token> _tokens;
            private int _position;

            public Parser(IReadOnlyList<Token> tokens) => _tokens = tokens;

            public FilterNode ParseExpression()
            {
                var expression = ParseOr();
                if (!IsAtEnd)
                {
                    throw new FormatException("Unexpected token sequence in filter expression.");
                }

                return expression;
            }

            private FilterNode ParseOr()
            {
                var left = ParseAnd();
                while (MatchKeyword("or"))
                {
                    var right = ParseAnd();
                    left = new BinaryFilterNode("or", left, right);
                }

                return left;
            }

            private FilterNode ParseAnd()
            {
                var left = ParseComparison();
                while (MatchKeyword("and"))
                {
                    var right = ParseComparison();
                    left = new BinaryFilterNode("and", left, right);
                }

                return left;
            }

            private FilterNode ParseComparison()
            {
                var left = ParsePrimary();
                if (MatchKeyword("eq") || MatchKeyword("ne") || MatchKeyword("gt") || MatchKeyword("ge") || MatchKeyword("lt") || MatchKeyword("le"))
                {
                    var op = Previous.Value.ToLowerInvariant();
                    var right = ParsePrimary();
                    return new BinaryFilterNode(op, left, right);
                }

                return left;
            }

            private FilterNode ParsePrimary()
            {
                if (MatchKeyword("not"))
                {
                    return new UnaryFilterNode("not", ParsePrimary());
                }

                if (Match(TokenType.Identifier, out var identifier))
                {
                    var identifierValue = identifier.Value;
                    if (Match(TokenType.OpenParen, out _))
                    {
                        var args = new List<FilterNode>();
                        if (!Match(TokenType.CloseParen, out _))
                        {
                            do
                            {
                                args.Add(ParseExpression());
                            } while (Match(TokenType.Comma, out _));

                            Expect(TokenType.CloseParen);
                        }

                        return new FunctionFilterNode(identifierValue.ToLowerInvariant(), args);
                    }

                    if (TryParseLiteralFromIdentifier(identifierValue, out var literal))
                    {
                        return new LiteralFilterNode(literal);
                    }

                    return new PropertyFilterNode(identifierValue);
                }

                if (Match(TokenType.String, out var stringToken))
                {
                    return new LiteralFilterNode(stringToken.Value);
                }

                if (Match(TokenType.Number, out var numberToken))
                {
                    var value = ParseNumber(numberToken.Value);
                    return new LiteralFilterNode(value);
                }

                if (Match(TokenType.OpenParen, out _))
                {
                    var expression = ParseExpression();
                    Expect(TokenType.CloseParen);
                    return expression;
                }

                throw new FormatException("Invalid filter expression.");
            }

            private static bool TryParseLiteralFromIdentifier(string identifier, out object? literal)
            {
                switch (identifier.ToLowerInvariant())
                {
                    case "true":
                        literal = true;
                        return true;
                    case "false":
                        literal = false;
                        return true;
                    case "null":
                        literal = null;
                        return true;
                    default:
                        literal = null;
                        return false;
                }
            }

            private static object ParseNumber(string value)
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
                {
                    return longResult;
                }

                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalResult))
                {
                    return decimalResult;
                }

                throw new FormatException($"Unable to parse numeric literal '{value}'.");
            }

            private bool Match(TokenType type, out Token token)
            {
                if (!IsAtEnd && _tokens[_position].Type == type)
                {
                    token = _tokens[_position++];
                    return true;
                }

                token = default;
                return false;
            }

            private bool MatchKeyword(string keyword)
            {
                if (!IsAtEnd && _tokens[_position].Type == TokenType.Identifier &&
                    string.Equals(_tokens[_position].Value, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    _position++;
                    return true;
                }

                return false;
            }

            private void Expect(TokenType type)
            {
                if (!Match(type, out _))
                {
                    throw new FormatException($"Expected token of type {type}.");
                }
            }

            private Token Previous => _tokens[_position - 1];

            private bool IsAtEnd => _position >= _tokens.Count;
        }
    }
}
