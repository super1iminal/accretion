using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Exceptions
{
    public class Jump : RuntimeError
    {
        public Jump(Token label, string message) : base(label, message)
        {

        }
    }
}
