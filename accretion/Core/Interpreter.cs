using accretion.Core.InterpreterTools;
using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using accretion.Utilities;
using System.Collections.Generic;
using static accretion.Domain.Environment;

namespace accretion
{
    // tree-walk interpreter
    // need a type check for each cast
    public class Interpreter  // remember, object is the return value of the visitor
    {
        private readonly ErrorManager errors;
        
        public LayeredEnvironment Env { get; private set; } = new(); 

        public Dictionary<Expr, VarLocation> ResolutionMap { get; } = new(); // used indirectly (through Resolve() below) by Resolver
                                                              // (resolves scope of variables, e.g., Expr x is 5 scopes away,
                                                              // but we know that Expr x (different) is 1 scope away)

        private readonly StmtVisitor stmtVisitor;     

        // native function

        public Interpreter(ErrorManager errors, Logger logger)
        {
            foreach (var native in NativeRegistry.All)
            {
                Env.Define(native.Name, native.Value);
            }

            Env = new(Env); // this is the same setup as we do in the typer, where we have a "global" base scope and then begin resolving with a new layered scope, except here we do it in constructor.

            stmtVisitor = new(this, logger, errors);

            this.errors = errors;
        }
        // PUBLIC API
        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    stmtVisitor.Execute(statement);
                } 
            }
            catch (RuntimeError e)
            {
                errors.RuntimeError(e);
            }
        }


        // ======== HELPERS ======== 
        // recursively evaluates an expression and returns the result
        public void Resolve(Expr expr, int depth, string name)
        {
            ResolutionMap[expr] = new(depth, name);
            // since we're using expr and not name, the difference between vars (if multiple of same name) is builtin to our locals dict
        }

        public void ExecuteBlock(List<Stmt> statements, LayeredEnvironment environment)
        {
            LayeredEnvironment previous = this.Env;

            try
            {
                this.Env = environment;

                foreach (Stmt statement in statements)
                {
                    stmtVisitor.Execute(statement);
                }
            }
            finally
            {
                this.Env = previous;
            }
        }
    }
}
