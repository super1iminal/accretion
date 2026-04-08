using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace accretion.Core.Resolvers
{

    public class Typer : Expr.IVisitor<AccType>, Stmt.IVisitor
    {
        // typer stuff
        private struct Signature 
        {
            public Token SToken; // token is used for error reporting purposes
            public string Identifier; // used as canon ID
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

            public override bool Equals(object obj)
            {
                if (obj is not Signature otherSig) return false;
                return Equals(Identifier, otherSig.Identifier) && Equals(AType, otherSig.AType);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Identifier, AType.GetHashCode());
            }
        }

        private readonly Stack<HashSet<Signature>> scopes = new(); // 


        private readonly HashSet<AccType> validTypes = NativeAccTypeFactory.nativeAccTypes;
        private readonly AccType ignoreType = new("ignore"); // anytime a variable or function is ignored due to non-existent types, it is set to ignore
                                                             // so the user doesn't get flooded with compile errors

        // resolver stuff
        private readonly Interpreter interpreter;


        // heuristic stuff
        // also, tokens w/ same lexeme are diff, so no worries about overloaded funcs here
        private Stack<HashSet<Token>> notAccessedYet = new(); // check whether all vars in a scope have been used
        private Stack<HashSet<Token>> notDefinedYet = new(); // check whether var is defined yet
        private bool inFunction = false; // to see whether we're returning outside of a function
        private bool inloop = false;
        private AccType currentFunctionType = null; // to see if return value matches stated function return value


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
        // cannot be a function; is only a variable; functions identifiers (variables) are handled else WHERE
        public AccType VisitVariableExpr(Expr.Variable expr)
        {
            if (scopes.Count > 1 && notDefinedYet.Peek().Any(k => k.Lexeme == expr.Name.Lexeme))
            {
                errors.CompilerError(expr.Name, "Can't use a variable before it's defined.");
            }

            AccType varExprType = ResolveVar(expr, expr.Name);
            if (PropagateIgnore(varExprType)) return ignoreType;


            return varExprType; 
        }

        public AccType VisitAssignExpr(Expr.Assign expr)
        {
            AccType varType = ResolveVar(expr, expr.Name);
            AccType valueType = Resolve(expr.Value);

            Define(expr.Name);

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
            // AccType calleeType = Resolve(expr.Callee); // remember, callee can be an expression (so we need to resolve it), but (should) resolve to a *variable* in interpreter
            // we are no longer *starting* with an automatic resolution of calleeType. If it's a variable (function identifier), we handle it manually. if it's a function that returns another func, we allow it.

            // we start by checking the arguments
            List<AccType> argTypes = new();
            foreach (Expr arg in expr.Arguments)
            {
                AccType argType = Resolve(arg);
                if (PropagateIgnore(argType)) return ignoreType;
                argTypes.Add(argType);
            }

            if (expr.Callee is Expr.Variable varExpr)
            {
                // we handle the direct case (e.g., ->foo<-(x)) manually
                AccType retType = ResolveFun(expr, varExpr.Name, argTypes);
                if (PropagateIgnore(retType) || (retType is not FunType funType)) return ignoreType;

                return funType.ReturnType;
            }

            // if not a direct case (e.g., ->foo(x)<-(1), resolve normally, since foo(x) will only resolve to a single type
            AccType calleeType = Resolve(expr.Callee);
            if (PropagateIgnore(calleeType)) return ignoreType;

            if (calleeType is not FunType exprFunType)
            {
                errors.CompilerError(expr.Paren, "Cannot call a variable");
                return ignoreType;
            }

            if (expr.Arguments.Count != exprFunType.ParamTypes.Count)
            {
                errors.CompilerError(expr.Paren, "Number of arguments does not match");
                return exprFunType.ReturnType;
            }
            for (int i = 0; i < expr.Arguments.Count; i++)
            {
                AccType argType = Resolve(expr.Arguments[i]);
                AccType paramType = exprFunType.ParamTypes[i];
                if (PropagateIgnore(argType, paramType)) return ignoreType;

                if (!Equals(argType, paramType))
                {
                    errors.CompilerError(expr.Argumentnames[i], "Argument type does not match parameter type");
                }
            }

            return exprFunType.ReturnType;
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
            notDefinedYet.Peek().Add(name); // add every time var is called. maybe optimizable?
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

        // ok, ResolveVar is called directly when we access a *variable* (not a function) to figure out what variable we want
        private AccType ResolveVar(Expr expr, Token name)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).Any(k => k.Identifier == name.Lexeme))
                {
                    interpreter.Resolve(expr, i, name.Lexeme); // todo important: variables are refered to by their normal names
                    if (i > 0)  notAccessedYet.ElementAt(i).Remove(name);
                    return scopes.ElementAt(i).Single(k => (k.Identifier == name.Lexeme) && (k.AType is not FunType)).AType; // throws error if more than one non-function variable with same name. intended.
                }
            }
            errors.CompilerError(name, "There is no declared variable with this name");
            return ignoreType;
        }

        // ok, 
        /// <summary>
        /// ResolveFun is called directly when we call a function to figure out what function we called, and also registers it in the interpreter
        /// </summary>
        /// <param name="expr">Is the call expression, owner of "name" and necessary to pass to interpreter to register expr->function connection</param>
        /// <param name="name">Name of the expr</param>
        /// <param name="argTypes">resolved ArgTypes of the call expression</param>
        /// <returns></returns>
        private AccType ResolveFun(Expr.Call expr, Token name, List<AccType> argTypes)
        {
            for (int i=0; i < scopes.Count; i++)
            {
                List<FunType> possibleFuncs = scopes.ElementAt(i)
                    .Where(k => k.Identifier == name.Lexeme && k.AType is FunType)
                    .Select(k => (FunType)k.AType)
                    .ToList();

                if (possibleFuncs.Count == 0) continue;

                foreach (FunType possibleFunc in possibleFuncs)
                {
                    if (possibleFunc.ParamTypes.Count != argTypes.Count) continue;

                    // duplicate code in VisitCallExpr
                    bool match = true;
                    for (int j = 0; j < argTypes.Count; j++)
                    {
                        if ((!Equals(argTypes[j], possibleFunc.ParamTypes[j])) 
                            && (!Equals(possibleFunc.ParamTypes[j], ignoreType))
                            && (!Equals(argTypes[j], ignoreType)))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        interpreter.Resolve(expr.Callee, i, MangleName(name.Lexeme, possibleFunc.ParamTypes)); // todo: since environemnts now use both depth and mangled name, we need to add that to the locals dict in interpreter
                        if (i > 0) notAccessedYet.ElementAt(i).Remove(name);
                        return possibleFunc;
                    }
                }

                // we keep going if we haven't found a match, all the way up the list of scopes

                // todo: we could try again with implicit casts, but would have to be done after so we don't greedily select the incorrect function
            }



            errors.CompilerError(name, "No matching function found for the given name and arguments");
            return ignoreType;
        }

        // resolve fun is diff from resolvevar. resolvefun resolves the inside of a function. resolvevar finds the var/fun assocaited with a token
        private void ResolveFunction(Stmt.Function function)
        {
            // make sure function return type is a valid type
            bool enclosingFunction = inFunction;
            inFunction = true;
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

            inFunction = enclosingFunction;
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

        private static string MangleName(string name, List<AccType> paramTypes)
        {
            StringBuilder sb = new();
            sb.Append(name);
            foreach (AccType pType in paramTypes)
            {
                sb.Append(pType.Value);
            }

            return sb.ToString();
        }


    }
}

// todo: combination of resolver and type mostly complete. still need to integrate interpreter and typer tho for resolutions