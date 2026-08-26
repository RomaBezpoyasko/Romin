using System;
using System.Collections.Generic;
using System.Text;

using static System.Net.WebRequestMethods;

namespace Romin
{
    // =========================================================
    // LEXER
    // =========================================================
    public class Lexer
    {
        #region Properties 

        // Current source code position.
        private int _line = 1;
        private int _column = 1;

        // Source text and current character position.
        private string _text;
        private readonly string _file;
        private int _pos;

        // Returns the current character or '\0' when the end of the source is reached.
        char Cur => _pos >= _text.Length ? '\0' : _text[_pos];

        // Stack used to track indentation levels.
        Stack<int> _indents = new();

        // Parentheses and brackets nesting depth.
        // NewLine tokens are suppressed while inside them.
        int _parenDepth, _bracketDepth;

        // Indicates that the source contains the 'base' keyword.
        public bool HasBase;

        #endregion

        #region Constructor

        /// <summary>
        /// Create new instance of lexer class
        /// </summary>
        /// <param name="text"></param>
        public Lexer(string text, string file = null)
        {
            _text = text;

            // The first indentation level is always zero.
            _indents.Push(0);

            _file = file;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Next possition
        /// </summary>
        void Next()
        {
            // Update line and column information when moving to the next character.
            if (Cur == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _pos++;
        }

        /// <summary>
        /// Tokenize
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            // Process the source character by character until EOF.
            while (Cur != '\0')
            {
                // Handle line endings.
                if (Cur == '\n' || Cur == '\r')
                {
                    SkipNewlineChars();

                    // Ignore physical line breaks inside parentheses or brackets.
                    if (_parenDepth > 0 || _bracketDepth > 0)
                        continue;

                    Add(TokenType.NewLine, "\n");

                    // Process indentation of the next logical line.
                    while (true)
                    {
                        int start = _pos;
                        int spaces = 0;

                        // Count leading spaces.
                        while (Cur == ' ')
                        {
                            spaces++;
                            Next();
                        }

                        // Skip empty lines.
                        if (Cur == '\r' || Cur == '\n')
                        {
                            SkipNewlineChars();
                            continue;
                        }

                        // Ignore a line that contains only a comment.
                        if (Cur == '/' && Peek() == '/')
                        {
                            while (Cur != '\r' &&
                                   Cur != '\n' &&
                                   Cur != '\0')
                            {
                                Next();
                            }

                            continue;
                        }

                        int current = _indents.Peek();

                        // A greater indentation level creates an Indent token.
                        if (spaces > current)
                        {
                            _indents.Push(spaces);
                            Add(TokenType.Indent, "");
                        }

                        // A smaller indentation level creates one or more Dedent tokens.
                        while (_indents.Count > 1 &&
                                spaces < _indents.Peek())
                        {
                            _indents.Pop();
                            Add(TokenType.Dedent, "");
                        }

                        break;
                    }

                    continue;
                }

                // Ignore spaces and tabs between tokens.
                if (Cur == ' ' || Cur == '\t')
                {
                    Next();
                    continue;
                }

                // Identifiers and keywords start with a letter or underscore.
                if (char.IsLetter(Cur) || Cur == '_')
                {
                    tokens.Add(ReadWord());
                    continue;
                }

                // Read integer or floating-point numbers.
                if (char.IsDigit(Cur))
                {
                    tokens.Add(ReadNumber()); continue;
                }

                // Interpolated string.
                // Example: $"Hello {name}"
                if (Cur == '$' && (Peek() == '"' || Peek() == '\''))
                {
                    char quote = Peek();
                    Next(); // skip $
                    tokens.Add(ReadString(quote, TokenType.InterpolatedString));
                    continue;
                }

                // Usual string.
                if (Cur == '"' || Cur == '\'')
                {
                    tokens.Add(ReadString(Cur, TokenType.String));
                    continue;
                }

                // Process operators and punctuation characters.
                switch (Cur)
                {
                    case '+':
                        {
                            Next();

                            // Increment operator.
                            if (Cur == '+')
                            {
                                Add(TokenType.PlusPlus, "++");
                            }
                            // Addition assignment operator.
                            else if (Cur == '=')
                            {
                                Add(TokenType.PlusEqual, "+=");
                            }
                            // Regular addition operator.
                            else
                            {
                                Add(TokenType.Plus, "+");
                                continue;
                            }

                            break;
                        }

                    case '-':
                        {
                            Next();

                            // Decrement operator.
                            if (Cur == '-')
                            {
                                Add(TokenType.MinusMinus, "--");
                            }
                            // Regular subtraction operator.
                            else
                            {
                                Add(TokenType.Minus, "-");
                                continue;
                            }

                            break;
                        }

                    case '*':
                        Add(TokenType.Star, "*");
                        break;

                    case '/':
                        {
                            Next();

                            // SINGLE LINE COMMENT
                            // //
                            if (Cur == '/')
                            {
                                while (Cur != '\n' &&
                                    Cur != '\r' &&
                                    Cur != '\0')
                                    Next();

                                continue;
                            }

                            // MULTI LINE COMMENT
                            // /* ... */
                            if (Cur == '*')
                            {
                                Next();

                                while (Cur != '\0')
                                {
                                    // Search for the closing */
                                    if (Cur == '*')
                                    {
                                        Next();

                                        if (Cur == '/')
                                        {
                                            Next();
                                            break;
                                        }

                                        continue;
                                    }

                                    Next();
                                }

                                continue;
                            }

                            // DIVISION OPERATOR
                            // /
                            Add(TokenType.Slash, "/");
                            continue;
                        }

                    case '>':
                        {
                            Next();

                            // Greater-than-or-equal operator.
                            if (Cur == '=')
                            {
                                Add(TokenType.GreaterEqual, ">=");
                            }
                            // Greater-than operator.
                            else
                            {
                                Add(TokenType.Greater, ">");
                                continue;
                            }

                            break;
                        }

                    case '<':
                        {
                            Next();

                            // Less-than-or-equal operator.
                            if (Cur == '=')
                            {
                                Add(TokenType.LessEqual, "<=");
                            }
                            // Less-than operator.
                            else
                            {
                                Add(TokenType.Less, "<");
                                continue;
                            }

                            break;
                        }

                    case '=':
                        {
                            Next();

                            // Equality operator.
                            if (Cur == '=')
                            {
                                Add(TokenType.EqualEqual, "==");
                            }
                            // Lambda/function arrow operator.
                            else if (Cur == '>')
                            {
                                Add(TokenType.Arrow, "=>");
                            }
                            // Assignment operator.
                            else
                            {
                                Add(TokenType.Equals, "=");
                                continue;
                            }

                            break;
                        }

                    case '!':
                        {
                            Next();

                            // Not-equal operator.
                            if (Cur == '=')
                            {
                                Add(TokenType.NotEqual, "!=");
                            }
                            // Logical NOT operator.
                            else
                            {
                                Add(TokenType.Not, "!");
                                continue;
                            }

                            break;
                        }

                    case '.':
                        {
                            Next();

                            // Range operator.
                            if (Cur == '.')
                            {
                                Next();
                                Add(TokenType.DotDot, "..");
                                continue;
                            }

                            // Member access operator.
                            Add(TokenType.Dot, ".");
                            continue;
                        }

                    case '(':
                        // Increase parentheses nesting depth.
                        _parenDepth++;
                        Add(TokenType.LParen, "(");
                        break;

                    case ')':
                        // Decrease parentheses nesting depth.
                        _parenDepth--;
                        Add(TokenType.RParen, ")");
                        break;

                    case '[':
                        // Increase brackets nesting depth.
                        _bracketDepth++;
                        Add(TokenType.LBracket, "[");
                        break;

                    case ']':
                        // Decrease brackets nesting depth.
                        _bracketDepth--;
                        Add(TokenType.RBracket, "]");
                        break;

                    case ',':
                        Add(TokenType.Comma, ",");
                        break;

                    case '&':
                        {
                            Next();

                            // Logical AND operator requires two '&' characters.
                            if (Cur == '&')
                            {
                                Add(TokenType.AndAnd, "&&");
                                break;
                            }

                            throw new Exception("Unexpected &");
                        }

                    case '|':
                        {
                            Next();

                            // Logical OR operator requires two '|' characters.
                            if (Cur == '|')
                            {
                                Add(TokenType.OrOr, "||");
                                break;
                            }

                            throw new Exception("Unexpected |");
                        }

                    case '?':
                        {
                            Next();

                            // Null-coalescing operator.
                            if (Cur == '?')
                            {
                                Add(TokenType.Coalesce, "??");
                            }
                            // Question mark operator.
                            else
                            {
                                Add(TokenType.Question, "?");
                                continue;
                            }

                            break;
                        }

                    case ':':
                        Add(TokenType.Colon, ":");
                        break;

                    case ';':
                        // Semicolons are currently ignored by the lexer.
                        Next();
                        continue;

                    default:
                        // The lexer does not recognize the current character.
                        throw
                            new Exception($"Unexpected char {Cur}");
                }

                Next();
            }

            // Close all remaining indentation levels at the end of the source.
            while (_indents.Peek() > 0)
            {
                _indents.Pop();
                Add(TokenType.Dedent, "");
            }

            // Add the final end-of-file token.
            Add(TokenType.EOF, "");

            void Add(TokenType t, string value)
            {
                // Create and append a token using the current source position.
                tokens.Add(Tok(t, value));
            }

            return tokens;
        }

        /// <summary>
        /// Look ahead in the source without changing the current position.
        /// </summary>
        char Peek(int offset = 1)
        {
            int p = _pos + offset;
            return p >= _text.Length ? '\0' : _text[p];
        }

        /// <summary>
        /// Skip CR/LF line ending characters.
        /// Supports both Windows (CRLF) and Unix (LF) line endings.
        /// </summary>
        void SkipNewlineChars()
        {
            if (Cur == '\r')
                Next();

            if (Cur == '\n')
                Next();
        }

        /// <summary>
        /// Read word
        /// </summary>
        /// <returns></returns>
        Token ReadWord()
        {
            string s = "";

            // Read letters, digits and underscores as a single identifier.
            while (char.IsLetterOrDigit(Cur) || Cur == '_')
            {
                s += Cur;
                Next();
            }

            // Remember that the source contains the 'base' keyword.
            if (s == "base")
                HasBase = true;

            // Convert known keywords to their corresponding token types.
            // Unknown words are treated as identifiers.
            return s switch
            {
                "if" => Tok(TokenType.If, s),
                "while" => Tok(TokenType.While, s),
                "fn" => Tok(TokenType.Fn, s),
                "return" => Tok(TokenType.Return, s),
                "true" => Tok(TokenType.True, s),
                "false" => Tok(TokenType.False, s),
                "for" => Tok(TokenType.For, s),
                "in" => Tok(TokenType.In, s),
                "and" => Tok(TokenType.And, s),
                "or" => Tok(TokenType.Or, s),
                "else" => Tok(TokenType.Else, s),
                "null" => Tok(TokenType.Null, s),
                "try" => Tok(TokenType.Try, s),
                "catch" => Tok(TokenType.Catch, s),
                "new" => Tok(TokenType.New, s),
                "use" => Tok(TokenType.Use, s),
                "base" => Tok(TokenType.Base, s),
                _ => Tok(TokenType.Identifier, s)
            };
        }

        /// <summary>
        /// Read number
        /// </summary>
        /// <returns></returns>
        Token ReadNumber()
        {
            // Save the starting source position of the number.
            int line = _line;
            int column = _column;
            int pos = _pos;

            var sb = new StringBuilder();

            // Read the integer part.
            while (char.IsDigit(Cur))
            {
                sb.Append(Cur);
                Next();
            }

            // Read the fractional part if this is a floating-point number.
            // The second dot of a range operator (..) must not be consumed.
            if (Cur == '.'
                && Peek() != '.'
                && char.IsDigit(Peek()))
            {
                sb.Append('.');
                Next();

                while (char.IsDigit(Cur))
                {
                    sb.Append(Cur);
                    Next();
                }
            }

            // Decimal suffix.
            // Examples:
            // 123m
            // 123.45m
            // 123.45M
            if (Cur == 'm' || Cur == 'M')
            {
                sb.Append(Cur);
                Next();
            }

            return Tok(
                TokenType.Number,
                sb.ToString(),
                line,
                column,
                pos);
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="quote"></param>
        /// <param name="tokenType"></param>
        /// <returns></returns>
        Token ReadString(char quote, TokenType tokenType)
        {
            // Save the starting position of the string.
            int line = _line;
            int column = _column;
            int pos = _pos;

            // Skip the opening quote.
            Next();

            var sb = new StringBuilder();

            // Read characters until the closing quote or end of source.
            while (Cur != quote && Cur != '\0')
            {
                // Handle escape sequences.
                if (Cur == '\\')
                {
                    Next();

                    switch (Cur)
                    {
                        case 'n':
                            sb.Append('\n');
                            break;

                        case 'r':
                            sb.Append('\r');
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case '\\':
                            sb.Append('\\');
                            break;

                        case '"':
                            sb.Append('"');
                            break;

                        case '\'':
                            sb.Append('\'');
                            break;

                        // Unknown escape sequences are preserved as-is.
                        default:
                            sb.Append(Cur);
                            break;
                    }

                    Next();
                    continue;
                }

                sb.Append(Cur);
                Next();
            }

            // Skip the closing quote if it exists.
            if (Cur == quote)
                Next();

            return Tok(
                tokenType,
                sb.ToString(),
                line,
                column,
                pos);
        }

        /// <summary>
        /// Add token
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Token Tok(TokenType type, string value) =>
                Tok(type, value,
                _line,
                _column,
                _pos);

        /// <summary>
        /// Create new token
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        Token Tok(TokenType type,
                string value,
                int line,
                int column,
                int pos)
        {
            // Create a token containing its type, value and source location.
            return new Token
            {
                Type = type,
                Value = value,

                Line = line,
                Column = column,
                Position = pos,

                File = _file
            };
        }

        #endregion
    }
}