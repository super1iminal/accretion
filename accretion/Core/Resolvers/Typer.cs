using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace accretion.Core.Resolvers
{
    // todo: treat ints as doubles in initializer and assignment if the type of var is double

    public class Typer : Expr.IVisitor<AccType>, Stmt.IVisitor
    {
        // typer stuff
        private struct Signature // todo
        {
            public Token SToken;
            public string Identifier; // todo I don't like relying on tokens
            public AccType AType;

            public Signature(string identifier, AccType AType)
            {
                this.Identifier = identifier;
                this.AType = AType;
                this.SToken = null;
            }

            public Signature(Token token, AccType AType)
            {
                this.SToken = token;
                this.AType = AType;
                this.Identifier = token.Lexeme;
            }
        }

        private readonly Stack<HashSet<Signature>> scopes = new(); // 

        private readonly HashSet<AccType> validTypes = NativeAccTypeFactory.nativeAccTypes;
        private readonly AccType ignoreType = new("ignore"); // anytime a variable or function is ignored due to non-existent types, it is set to ignore
                                                             // so the user doesn't get flooded with compile errors

        private AccType currentFunctionType = null; // to see if return value matches stated function return value

        // resolver stuff
        private readonly Interpreter interpreter;


        // heuristic stuff
        private Stack<HashSet<Token>> notAccessedYet = new(); // check whether all vars in a scope have been used
        private Stack<HashSet<Token>> notDefinedYet = new(); // check whether var is defined yet
        private bool inFunction = false; // to see whether we're returning outside of a function
        private bool inloop = false;


        // shared stuff
        private readonly ErrorManager errors;

        public Typer(Interpreter interpreter, ErrorManager errors)
        {
            foreach (var native in NativeRegistry.All)
            {
                scopes.Push(
                    new() {
                        new Signature(native.Name, native.Type),  // true because it's already been defined, obv
                    }
                );
            }

            this.errors = errors;
            this.interpreter = interpreter;
        }


        // STATEMENTS
        public void VisitBlockStmt(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return;
        }

        // variable declaration
        public void VisitVarStmt(Stmt.Var stmt)
        {
            // var stuff
            AccType initType = null;
            bool defined = false;
            if (stmt.Initializer != null)
            {
                initType = Resolve(stmt.Initializer);
                defined = true;
            }
            AccType varType = DeclareVar(stmt.Name, stmt.Type);
            if (defined) Define(stmt.Name);


            // init stuff
            if (Equals(varType, ignoreType)) return;

            initType = ImplicitCast(initType, varType);

            if (initType != null && !Equals(varType, initType))
            {
                errors.CompilerError(stmt.Type, "Variable initializer does not match variable type"); // todo: move this to the initializer resolution to use initializer token,
                                                                                                      // or implicitly split variable initialization into declaration and assignment to avoid this
            }

            return;
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            AccType previousFunType = currentFunctionType;

            DeclareFun(stmt.Name, stmt.Returntype, stmt.Parametertypes);
            // Define(stmt.Name); // unecessary, since DeclareFun does not add fun name to undeclared vars

            ResolveFunction(stmt);

            currentFunctionType = previousFunType;
            return;
        }

        // nothing statements
        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitIfStmt(Stmt.If stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Consequent);

            if (stmt.Alternative != null) Resolve(stmt.Alternative); // no control flow, we evaluate both branches
            return;
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitReturnStmt(Stmt.Return stmt)
        {
            if (!inFunction)
            {
                errors.CompilerError(stmt.Keyword, "Can't return from top-level code");
            }

            AccType returnValueType;
            if (stmt.Value != null)
            {
                returnValueType = Resolve(stmt.Value);
            }
            else
            {
                returnValueType = NativeAccTypeFactory.VOID;
            }


            // return stuff
            if (Equals(currentFunctionType, ignoreType) || Equals(returnValueType, ignoreType) || (currentFunctionType is not FunType cfReturnType)) return;

            returnValueType = ImplicitCast(returnValueType, cfReturnType.ReturnType);
            if (!Equals(returnValueType, ((FunType)currentFunctionType).ReturnType))
            {
                errors.CompilerError(stmt.Keyword, $"Returned value does not match return type ({currentFunctionType.Value})");
            }

            return;
        }

        public void VisitWhileStmt(Stmt.While stmt)
        {
            bool enclosingLoop = inloop;
            inloop = true;

            Resolve(stmt.Condition);
            Resolve(stmt.Body);

            inloop = enclosingLoop;
            return;
        }

        public void VisitJumpStmt(Stmt.Jump stmt)
        {
            if (!inloop) errors.CompilerError(stmt.Label, "Can't jump outside of a loop.");
            return;
        }





        // expressions
        // this is a "get" operation
        // can be a function or a var
        public AccType VisitVariableExpr(Expr.Variable expr)
        {
            if (scopes.Count > 1 && scopes.Peek().Any(k => k.Identifier == expr.Name.Lexeme))
            {
                errors.CompilerError(expr.Name, "Can't use a variable before it's defined.");
            }

            AccType varExprType = ResolveVar(expr, expr.Name);
            if (PropagateIgnore(varExprType)) return ignoreType;


            return varExprType;  // will return either var type or function return type
        }

        public AccType VisitAssignExpr(Expr.Assign expr)
        {
            AccType varType = ResolveVar(expr, expr.Name);
            AccType valueType = Resolve(expr.Value);

            notDefinedYet.Peek().Remove(expr.Name); // add every time var is called. maybe optimizable?

            // special typing
            if (PropagateIgnore(varType, valueType)) return ignoreType;
            valueType = ImplicitCast(valueType, varType);

            if (Equals(varType, valueType)) return valueType;

            errors.CompilerError(expr.Name, "Value type does not match declared type");

            return valueType;
        }

        // MUST MATCH INTERPRETER OUTPUTS
        public AccType VisitBinaryExpr(Expr.Binary expr)
        {
            AccType left = Resolve(expr.Left);
            AccType right = Resolve(expr.Right);
            if (PropagateIgnore(left, right)) return ignoreType;

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                case TokenType.GREATER_EQUAL:
                case TokenType.LESS:
                case TokenType.LESS_EQUAL:
                    if (!IsNum(left, right))
                    {
                        errors.CompilerError(expr.Op, "Operands must be numbers");
                        return ignoreType;
                    }
                    return NativeAccTypeFactory.BOOL;
                case TokenType.SLASH:
                    if (!IsNum(left, right))
                    {
                        errors.CompilerError(expr.Op, "Operands must be numbers");
                        return ignoreType;
                    }
                    return NativeAccTypeFactory.DOUBLE;
                case TokenType.MINUS:
                case TokenType.STAR:
                    if (!IsNum(left, right))
                    {
                        errors.CompilerError(expr.Op, "Operands must be numbers");
                        return ignoreType;
                    }
                    if (IsDouble(left) || IsDouble(right)) return NativeAccTypeFactory.DOUBLE;
                    else return NativeAccTypeFactory.INT;

                case TokenType.PLUS:
                    if (IsNum(left, right))
                    {
                        if (IsDouble(left) || IsDouble(right))
                        {
                            return NativeAccTypeFactory.DOUBLE;
                        } else
                        {
                            return NativeAccTypeFactory.INT;
                        }
                    }
                    else if (IsString(left) || IsString(right) && IsAlphaNum(left, right))
                    {
                        return NativeAccTypeFactory.STRING;
                    }
                    else
                    {
                        errors.CompilerError(expr.Op, "Operands must be numbers or strings");
                        return ignoreType;
                    }

                case TokenType.BANG_EQUAL:
                case TokenType.EQUAL_EQUAL:
                    return NativeAccTypeFactory.BOOL;

            }

            throw new NotImplementedException("Binary expr case unhandled. Error code 1231312.");
        }

        public AccType VisitCallExpr(Expr.Call expr)
        {
            AccType calleeType = Resolve(expr.Callee); // remember, callee can be an expression (so we need to resolve it), but (should) resolve to a *variable* in interpreter
            if (PropagateIgnore(calleeType)) return ignoreType;


            if (calleeType is not FunType funType)
            {
                errors.CompilerError(expr.Paren, "Cannot call a variable"); // TODO: add synchronization after errors that can't return a type. resolved with ignoreType?
                return ignoreType;
            }

            if (expr.Arguments.Count != funType.ParamTypes.Count)
            {
                errors.CompilerError(expr.Paren, "Number of arguments does not match");
                return funType.ReturnType;
            }
            for (int i = 0; i < expr.Arguments.Count; i++)
            {
                AccType argType = Resolve(expr.Arguments[i]);
                AccType paramType = funType.ParamTypes[i];
                if (PropagateIgnore(argType, paramType)) return ignoreType;

                if (!Equals(argType, paramType))
                {
                    errors.CompilerError(expr.Argumentnames[i], "Argument type does not match parameter type");
                }
            }

            return funType.ReturnType;
        }

        public AccType VisitGroupingExpr(Expr.Grouping expr)
        {
            AccType expressionType = Resolve(expr.Expression);
            if (PropagateIgnore(expressionType)) return ignoreType;

            return expressionType;
        }

        public AccType VisitLiteralExpr(Expr.Literal expr)
        {
            object value = expr.Value;

            return NativeAccTypeFactory.AccTypeFromObject(value); // value types should be restricted to native types in the parser
        }

        public AccType VisitLogicalExpr(Expr.Logical expr)
        {
            AccType left = Resolve(expr.Left);
            AccType right = Resolve(expr.Right);
            if (PropagateIgnore(left, right)) return ignoreType;
            return NativeAccTypeFactory.BOOL;
        }

        public AccType VisitUnaryExpr(Expr.Unary expr)
        {
            AccType type = Resolve(expr.Right);
            if (PropagateIgnore(type)) return ignoreType;

            switch (expr.Op.Type)
            {
                case TokenType.BANG:
                    return NativeAccTypeFactory.BOOL;
                case TokenType.MINUS:
                    if (IsNum(type))
                    {
                        return type;
                    }
                    else
                    {
                        errors.CompilerError(expr.Op, "Operand must be a number");
                        return type;
                    }

            }

            return null; // unreachable
        }

        public AccType VisitTernaryExpr(Expr.Ternary expr)
        {
            Resolve(expr.Condition);
            AccType consequentType = Resolve(expr.Consequent);
            AccType alternativeType = Resolve(expr.Alternative);

            if (PropagateIgnore(alternativeType, consequentType)) return ignoreType;

            if (!Equals(consequentType, alternativeType)) errors.CompilerError(expr.Name, "Both branches of the ternary expression must be of the same type");

            return consequentType;
        }







        // HELPERS

        public AccType ImplicitCast(AccType valueType, AccType sourceType)
        {
            // hardcoded (native) implicit casts:
            if (Equals(valueType, NativeAccTypeFactory.INT) && Equals(sourceType, NativeAccTypeFactory.DOUBLE))
            {
                return NativeAccTypeFactory.DOUBLE;
            }

            // todo: if classes, maybe do implcit casting here?

            return valueType;
        }

        /// <summary>
        ///  helper to check if any of the given types are of ignoreType. if so,
        ///  the ignore type should be propagated up the expression tree, since part of the expression is corrupted.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public bool PropagateIgnore(params AccType[] types)
        {
            foreach (AccType type in types)
            {
                if (Equals(type, ignoreType)) return true;
            }
            return false;
        }
        public void BeginResolve(List<Stmt> statements)
        {
            BeginScope();
            Resolve(statements);
            EndScope();
        }
        private void Resolve(List<Stmt> statements)
        {
            foreach (Stmt stmt in statements)
            {
                Resolve(stmt);
            }
        }

        private void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        private AccType Resolve(Expr expr)
        {
            return expr.Accept(this);
        }

        private void BeginScope()
        {
            scopes.Push(new());
            notAccessedYet.Push(new HashSet<Token>());
            notDefinedYet.Push(new HashSet<Token>());
        }

        private void EndScope()
        {
            HashSet<Signature> closedScope = scopes.Pop();
            HashSet<Token> varsNotAccessedYet = notAccessedYet.Pop();
            HashSet<Token> varsNotDefinedYet = notDefinedYet.Pop();

            foreach (Token token in varsNotAccessedYet) errors.CompilerWarning(token, "Variable/Function is never used");
            foreach (Token token in varsNotDefinedYet) errors.CompilerWarning(token, "Variable is declared but never assigned a value");
        }


        private AccType DeclareVar(Token name, Token typeToken)
        {
            if (scopes.Count == 0) throw new ApplicationException("Woah. You shouldn't be here. Error code 0918.");

            HashSet<Signature> scope = scopes.Peek();


            AccType type = new(typeToken.Lexeme);
            AccType validatedType = ValidOrIgnore(type, typeToken);

            // check for existence (name comparison)
            if (scope.Any(k => k.Identifier == name.Lexeme))
            {
                errors.CompilerError(name, "Already a variable or function declared with this name in this scope");
                return validatedType;
            }


            Signature sig = new(name, validatedType);
            
            notAccessedYet.Peek().Add(name);
            notDefinedYet.Peek().Add(name);
            scope.Add(sig); // not defined yet
            return validatedType;
        }

        private void DeclareFun(Token name, Token returnType, List<Token> paramTypes)
        {
            if (scopes.Count == 0) return;

            HashSet<Signature> scope = scopes.Peek();

            FunType type = new(returnType, paramTypes);
            AccType ignoreCheck = ValidOrIgnore(type.ReturnType, returnType);
            AccType verifiedType = Equals(ignoreCheck, ignoreType) ? ignoreType : type;


            // if existing other var w/ same name, should fail
            // if existing other fun w/ same name but diff param types, should pass
            // else, fail


            foreach (Signature match in scope.Where(k => k.Identifier == name.Lexeme))
            {
                // todo: what to do with ignore types here?
                if (match.AType is not FunType otherType)
                {
                    errors.CompilerError(name, "Already a variable declared with this name in this scope");
                    return;
                }
                else
                {
                    if (Equals(type, otherType))
                    {
                        errors.CompilerError(name, "Already a function declared with the same signature in this scope");
                    }
                }
            }


            currentFunctionType = verifiedType;

            Signature sig = new(name, verifiedType);

            notAccessedYet.Peek().Add(name);
            scope.Add(sig);
            // todo: paramtypes checked in ResolveFunction
        }

        /// <summary>
        /// helper that returns either the given type, if it is valid, or sends an error and returns the ignore type if the type is invalid.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeToken"></param>
        /// <returns></returns>
        private AccType ValidOrIgnore(AccType type, Token typeToken)
        {
            if (!validTypes.Contains(type)) {
                errors.CompilerError(typeToken, "Unknown type");
                return ignoreType; 
            }
            else return type;
        }

        private void Define(Token name) // unused
        {
            notDefinedYet.Peek().Remove(name);
        }

        private AccType ResolveVar(Expr expr, Token name)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).Any(k => k.Identifier == name.Lexeme))
                {
                    interpreter.Resolve(expr, i);
                    notAccessedYet.Peek().Remove(name);
                    return scopes.ElementAt(i).Single(k => (k.Identifier == name.Lexeme) && (k.AType is not FunType)).AType; // throws error if more than one non-function variable with same name. intended.
                }
            }
            errors.CompilerError(name, "There is no declared variable or function with this name");
            return ignoreType;
        }

        // resolve fun is diff from resolvevar. resolvefun resolves the inside of a function. resolvevar finds the var/fun assocaited with a token
        private void ResolveFunction(Stmt.Function function)
        {
            // make sure function return type is a valid type
            BeginScope();
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                Token param = function.Parameters[i];
                Token paramType = function.Parametertypes[i];

                DeclareVar(param, paramType);
                Define(param);
            }

            Resolve(function.Body); // different from how interepreter handles function declarations
                                    // at runtime, declaring a function doesn't do anything with function body
                                    // in static analysis, we traverse body
            EndScope();
        }

        private static bool IsInt(params AccType[] types)
        {
            AccType intType = NativeAccTypeFactory.INT;
            foreach (AccType type in types)
            {
                if (!Equals(type, intType)) return false;
            }

            return true;
        }

        private static bool IsDouble(params AccType[] types)
        {
            AccType doubleType = NativeAccTypeFactory.DOUBLE;
            foreach (AccType type in types)
            {
                if (!Equals(type, doubleType)) return false;
            }

            return true;
        }

        private static bool IsString(params AccType[] types)
        {
            AccType stringType = NativeAccTypeFactory.STRING;
            foreach (AccType type in types)
            {
                if (!Equals(type, stringType)) return false;
            }

            return true;
        }

        private static bool IsNum(params AccType[] types)
        {
            foreach (AccType type in types)
            {
                if (!(IsInt(type) || IsDouble(type))) return false;
            }

            return true;
        }

        private static bool IsAlphaNum(params AccType[] types)
        {
            foreach (AccType type in types)
            {
                if (!(IsInt(type) || IsDouble(type) || IsString(type))) return false;
            }

            return true;
        }

    }
}

// todo: combination of resolver and type mostly complete. still need to integrate interpreter and typer tho for resolutions