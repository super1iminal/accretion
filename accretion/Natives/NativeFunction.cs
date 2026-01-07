using accretion.Callables;
using accretion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Natives
{
    public class NativeFunction : AccretionCallable, Native
    {
        public int Arity { get; }
        public string Name { get; }

        public AccType Type { get; }

        public object Value { get { return this; } } // needs to return itself since in the global env in the interpreter we set the
                                                     // value of the native to be native.Value
                                                     // the reason we *would* set the value of the native to be the native itself is because NativeFunction extends AccCallable
                                                     // however, the reason we *don't* is because NativeValue stores a literal value (double, int, bool, etc).

        private Func<List<object>, object> implementation;

        public NativeFunction(string name, AccType returnType, List<AccType> paramTypes, Func<List<object>, object> implementation)
        {
            Name = name;
            this.implementation = implementation;
            Type = new FunType(returnType, paramTypes);
            Arity = paramTypes.Count;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            return implementation(arguments);
        }

        override public string ToString()
        {
            return "<native fn>";
        }

    }

}
