using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using System.Collections.Generic;
using System.Linq;

namespace accretion.Core.ResolverTools
{
    public class StmtVisitor : Stmt.IVisitor
    {
        private readonly ExprVisitor exprVisitor;
        private readonly Typer typer;

        public StmtVisitor(Typer typer)
        {
            this.typer = typer;
            this.exprVisitor = new(typer);
        }

        // STATEMENTS
        public void VisitBlockStmt(Stmt.Block stmt)
        {
            typer.BeginScope();
            Resolve(stmt.Statements);
            typer.EndScope();
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
                initType = exprVisitor.Resolve(stmt.Initializer);
                defined = true;
            }
            AccType varType = DeclareVar(stmt.Name, stmt.Type);
            if (defined) typer.Define(stmt.Name);


            // init stuff
            if (Equals(varType, typer.ignoreType)) return;

            initType = typer.ImplicitCast(initType, varType);

            if (initType != null && !Equals(varType, initType))
            {
                typer.errors.CompilerError(stmt.Type, "Variable initializer does not match variable type"); // todo: move this to the initializer resolution to use initializer token,
                                                                                                      // or implicitly split variable initialization into declaration and assignment to avoid this
            }

            return;
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            AccType previousFunType = typer.currentFunctionType;

            DeclareFun(stmt.Name, stmt.Returntype, stmt.Parametertypes);
            // Define(stmt.Name); // unecessary, since DeclareFun does not add fun name to undeclared vars

            ResolveFunction(stmt);

            typer.currentFunctionType = previousFunType;
            return;
        }

        // nothing statements
        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            exprVisitor.Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitIfStmt(Stmt.If stmt)
        {
            exprVisitor.Resolve(stmt.Condition);
            Resolve(stmt.Consequent);

            if (stmt.Alternative != null) Resolve(stmt.Alternative); // no control flow, we evaluate both branches
            return;
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            exprVisitor.Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitReturnStmt(Stmt.Return stmt)
        {
            if (!typer.inFunction)
            {
                typer.errors.CompilerError(stmt.Keyword, "Can't return from top-level code");
            }

            AccType returnValueType;
            if (stmt.Value != null)
            {
                returnValueType = exprVisitor.Resolve(stmt.Value);
            }
            else
            {
                returnValueType = NativeAccTypeFactory.VOID;
            }


            // return stuff
            if (Equals(typer.currentFunctionType, typer.ignoreType) || Equals(returnValueType, typer.ignoreType) || (typer.currentFunctionType is not FunType cfReturnType)) return;

            returnValueType = typer.ImplicitCast(returnValueType, cfReturnType.ReturnType);
            if (!Equals(returnValueType, ((FunType)typer.currentFunctionType).ReturnType))
            {
                typer.errors.CompilerError(stmt.Keyword, $"Returned value does not match return type ({typer.currentFunctionType.Value})");
            }

            return;
        }

        public void VisitWhileStmt(Stmt.While stmt)
        {
            bool enclosingLoop = typer.inloop;
            typer.inloop = true;

            exprVisitor.Resolve(stmt.Condition);
            Resolve(stmt.Body);

            typer.inloop = enclosingLoop;
            return;
        }

        public void VisitJumpStmt(Stmt.Jump stmt)
        {
            if (!typer.inloop) typer.errors.CompilerError(stmt.Label, "Can't jump outside of a loop.");
            return;
        }


        // HELPERS

        public void Resolve(List<Stmt> statements)
        {
            foreach (Stmt stmt in statements)
            {
                Resolve(stmt);
            }
        }

        public void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        private AccType DeclareVar(Token name, Token typeToken)
        {
            if (typer.scopes.Count == 0) throw new System.ApplicationException("Woah. You shouldn't be here. Error code 0918.");

            HashSet<Typer.Signature> scope = typer.scopes.Peek();


            AccType type = new(typeToken.Lexeme);
            AccType validatedType = ValidOrIgnore(type, typeToken);

            // check for existence (name comparison)
            if (scope.Any(k => k.Identifier == name.Lexeme))
            {
                typer.errors.CompilerError(name, "Already a variable or function declared with this name in this scope");
                return validatedType;
            }


            Typer.Signature sig = new(name, validatedType);

            typer.notAccessedYet.Peek()[name.Lexeme] = name;
            // notDefinedYet.Peek().Add(name); // add every time var is called. maybe optimizable?
            scope.Add(sig); // not defined yet
            return validatedType;
        }

        private void DeclareFun(Token name, Token returnType, List<Token> paramTypes)
        {
            if (typer.scopes.Count == 0) return;

            HashSet<Typer.Signature> scope = typer.scopes.Peek();

            FunType type = new(returnType, paramTypes);
            AccType ignoreCheck = ValidOrIgnore(type.ReturnType, returnType);
            AccType verifiedType = Equals(ignoreCheck, typer.ignoreType) ? typer.ignoreType : type;


            // if existing other var w/ same name, should fail
            // if existing other fun w/ same name but diff param types, should pass
            // else, fail


            foreach (Typer.Signature match in scope.Where(k => k.Identifier == name.Lexeme))
            {
                // todo: what to do with ignore types here?
                if (match.AType is not FunType otherType)
                {
                    typer.errors.CompilerError(name, "Already a variable declared with this name in this scope");
                    return;
                }
                else
                {
                    if (Equals(type, otherType))
                    {
                        typer.errors.CompilerError(name, "Already a function declared with the same signature in this scope");
                    }
                }
            }


            typer.currentFunctionType = verifiedType;

            Typer.Signature sig = new(name, verifiedType);

            FunType funType = verifiedType as FunType;
            string key = funType != null ? Typer.MangleName(name.Lexeme, funType.ParamTypes) : name.Lexeme;
            typer.notAccessedYet.Peek()[key] = name;
            scope.Add(sig);
            // todo: paramtypes checked in ResolveFunction
        }

        /// <summary>
        /// helper that returns either the given type, if it is valid, or sends an error and returns the ignore type if the type is invalid.
        /// </summary>
        private AccType ValidOrIgnore(AccType type, Token typeToken)
        {
            if (!typer.validTypes.Contains(type)) {
                typer.errors.CompilerError(typeToken, "Unknown type");
                return typer.ignoreType;
            }
            else return type;
        }

        // resolve fun is diff from resolvevar. resolvefun resolves the inside of a function. resolvevar finds the var/fun assocaited with a token
        private void ResolveFunction(Stmt.Function function)
        {
            // make sure function return type is a valid type
            bool enclosingFunction = typer.inFunction;
            typer.inFunction = true;
            typer.BeginScope();
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                Token param = function.Parameters[i];
                Token paramType = function.Parametertypes[i];

                DeclareVar(param, paramType);
                typer.Define(param);
            }

            Resolve(function.Body); // different from how interepreter handles function declarations
                                    // at runtime, declaring a function doesn't do anything with function body
                                    // in static analysis, we traverse body
            typer.EndScope();

            typer.inFunction = enclosingFunction;
        }
    }
}
