using accretion.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace accretion.Resolvers
{

    public class AccType 
    {
        public readonly string Value;
        public enum NativeType
        {
            VOID,
            DOUBLE,
            STRING,
            BOOL,
            INT
        }

        public AccType(string value) { Value = value; }
        public AccType(Token value) { Value = value.Lexeme; }
        public AccType(NativeType ntype)
        {
            switch (ntype)
            {
                case NativeType.DOUBLE:
                    Value = "double";
                    break;
                case NativeType.STRING:
                    Value = "string";
                    break;
                case NativeType.VOID:
                    Value = "void";
                    break;
                case NativeType.BOOL:
                    Value = "bool";
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not AccType type) return false;
            if (type.Value == null) return false;

            return Equals(Value, type.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class FunType : AccType
    {
        // value is also ReturnType for consistency and resolvability
        public readonly AccType ReturnType; // change this to FunType to allow for returning functions
        public readonly List<AccType> ParamTypes;

        public FunType(AccType returnType, List<AccType> ParamTypes) : base(returnType.Value)
        {
            this.ReturnType = returnType;
            this.ParamTypes = ParamTypes;
        }

        public FunType(Token returnTypeToken, List<Token> paramTypeTokens) : base(returnTypeToken.Lexeme) 
        {
            ReturnType = new AccType(returnTypeToken);
            ParamTypes = new();
            foreach (Token paramTypeToken in paramTypeTokens)
            {
                ParamTypes.Add(new AccType(paramTypeToken));
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FunType type)) return false;
            if (obj == null) return false;

            if (!Equals(ReturnType,type.ReturnType)) return false;
            if (this.ParamTypes.Count != type.ParamTypes.Count) return false;

            for (int i = 0;  i < this.ParamTypes.Count; i++)
            {
                if (!Equals(ParamTypes[i], type.ParamTypes[i])) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ReturnType.GetHashCode(), ParamTypes.GetHashCode());
        }
    }

    public class Typer : Expr.IVisitor<AccType>, Stmt.IVisitor
    {
        private readonly Stack<Dictionary<Token, AccType>> scopes = new();

        private readonly HashSet<AccType> validTypes = new();

        private FunType currentFunctionType = null;

        public Typer()
        {
            foreach (AccType.NativeType nativeType in Enum.GetValues(typeof(AccType.NativeType)))
            {
                validTypes.Add(new AccType(nativeType));
            }
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
            DeclareVar(stmt.Name, stmt.Type);
            if (stmt.Initializer != null)
            {
                Resolve(stmt.Initializer); // make sure initializer type is the same type as the declaration
            }

            return;
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            FunType previousFunType = currentFunctionType;

            DeclareFun(stmt.Name, stmt.Returntype, stmt.Parametertypes);

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
            if (stmt.Value != null)
            {
                AccType returnType = Resolve(stmt.Value);
                if (!Equals(returnType, currentFunctionType.ReturnType))
                {
                    Accretion.Error(stmt.Keyword, $"Returned value does not match return type ({currentFunctionType.Value})");
                }
            } else
            {
                if (!Equals(currentFunctionType.ReturnType, new AccType(AccType.NativeType.VOID)) )
                {
                    Accretion.Error(stmt.Keyword, $"Returned value does not match return type {currentFunctionType.Value})");
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
            return ResolveVar(expr.Name); // will return either var type or function return type
        }

        public AccType VisitAssignExpr(Expr.Assign expr)
        {
            AccType varType = ResolveVar(expr.Name);
            AccType valueType = Resolve(expr.Value);

            if (Equals(varType, valueType)) return valueType;

            Accretion.Error(expr.Name, "Value type does not match declared type.");

            return valueType;
        }

        // MUST MATCH INTERPRETER OUTPUTS
        public AccType VisitBinaryExpr(Expr.Binary expr)
        {
            AccType left = Resolve(expr.Left);
            AccType right = Resolve(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                case TokenType.GREATER_EQUAL:
                case TokenType.LESS:
                case TokenType.LESS_EQUAL:
                case TokenType.MINUS:
                case TokenType.SLASH:
                case TokenType.STAR:
                    if (!IsNum(left, right)) Accretion.Error(expr.Op, "Operands must be numbers.");
                    if (IsDouble(left) || IsDouble(right)) return new AccType(AccType.NativeType.DOUBLE);
                    else return new AccType(AccType.NativeType.INT);

                case TokenType.PLUS:
                    if (IsNum(left, right))
                    {
                        if (IsDouble(left) || IsDouble(right))
                        {
                            return new AccType(AccType.NativeType.DOUBLE);
                        } else
                        {
                            return new AccType(AccType.NativeType.INT);
                        }
                    }
                    else if (IsString(left) || IsString(right) && IsAlphaNum(left, right))
                    {
                        return new AccType(AccType.NativeType.STRING);
                    }
                    else
                    {
                        Accretion.Error(expr.Op, "Operands must be numbers or strings.");
                        break;
                    }

                case TokenType.BANG_EQUAL:
                case TokenType.EQUAL_EQUAL:
                    return new AccType(AccType.NativeType.BOOL);

                }

            return null; // unreachable, but required to satisfy all paths must return a value
        }

        public AccType VisitCallExpr(Expr.Call expr)
        {
            AccType calleeType = Resolve(expr.Callee); // remember, callee can be an expression, but (should) resolve to a variable in interpreter
            if (calleeType is not FunType funType)
            {
                Accretion.Error(expr.Paren, "Cannot call a variable.");
                return new AccType(AccType.NativeType.VOID);
            }
            if (expr.Arguments.Count != funType.ParamTypes.Count)
            {
                Accretion.Error(expr.Paren, "Number of arguments does not match.");
                return funType.ReturnType;
            }
            for (int i = 0; i < expr.Arguments.Count; i++)
            {
                AccType argType = Resolve(expr.Arguments[i]);
                AccType paramType = funType.ParamTypes[i];

                if (!Equals(argType, paramType))
                {
                    Accretion.Error(expr.Argumentnames[i], "Argument type does not match parameter type.");
                }
            }

            return funType.ReturnType;
        }

        public AccType VisitGroupingExpr(Expr.Grouping expr)
        {
            AccType expressionType = Resolve(expr.Expression);
            return expressionType;
        }

        public AccType VisitLiteralExpr(Expr.Literal expr)
        {
            object value = expr.Value;

            if (value is null) return new AccType(AccType.NativeType.VOID);
            else if (value is string) return new AccType(AccType.NativeType.STRING);
            else if (value is double) return new AccType(AccType.NativeType.DOUBLE);
            else if (value is bool) return new AccType(AccType.NativeType.BOOL);

            throw new NotSupportedException("Weird. You shouldn't be here. How did you get here? Error code 1067.");
        }

        public AccType VisitLogicalExpr(Expr.Logical expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return new AccType(AccType.NativeType.BOOL);
        }

        public AccType VisitUnaryExpr(Expr.Unary expr)
        {
            AccType type = Resolve(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.BANG:
                    return new AccType(AccType.NativeType.BOOL);
                case TokenType.MINUS:
                    if (IsNum(type))
                    {
                        return type;
                    }
                    else
                    {
                        Accretion.Error(expr.Op, "Operand must be a number.");
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

            if (!Equals(consequentType, alternativeType)) Accretion.Error(expr.Name, "Both branches of the ternary expression must be of the same type.");

            return consequentType;
        }





        // HELPERS
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


        private void DeclareVar(Token name, Token typeToken)
        {
            // make sure var type is a valid type
            if (scopes.Count == 0) return;

            Dictionary<Token, AccType> scope = scopes.Peek();

            AccType type = new(typeToken);

            if (!validTypes.Contains(type))
            {
                Accretion.Error(typeToken, "Unknown type.");
            }
            else
            {
                scope[name] = type;
            }
        }

        private void DeclareFun(Token name, Token returnType, List<Token> paramTypes)
        {
            if (scopes.Count == 0) return;

            Dictionary<Token, AccType> scope = scopes.Peek();

            FunType type = new(returnType, paramTypes);
            currentFunctionType = type;

            bool failure = false;

            if (!validTypes.Contains(type.ReturnType))
            {
                Accretion.Error(returnType, "Unknown type.");
                failure = true;
            }

            // paramtypes checked in ResolveFunction

            if (!failure)
            {
                scope[name] = type;
            }

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

            // unreachable (if you run normal resolver first)
            return null;
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
            AccType intType = new AccType(AccType.NativeType.INT);
            foreach (AccType type in types)
            {
                if (!Equals(type, intType)) return false;
            }

            return true;
        }

        private static bool IsDouble(params AccType[] types)
        {
            AccType doubleType = new AccType(AccType.NativeType.DOUBLE);
            foreach (AccType type in types)
            {
                if (!Equals(type, doubleType)) return false;
            }

            return true;
        }

        private static bool IsString(params AccType[] types)
        {
            AccType stringType = new AccType(AccType.NativeType.STRING);
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

    // next TODO: not here. gotta extend Resolver to accept globals, make it abstract, extend it with ScopeResolver, TypeResolver, HeuristicResolver (heuristic is just a grab bag)
}
