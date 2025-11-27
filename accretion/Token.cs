using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public class Token
    {
        public readonly TokenType Type;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;

        public Token(TokenType type, string lexeme, object literal, int line)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            this.Literal = literal;
            this.Line = line;
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
                return (Type ==tother.Type && Equals(Literal, tother.Literal) && Equals(Lexeme, tother.Lexeme)); // don't compare lines
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Lexeme, Literal);
        }
    }
}
