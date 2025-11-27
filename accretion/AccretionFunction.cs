using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public class AccretionFunction : AccretionCallable
    {
        private readonly Stmt.Function declaration;
        private readonly Environment closure;

        public int Arity { get => declaration.Parameters.Count; }

        public AccretionFunction(Stmt.Function declaration, Environment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Environment environment = new Environment(closure);

            for (int i = 0; i < arguments.Count; i++)
            {
                environment.Define(declaration.Parameters[i].Lexeme, arguments[i]);
            }

            try
            {
                interpreter.ExecuteBlock(declaration.Body, environment);
            } catch (Return returnValue)
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
