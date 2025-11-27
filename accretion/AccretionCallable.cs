using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public interface AccretionCallable
    {
        public int Arity { get; }
        object Call(Interpreter interpreter, List<object> args);
    }
}
