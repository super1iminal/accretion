using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public class Environment
    {
        // TODO: add initialization tracking and throw a runtime error in interpreter if accessing a non-initialized variable
        private readonly Dictionary<string, object> values = new();
        private readonly Environment enclosing; // parent env, for scoping


        public Environment()
        {
            enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        public object Get(Token name)
        {
            if (values.ContainsKey(name.Lexeme))
            {
                return values.GetValueOrDefault(name.Lexeme);
            }

            // recursive check, walk up scope
            if (enclosing != null) return enclosing.Get(name);

            throw new RuntimeError(name, $"Undefined variable {name.Lexeme}.");
        }

        public object GetAt(int distance, string name)
        {
            return Ancestor(distance).values[name]; // don't even need to check to make sure var is here, we know it is because of our static pass
        }

        private Environment Ancestor(int distance)
        {
            Environment environment = this;
            for (int i = 0; i < distance; i++)
            {
                environment = environment.enclosing;
            }

            return environment; 
        }

        public void Define(string name, object value)
        {
            // no check to see if it already exists
            values.Add(name, value);
        }

        public void Assign(Token name, object value)
        {
            // not allowed to create a new variable
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = value;
                return;
            }

            if (enclosing != null)
            {
                enclosing.Assign(name, value);
                return;
            }

            throw new RuntimeError(name, $"Undefined variable ${name.Lexeme}.");
        }

        public void AssignAt(int distance, Token name, object value)
        {
            Ancestor(distance).values[name.Lexeme] = value;
        }
    }
}
