using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    public class Return : RuntimeError
    {
        public readonly object Value;

        public Return(object value, Token label, string message) : base(label, message)
        {
            this.Value = value;
        }
    }
}
