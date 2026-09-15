using accretion.Domain;
using accretion.Exceptions;
using System.Collections.Generic;

namespace accretion.Callables
{
    public class AccretionFunction : AccretionCallable
    {
        private readonly Stmt.Function declaration;
        private readonly LayeredEnvironment closure;

        public int Arity { get => declaration.Parameters.Count; }

        public AccretionFunction(Stmt.Function declaration, LayeredEnvironment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            // environment for local variables
            LayeredEnvironment environment = new LayeredEnvironment(closure);

            for (int i = 0; i < arguments.Count; i++)
            {
                // params are local vars
                environment.Define(declaration.Parameters[i].Lexeme, arguments[i]);
            }

            try
            {
                interpreter.ExecuteBlock(declaration.Body, environment);
            }
            catch (Return returnValue) // return values are required, not implied (checked statically), and trigger an exception to come back here
            {
                return returnValue.Value;
            }
            return null;
        }

        public override string ToString()
        {
            return $"<fn {declaration.Name.Lexeme}>";
        }
    }
}
