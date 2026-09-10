using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace accretion.Core.ResolverTools
{
    public class ExprVisitor : Expr.IVisitor<AccType>
    {
        private readonly Typer typer;

        public ExprVisitor(Typer typer)
        {
            this.typer = typer;
        }

        // this is a "get" operation
        // cannot be a function; is only a variable; functions identifiers (variables) are handled else WHERE
        public AccType VisitVariableExpr(Expr.Variable expr)
        {
            // if (scopes.Count > 1 && notDefinedYet.Peek().Any(k => k.Lexeme == expr.Name.Lexeme))
            // {
            //     errors.CompilerError(expr.Name, "Can't use a variable before it's defined.");
            // }

            AccType varExprType = ResolveVar(expr, expr.Name);
            if (typer.PropagateIgnore(varExprType)) return typer.ignoreType;


            return varExprType;
        }

        public AccType VisitAssignExpr(Expr.Assign expr)
        {
            AccType varType = ResolveVar(expr, expr.Name);
            AccType valueType = Resolve(expr.Value);

            typer.Define(expr.Name);

            // special typing
            if (typer.PropagateIgnore(varType, valueType)) return typer.ignoreType;
            valueType = typer.ImplicitCast(valueType, varType);

            if (Equals(varType, valueType)) return valueType;

            typer.errors.CompilerError(expr.Name, "Value type does not match declared type");

            return valueType;
        }

        // MUST MATCH INTERPRETER OUTPUTS
        public AccType VisitBinaryExpr(Expr.Binary expr)
        {
            AccType left = Resolve(expr.Left);
            AccType right = Resolve(expr.Right);
            if (typer.PropagateIgnore(left, right)) return typer.ignoreType;

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                case TokenType.GREATER_EQUAL:
                case TokenType.LESS:
                case TokenType.LESS_EQUAL:
                    if (!IsNum(left, right))
                    {
                        typer.errors.CompilerError(expr.Op, "Operands must be numbers");
                        return typer.ignoreType;
                    }
                    return NativeAccTypeFactory.BOOL;
                case TokenType.SLASH:
                    if (!IsNum(left, right))
                    {
                        typer.errors.CompilerError(expr.Op, "Operands must be numbers");
                        return typer.ignoreType;
                    }
                    return NativeAccTypeFactory.DOUBLE;
                case TokenType.MINUS:
                case TokenType.STAR:
                    if (!IsNum(left, right))
                    {
                        typer.errors.CompilerError(expr.Op, "Operands must be numbers");
                        return typer.ignoreType;
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
                        typer.errors.CompilerError(expr.Op, "Operands must be numbers or strings");
                        return typer.ignoreType;
                    }

                case TokenType.BANG_EQUAL:
                case TokenType.EQUAL_EQUAL:
                    return NativeAccTypeFactory.BOOL;

            }

            throw new NotImplementedException("Binary expr case unhandled. Error code 1231312.");
        }

        public AccType VisitCallExpr(Expr.Call expr)
        {
            // we start by checking the arguments
            List<AccType> argTypes = new();
            foreach (Expr arg in expr.Arguments)
            {
                AccType argType = Resolve(arg);
                if (typer.PropagateIgnore(argType)) return typer.ignoreType;
                argTypes.Add(argType);
            }

            if (expr.Callee is Expr.Variable varExpr)
            {
                // we handle the direct case (e.g., ->foo<-(x)) manually
                AccType retType = ResolveFun(expr, varExpr.Name, argTypes);
                if (typer.PropagateIgnore(retType) || (retType is not FunType funType)) return typer.ignoreType;

                return funType.ReturnType;
            }

            // if not a direct case (e.g., ->foo(x)<-(1), resolve normally, since foo(x) will only resolve to a single type
            AccType calleeType = Resolve(expr.Callee);
            if (typer.PropagateIgnore(calleeType)) return typer.ignoreType;

            if (calleeType is not FunType exprFunType)
            {
                typer.errors.CompilerError(expr.Paren, "Cannot call a variable");
                return typer.ignoreType;
            }

            if (expr.Arguments.Count != exprFunType.ParamTypes.Count)
            {
                typer.errors.CompilerError(expr.Paren, "Number of arguments does not match");
                return exprFunType.ReturnType;
            }
            for (int i = 0; i < expr.Arguments.Count; i++)
            {
                AccType argType = Resolve(expr.Arguments[i]);
                AccType paramType = exprFunType.ParamTypes[i];
                if (typer.PropagateIgnore(argType, paramType)) return typer.ignoreType;

                if (!Equals(argType, paramType))
                {
                    typer.errors.CompilerError(expr.Argumentnames[i], "Argument type does not match parameter type");
                }
            }

            return exprFunType.ReturnType;
        }

        public AccType VisitGroupingExpr(Expr.Grouping expr)
        {
            AccType expressionType = Resolve(expr.Expression);
            if (typer.PropagateIgnore(expressionType)) return typer.ignoreType;

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
            if (typer.PropagateIgnore(left, right)) return typer.ignoreType;
            return NativeAccTypeFactory.BOOL;
        }

        public AccType VisitUnaryExpr(Expr.Unary expr)
        {
            AccType type = Resolve(expr.Right);
            if (typer.PropagateIgnore(type)) return typer.ignoreType;

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
                        typer.errors.CompilerError(expr.Op, "Operand must be a number");
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

            if (typer.PropagateIgnore(alternativeType, consequentType)) return typer.ignoreType;

            if (!Equals(consequentType, alternativeType)) typer.errors.CompilerError(expr.Name, "Both branches of the ternary expression must be of the same type");

            return consequentType;
        }


        // HELPERS

        public AccType Resolve(Expr expr)
        {
            return expr.Accept(this);
        }

        // ok, ResolveVar is called directly when we access a *variable* (not a function) to figure out what variable we want
        private AccType ResolveVar(Expr expr, Token name)
        {
            for (int i = 0; i < typer.scopes.Count; i++)
            {
                if (typer.scopes.ElementAt(i).Any(k => k.Identifier == name.Lexeme))
                {
                    typer.interpreter.Resolve(expr, i, name.Lexeme); // todo important: variables are refered to by their normal names
                    if ((i + 1)!= typer.scopes.Count) typer.notAccessedYet.ElementAt(i).Remove(name.Lexeme);
                    return typer.scopes.ElementAt(i).Single(k => (k.Identifier == name.Lexeme) && (k.AType is not FunType)).AType; // throws error if more than one non-function variable with same name. intended.
                }
            }
            typer.errors.CompilerError(name, "There is no declared variable with this name");
            return typer.ignoreType;
        }

        /// <summary>
        /// ResolveFun is called directly when we call a function to figure out what function we called, and also registers it in the interpreter
        /// </summary>
        private FunType? FindMatchingOverload(List<FunType> candidates, List<AccType> argTypes, bool allowImplicitCasts)
        {
            foreach (FunType candidate in candidates)
            {
                if (candidate.ParamTypes.Count != argTypes.Count) continue;

                bool match = true;
                for (int j = 0; j < argTypes.Count; j++)
                {
                    AccType argType = allowImplicitCasts ? typer.ImplicitCast(argTypes[j], candidate.ParamTypes[j]) : argTypes[j];

                    if ((!Equals(argType, candidate.ParamTypes[j]))
                        && (!Equals(candidate.ParamTypes[j], typer.ignoreType))
                        && (!Equals(argType, typer.ignoreType)))
                    {
                        match = false;
                        break;
                    }
                }

                if (match) return candidate;
            }

            return null;
        }

        private AccType ResolveFun(Expr.Call expr, Token name, List<AccType> argTypes)
        {
            for (int i=0; i < typer.scopes.Count; i++)
            {
                List<FunType> possibleFuncs = typer.scopes.ElementAt(i)
                    .Where(k => k.Identifier == name.Lexeme && k.AType is FunType)
                    .Select(k => (FunType)k.AType)
                    .ToList();

                if (possibleFuncs.Count == 0) continue;

                // try exact match first, then fall back to implicit casts
                FunType? matched = FindMatchingOverload(possibleFuncs, argTypes, allowImplicitCasts: false)
                    ?? FindMatchingOverload(possibleFuncs, argTypes, allowImplicitCasts: true);

                if (matched != null)
                {
                    typer.interpreter.Resolve(expr.Callee, i, Typer.MangleName(name.Lexeme, matched.ParamTypes)); // todo: since environemnts now use both depth and mangled name, we need to add that to the locals dict in interpreter
                    if ((i + 1) != typer.scopes.Count) typer.notAccessedYet.ElementAt(i).Remove(Typer.MangleName(name.Lexeme, matched.ParamTypes));
                    return matched;
                }

                // we keep going if we haven't found a match, all the way up the list of scopes
            }

            typer.errors.CompilerError(name, "No matching function found for the given name and arguments");
            return typer.ignoreType;
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
