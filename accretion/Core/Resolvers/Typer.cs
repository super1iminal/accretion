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
        private readonly Stack<Dictionary<Token, AccType>> scopes = new();
        private readonly Dictionary<string, AccType> globalTypes = new();

        private readonly HashSet<AccType> validTypes = new();
        private readonly AccType ignoreType = new("ignore"); // anytime a variable or function is ignored due to non-existent types, it is set to ignore
                                                             // so the user doesn't get flooded with compile errors

        private AccType currentFunctionReturnType = null; // to see if return value matches stated function return value

        private readonly ErrorManager errors;

        public Typer(ErrorManager errors)
        {
            validTypes.UnionWith(NativeAccTypeFactory.nativeAccTypes);

            foreach (var native in NativeRegistry.All)
            {
                globalTypes[native.Name] = native.Type;
            }

            this.errors = errors;
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
            AccType initType = null;
            if (stmt.Initializer != null)
            {
                initType = Resolve(stmt.Initializer);
            }
            AccType varType = DeclareVar(stmt.Name, stmt.Type);

            if (varType == ignoreType)
            {
                return;
            }

            if (initType != null && !Equals(varType, initType))
            {
                errors.CompilerError(stmt.Type, "Variable initializer does not match variable type"); // todo: move this to the initializer resolution to use initializer token,
                                                                                                      // or implicitly split variable initialization into declaration and assignment to avoid this
            }

            return;
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            AccType previousFunType = currentFunctionReturnType;

            DeclareFun(stmt.Name, stmt.Returntype, stmt.Parametertypes);

            ResolveFunction(stmt);

            currentFunctionReturnType = previousFunType;
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
            if (Equals(currentFunctionReturnType, ignoreType) || currentFunctionReturnType is not FunType) return;

            if (stmt.Value != null)
            {
                AccType returnType = Resolve(stmt.Value);
            }
            else
            {
                AccType returnType = NativeAccTypeFactory.VOID;
            }
            {
                AccType returnType = Resolve(stmt.Value);
                if (!Equals(returnType, ((FunType)currentFunctionReturnType).ReturnType))
                {
                    errors.CompilerError(stmt.Keyword, $"Returned value does not match return type ({currentFunctionReturnType.Value})");
                }
            }

            return;
        }

        public void VisitWhileStmt(Stmt.While stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return;
        }

        public void VisitJumpStmt(Stmt.Jump stmt)
        {
            return;
        }





        // expressions
        // this is a "get" operation
        public AccType VisitVariableExpr(Expr.Variable expr)
        {
            AccType varExprType = ResolveVar(expr.Name);
            if (PropagateIgnore(varExprType)) return ignoreType;


            return varExprType;  // will return either var type or function return type
        }

        public AccType VisitAssignExpr(Expr.Assign expr)
        {
            AccType varType = ResolveVar(expr.Name);
            AccType valueType = Resolve(expr.Value);
            if (PropagateIgnore(varType, valueType)) return ignoreType;

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
                case TokenType.MINUS:
                case TokenType.SLASH:
                case TokenType.STAR:
                    if (!IsNum(left, right)) errors.CompilerError(expr.Op, "Operands must be numbers");
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
                        break;
                    }

                case TokenType.BANG_EQUAL:
                case TokenType.EQUAL_EQUAL:
                    return NativeAccTypeFactory.BOOL;

                }

            return null; // unreachable, but required to satisfy all paths must return a value
        }

        public AccType VisitCallExpr(Expr.Call expr)
        {
            AccType calleeType = Resolve(expr.Callee); // remember, callee can be an expression, but (should) resolve to a variable in interpreter
            if (PropagateIgnore(calleeType)) return ignoreType;
            if (calleeType is not FunType funType)
            {
                errors.CompilerError(expr.Paren, "Cannot call a variable"); // TODO: add synchronization after errors that can't return a type
                return NativeAccTypeFactory.VOID;
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
            scopes.Push(new Dictionary<Token, AccType>());
        }

        private void EndScope()
        {
            scopes.Pop();
        }


        private AccType DeclareVar(Token name, Token typeToken)
        {
            if (scopes.Count == 0) throw new ApplicationException("Woah. You shouldn't be here. Error code 0918.");

            Dictionary<Token, AccType> scope = scopes.Peek();

            AccType type = new(typeToken.Lexeme);

            AccType validatedType = ValidOrIgnore(type, typeToken);

            scope[name] = validatedType;
            return validatedType;
        }

        private void DeclareFun(Token name, Token returnType, List<Token> paramTypes)
        {
            if (scopes.Count == 0) return;

            Dictionary<Token, AccType> scope = scopes.Peek();

            FunType type = new(returnType, paramTypes);

            AccType ignoreCheck = ValidOrIgnore(type.ReturnType, returnType);

            AccType verifiedType;

            if (Equals(ignoreCheck, ignoreType))
            {
                 verifiedType = ignoreType;
            }
            else
            {
                verifiedType = type;
            }
            
            currentFunctionReturnType = verifiedType;
            scope[name] = verifiedType;
            // paramtypes checked in ResolveFunction
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

        //private void Define(Token name) // unused
        //{
        //    if (scopes.Count == 0) return;
        //}

        private AccType ResolveVar(Token name)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).ContainsKey(name))
                {
                    return scopes.ElementAt(i)[name];
                }
            }

            if (globalTypes.ContainsKey(name.Lexeme))
            {
                return globalTypes[name.Lexeme];
            } else
            {
                // we should NOT get here since we fill non-existent types with the ignore type (so all elements should be in scope)
                throw new ApplicationException("Mismatch between resolver and typer. Error code 6767.");
            }

            // unreachable (if you run normal resolver first)
            // return null;
        }

        private void ResolveFunction(Stmt.Function function)
        {
            // make sure function return type is a valid type
            BeginScope();
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                Token param = function.Parameters[i];
                Token paramType = function.Parametertypes[i];

                DeclareVar(param, paramType);
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
