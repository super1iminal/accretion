using accretion.Core.InterpreterTools;
using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using accretion.Utilities;
using System.Collections.Generic;

namespace accretion
{
    // tree-walk interpreter
    // need a type check for each cast
    public class Interpreter  // remember, object is the return value of the visitor
    {
        private readonly ErrorManager errors;
        
        public SingleEnvironment Globals { get; } = new();
        public LayeredEnvironment Environment { get; private set; } = new();
        public Dictionary<Expr, int> Locals { get; } = new(); // used indirectly (through Resolve() below) by Resolver
                                                              // (resolves scope of variables, e.g., Expr x is 5 scopes away,
                                                              // but we know that Expr x (different) is 1 scope away)

        private readonly StmtVisitor stmtVisitor;     

        // native function

        public Interpreter(ErrorManager errors, Logger logger)
        {
            foreach (var native in NativeRegistry.All)
            {
                Globals.Define(native.Name, native.Value);
            }

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
        public void Resolve(Expr expr, int depth)
        {
            Locals[expr] = depth;
            // since we're using expr and not name, the difference between vars (if multiple of same name) is builtin to our locals dict
        }

        public void ExecuteBlock(List<Stmt> statements, LayeredEnvironment environment)
        {
            LayeredEnvironment previous = this.Environment;

            try
            {
                this.Environment = environment;

                foreach (Stmt statement in statements)
                {
                    stmtVisitor.Execute(statement);
                }
            }
            finally
            {
                this.Environment = previous;
            }
        }
    }
}
