using System.Collections.Generic;

namespace accretion.Callables
{
    public interface AccretionCallable
    {
        public int Arity { get; }
        object Call(Interpreter interpreter, List<object> args);
    }
}
