using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public enum TokenType
    {
        // Single-character tokens.
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE, COLON,
        COMMA, DOT, MINUS, PLUS, QUESTION, SEMICOLON, SLASH, STAR,

        // One or two character tokens.
        BANG, BANG_EQUAL,
        EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL,
        LESS, LESS_EQUAL,

        // Literals.
        IDENTIFIER, STRING, DOUBLE, INT,

        // Keywords.
        AND, BREAK, CONTINUE, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
        PRINT, RETURN, TRUE, VAR, WHILE,

        EOF,


        // class-related keywords
        CLASS, SUPER, THIS
    }
}
