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
     *  
     *  parser goes thru sequence of tokens by a top-down approach, checking for pattern matches and if so, consuming the sequence of tokens that matches a pattern
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
                statements.Add(Declaration());
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

        private bool CheckNext(TokenType type)
        {
            if (current + 1 >= tokens.Count) return false;
            return PeekNext().Type == type;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token PeekNext()
        {
            if (current + 1 >= tokens.Count) return tokens[tokens.Count - 1];
            return tokens[current + 1];
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










        // ======== DECLARATION RULES ======== 
        private Stmt Declaration()
        {
            // declaration  -> (IDENTIFIER IDENTIFIER (varDecl | funDecl)) | statement
            // note that statements can contain identifiers but only a single one, not two consecutive identifiers
            try
            {
                if (Check(TokenType.IDENTIFIER) && CheckNext(TokenType.IDENTIFIER))
                {
                    Token type = Advance();
                    Token name = Advance();

                    if (Check(TokenType.LEFT_PAREN))
                    {
                        return FunctionDeclaration(type, name);
                    }
                    else
                    {
                       return VarDeclaration(type, name);
                    }
                }

                return Statement();
            }
            catch (ParseError)
            {
                Synchronize(); // TODO: fix synchronize
                return null;
            }
        }

        private Stmt FunctionDeclaration(Token type, Token name)
        {
            // funDecl -> "(" parameters? ")" block
            // parameters -> IDENTIFIER IDENTIFIER ( IDENTIFIER IDENTIFIER ",")*
            Consume(TokenType.LEFT_PAREN, $"Expect '(' after function name.");
            List<Token> parameters = new();
            List<Token> parameterTypes = new();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 parameters.");
                    }

                    Token paramType = Consume(TokenType.IDENTIFIER, "Expect parameter type.");
                    Token paramName = Consume(TokenType.IDENTIFIER, "Expect parameter name.");

                    parameters.Add(paramName);
                    parameterTypes.Add(paramType);

                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

            Consume(TokenType.LEFT_BRACE, $"Expect '{{' before function body.");
            List<Stmt> body = Block();

            return new Stmt.Function(name, parameters, parameterTypes, body, type);
        }

        private Stmt VarDeclaration(Token type, Token name)
        {
            // varDecl      -> ( "=" expression )? ";"

            Expr initializer = null;
            if (Match(TokenType.EQUAL))
            {
                initializer = Expression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration."); // handles any other tokens after IDENTIFIER IDENTIFIER xxxx that don't match var or function
            return new Stmt.Var(name, type, initializer);
        }








        // ======== STATEMENT RULES ======== 
        private Stmt Statement()
        {
            // statement    -> exprStmt | printStmt
            if (Match(TokenType.BREAK) || Match(TokenType.CONTINUE)) return JumpStatement();
            if (Match(TokenType.FOR)) return ForStatement();
            if (Match(TokenType.IF)) return IfStatement();
            if (Match(TokenType.PRINT)) return PrintStatement();
            if (Match(TokenType.RETURN)) return ReturnStatement();
            if (Match(TokenType.WHILE)) return WhileStatement();
            if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

            return ExpressionStatement(); // fallthrough case, hard to recognize an expression statement on its own
        }

        private Stmt IfStatement()
        {
            // ifStmt       -> "if" "(" expression ")" statement ("else" statement)?
            // ambiguity: if (a) if (b) x(); else y();
            //  does the else belong to the first if or the second?
            // sol'n: else if bound to the nearest if
            // which is what happens, since we cval expression, then check for another statement (which could include if + else)

            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt consequent = Statement(); // allows for blocks
            Stmt alternative = null;

            if (Match(TokenType.ELSE))
            {
                alternative = Statement();
            }

            return new Stmt.If(condition, consequent, alternative);
        }

        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            Stmt body = Statement();

            return new Stmt.While(condition, body);
        }

        // we don't actually have a Stmt.For class, instead we turn it into a While stmt by "Desugaring"
        // this also allows us to have the declared var in scope of the for loop by creating a virtual block
        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (Match(TokenType.SEMICOLON))
            {
                initializer = null;
            } else if (Check(TokenType.IDENTIFIER) && CheckNext(TokenType.IDENTIFIER)) // need to have both because a single identifier can be part of an expr stmt
            {
                Token type = Advance();
                Token name = Advance();
                initializer = VarDeclaration(type, name);
            } else
            {
                initializer = ExpressionStatement();
            }

            Expr condition = null;
            if (!Check(TokenType.SEMICOLON))
            {
                condition = Expression();
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!Check(TokenType.RIGHT_PAREN))
            {
                increment = Expression();
            }
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses");

            Stmt body = Statement();

            if (increment != null)
            {
                body = new Stmt.Block(new List<Stmt> { body, new Stmt.Expression(increment)});
            }

            if (condition == null) condition = new Expr.Literal(true);
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt> { initializer, body });
            }

            return body;
        }

        private Stmt JumpStatement()
        {
            // TODO: add static check to see if jump statement is outside of loop
            Token label = Previous();

            Consume(TokenType.SEMICOLON, $"Expect ';' after {label.Lexeme}.");

            Stmt stmt = new Stmt.Jump(label);
            return stmt;
        }

        private List<Stmt> Block()
        {
            // block -> "{" declaration* "}"
            List<Stmt> statements = new();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }


        private Stmt PrintStatement()
        {
            // printStmt    -> "print" expression ";"
            Expr value = Expression(); // remember, expressions resolve to a value

            Consume(TokenType.SEMICOLON, "Expect ';' after value.");

            return new Stmt.Print(value);
        }


        private Stmt ExpressionStatement()
        {
            // exprStmt     -> expression ";"
            Expr expr = Expression();

            Consume(TokenType.SEMICOLON, "Expect ';' after value.");

            return new Stmt.Expression(expr);
        }

        private Stmt ReturnStatement()
        {
            Token keyword = Previous();
            Expr value = null;
            if (!Check(TokenType.SEMICOLON))
            {
                value = Expression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
        }









        // ======== EXPRESSION RULES ======== 
        private Expr Expression()
        {
            // expression -> assignment
            return AssignmentRule();
        }

        private Expr AssignmentRule()
        {
            // assignment -> IDENTIFIER "=" assignment | ternary
            // allows for x = y = 4
            //  this would be x = (y = 4)

            // problem: there may be many tokens until =
            //  e.g., node.left = new Node()
            // but we only have a single token of lookahead (we would otherwise look for =, like in declaration)

            Expr expr = OrRule(); // base case, could be a variable
            // we parse the left side as an rvalue, but then we convert it into an l-value representation
                // works because every valid assignment target is also just a normal expression, so we can use that and parse the left side as if it was an expression
                // (but it's not, since it doesn't resolve to anything)
                // however, the LHS needs to be a valid assignment target (currently just a variable)

            if (Match(TokenType.EQUAL))
            {
                Token equals = Previous(); // gets the token we just read in Match() (=)
                Expr value = AssignmentRule();
            
                if (expr is Expr.Variable var) {
                    Token name = var.Name;
                    return new Expr.Assign(name, value);
                }

                Error(equals, "Invalid assignment target.");
            }

            return expr; // at the end of recursion loop, expr will return the literal (e.g., 4)
                        // in the second-to-last loop, expr will return the assignment (e.g., Assign(y, 4)
                        // in the third-to-last loop, expr will return the assignment (Assign(x, y))
        }

        private Expr OrRule()
        {
            Expr expr = AndRule();

            while (Match(TokenType.OR))
            {
                Token op = Previous();
                Expr right = AndRule();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr AndRule()
        {
            Expr expr = TernaryRule();

            while (Match(TokenType.AND))
            {
                Token op = Previous();
                Expr right = TernaryRule();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }


        private Expr TernaryRule()
        {
            // ternary -> equality ( "?" ternary ":" ternary )?

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

            return CallRule();
        }

        private Expr CallRule()
        {
            Expr expr = PrimaryRule();

            while (true)
            {
                if (Match(TokenType.LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                } else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            List<Expr> arguments = new();

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count >= 255)
                    {
                        Error(Peek(), "Can't have more than 255 arguments.");
                    }
                    arguments.Add(Expression());
                } while (Match(TokenType.COMMA));
            }

            Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
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

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous());
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
