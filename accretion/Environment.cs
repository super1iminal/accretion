using accretion.Errors;
using System.Collections.Generic;


namespace accretion
{
    public class Environment
    {

        protected readonly Dictionary<string, object> values = new();

        public Environment()
        {

        }

        public void Define(string name, object value)
        {
            // no check to see if it already exists
            values.Add(name, value);
        }
    }

    public class SingleEnvironment : Environment
    {
        public SingleEnvironment() : base()
        {

        }

        public bool TryGet(Token name, out object o)
        {
            if (values.ContainsKey(name.Lexeme))
            {
                o = values.GetValueOrDefault(name.Lexeme);
                return true;
            }
            o = null;
            return false;
        }

        public void Assign(Token name, object value)
        {
            // not allowed to create a new variable
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = value;
                return;
            }

            throw new RuntimeError(name, $"Undefined variable ${name.Lexeme}.");
        }

    }
    public class LayeredEnvironment : Environment
    {
        private readonly LayeredEnvironment enclosing; // parent env, for scoping

        public LayeredEnvironment() : base()
        {
            enclosing = null;
        }

        public LayeredEnvironment(LayeredEnvironment enclosing)
        {
            this.enclosing = enclosing;
        }

        private LayeredEnvironment Ancestor(int distance)
        {
            LayeredEnvironment environment = this;
            for (int i = 0; i < distance; i++)
            {
                environment = environment.enclosing;
            }

            return environment;
        }

        public object GetAt(int distance, string name)
        {
            return Ancestor(distance).values[name]; // don't even need to check to make sure var is here, we know it is because of our static pass
            // the reason we don't walk up ancestors is: (example)
            // we define global "a"
            // we define function showA that prints global a
            // we enter a block
            // we define local "a"
            // we call showA
            // we need this to call the global "a", but it calls the local "a" if we do dynamic walk up. we need it baked in
        }

        public void AssignAt(int distance, Token name, object value)
        {
            Ancestor(distance).values[name.Lexeme] = value; // no need to check, we know it exists
        }
    }
}
