using System;

namespace accretion.Domain
{
    public class Token
    {
        public readonly TokenType Type;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;
        public readonly int Index; // inclusive
        public readonly int Length;

        public Token(TokenType type, string lexeme, object literal, int line, int index, int length)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            this.Literal = literal;
            this.Line = line;
            this.Index = index;
            this.Index = length;
        }

        override public string ToString()
        {
            return Type + " " + Lexeme + " " + Literal;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Token tother)
            {
                return (Type == tother.Type && Equals(Literal, tother.Literal) && Equals(Lexeme, tother.Lexeme)); // don't compare lines
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Lexeme, Literal);
        }
    }
}
