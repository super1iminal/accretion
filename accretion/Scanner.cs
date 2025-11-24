using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new();
        private int start = 0;
        private int current = 0; // current represents the next yet-to-be-read character
        private int line = 1;


        private static readonly Dictionary<string, TokenType> keywords = new()
        {
            {"and", TokenType.AND },
            {"class", TokenType.CLASS },
            {"else", TokenType.ELSE },
            {"false", TokenType.FALSE },
            {"for", TokenType.FOR },
            {"fun", TokenType.FUN },
            {"if", TokenType.IF },
            {"nil", TokenType.NIL },
            {"or", TokenType.OR },
            {"print", TokenType.PRINT },
            {"return", TokenType.RETURN },
            {"super", TokenType.SUPER },
            {"this", TokenType.THIS },
            {"true", TokenType.TRUE },
            {"var", TokenType.VAR },
            {"while", TokenType.WHILE }
        };


        public Scanner(string source)
        {
            this.source = source;
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                start = current;
                ScanToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, line));
            return tokens;
        }

        private bool IsAtEnd()
        {
            return current >= source.Length;
        }

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': AddToken(TokenType.LEFT_PAREN); break;
                case ')': AddToken(TokenType.RIGHT_PAREN); break;
                case '{': AddToken(TokenType.LEFT_BRACE); break;
                case '}': AddToken(TokenType.RIGHT_BRACE); break;
                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.DOT); break;
                case '-': AddToken(TokenType.MINUS); break;
                case '+': AddToken(TokenType.PLUS); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '*': AddToken(TokenType.STAR); break;
                case '?': AddToken(TokenType.QUESTION); break;
                case ':': AddToken(TokenType.COLON); break;
                case '!':
                    AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;

                case '/':
                    if (Match('/'))
                    {
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else
                    {
                        AddToken(TokenType.SLASH);
                    }
                    break;

                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.
                    break;

                case '\n':
                    line++;
                    break;

                case '"': MatchString(); break;

                default:
                    if (char.IsDigit(c))
                    {
                        MatchNumber();
                    }
                    else if (IsALphaU(c))
                    {
                        MatchIdentifier();
                    }
                    else
                    {
                        Accretion.Error(line, "Unexpected character.");
                    }
                    break;
            }
        }


        // CORE FUNCTIONALITY
        private void AddToken(TokenType ttype)
        {
            AddToken(ttype, null);
        }

        private void AddToken(TokenType ttype, object literal)
        {
            string lexeme = source.Substring(start, current - start);
            tokens.Add(new Token(ttype, lexeme, literal, line));
        }


        // SOURCE INTERACTION
        private char Advance()
        {
            return source[current++];
        }

        private bool Match(char expected)
        {
            if (Peek() != expected) return false;

            current++;
            return true;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        private char PeekNext()
        {
            if ((current + 1) >= source.Length) return '\0';
            return source[current + 1];
        }



        // MATCHERS
        private void MatchString()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') line++;
                Advance();
            }

            if (IsAtEnd())
            {
                Accretion.Error(line, "Unterminated string.");
                return;
            }

            // we had skipped this for the closing '"'
            Advance();

            string literal = source.Substring(start + 1, (current - start) - 2);
            AddToken(TokenType.STRING, literal);
        }

        private void MatchNumber()
        {
            while (char.IsDigit(Peek())) Advance();

            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                Advance();

                while (char.IsDigit(Peek())) Advance();
            }

            AddToken(TokenType.NUMBER, Double.Parse(source.Substring(start, current - start)));
        }

        private void MatchIdentifier()
        {
            while (IsAlphaNumU(Peek())) Advance();

            string literal = source.Substring(start, current - start);
            TokenType type;
            if (!keywords.TryGetValue(literal, out type)) 
            {
                type = TokenType.IDENTIFIER;
            }

            AddToken(type);
        }




        // STATIC HELPERS
        private static bool IsALphaU(char c)
        {
            return char.IsLetter(c) || (c == '_');
        }

        private static bool IsAlphaNumU(char c)
        {
            return char.IsLetterOrDigit(c) || (c == '_');
        }

    }
}
