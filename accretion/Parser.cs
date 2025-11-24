using System;
using System.Collections.Generic;
using System.Data.Common;

namespace accretion
{
    /* 
     * a parser has two jobs:
     *  - given a valid sequence of tokens, produce an AST
     *  - given an invalid sequence of tokens, detect any errors and tell user their mistakes
     *  
     */
    public class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        private class ParseError : Exception { }

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Stmt> Parse()
        {
            List<Stmt> statements = new();
            while (!IsAtEnd())
            {
                statements.Add(Statement());
            }

            return statements;
        }


        // ERRORS
        /// <summary>
        /// when we encounter an erroneous syntax, we don't want the parser to freak out and label everything after it an error too (ahem, c)
        /// so we need a synchronization point to "reset" the parser to keep going and catch more potential errors, or at least not freak out
        /// this synchro point is after a semicolon
        /// </summary>
        private void Synchronize()
        {
            Advance(); // current token is bad, let's advance

            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.SEMICOLON) return;

                switch (Peek().Type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                Advance();
            }
        }

        private ParseError Error(Token token, string message)
        {
            Accretion.Error(token, message);
            return new ParseError();
        }






        // ======== CORE HELPERS ======== 
        private bool Match(params TokenType[] types)
        {
            foreach (TokenType t in types)
            {
                if (Check(t))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }


        // advances only if the token matches the type. similar to match but returns token instead of bool
        // also throws an error if no match
        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();

            throw Error(Peek(), message);
        }



        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        /// <summary>
        /// returns current token, and advances to the next unread token
        /// </summary>
        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        /// <summary>
        /// returns most recently consumed token
        /// </summary>
        /// <returns>returns most recently consumed token</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Token Previous()
        {
            if (current > 0)
            {
                return tokens[current - 1];
            }
            throw new ArgumentOutOfRangeException("trying to access previous token at current = 0");
        }











        // ======== STATEMENT RULES ======== 
        private Stmt Statement()
        {
            if (Match(TokenType.PRINT)) return PrintStatement();

            return ExpressionStatement(); // fallthrough case, hard to recognize an expression statement on its own
        }


        private Stmt PrintStatement()
        {
            Expr value = Expression(); // remember, expressions resolve to a value

            Consume(TokenType.SEMICOLON, "Expect ';' after value.");

            return new Stmt.Print(value);
        }


        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();

            Consume(TokenType.SEMICOLON, "Expect ';' after value.");

            return new Stmt.Expression(expr);
        }









        // ======== EXPRESSION RULES ======== 
        private Expr Expression()
        {
            return TernaryRule();
        }

        private Expr TernaryRule()
        {
            Expr expr = EqualityRule();

            if (Match(TokenType.QUESTION))
            {
                // current is currently one token after ?
                Expr consequent = TernaryRule();
                if (Match(TokenType.COLON))
                {
                    // current is currently one token after :
                    Expr alternative = TernaryRule();
                    expr = new Expr.Ternary(expr, consequent, alternative);
                } 
                else
                {
                    throw Error(Peek(), "Expect alternative expression (:) in ternary.");
                }
            }

            return expr;
        }

        private Expr EqualityRule()
        {
            // equality -> comparison ( ( "!=" | "==" ) comparison )*

            // matches an equality *or anything of higher precedence* (through comparison rule)
            Expr expr = ComparisonRule();

            while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token op = Previous(); // match advances if it finds a match
                Expr right = ComparisonRule(); // this probably advances, meaning the next loop will match another potential equality
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }


        private Expr ComparisonRule()
        {
            // comparison -> term ( ( ">" | ">=" | "<" | "<=" ) term )*
            
            Expr expr = TermRule();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = Previous();
                Expr right = TermRule();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;

        }


        private Expr TermRule()
        {
            // term -> factor ( ( "+" | "-") factor )*

            Expr expr = FactorRule();

            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                Token op = Previous();
                Expr right = FactorRule();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }


        private Expr FactorRule()
        {
            // factor -> unary ( ( "/" | "*") unary )*

            Expr expr = UnaryRule();

            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = Previous();
                Expr right = UnaryRule();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr UnaryRule()
        {
            // unary -> ("!" | "-") unary | primary

            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                Token op = Previous();
                Expr right = UnaryRule();
                return new Expr.Unary(op, right);
            }

            return PrimaryRule();
        }

        private Expr PrimaryRule()
        {
            // primary -> NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")"

            if (Match(TokenType.FALSE)) return new Expr.Literal(false);
            if (Match(TokenType.TRUE)) return new Expr.Literal(true);
            if (Match(TokenType.NIL)) return new Expr.Literal(null);

            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expr.Literal(Previous().Literal);
            }

            // groups have highest priority
            if (Match(TokenType.LEFT_PAREN))
            {
                Expr expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw Error(Peek(), "Expect expression.");
        }

        
    }
}
