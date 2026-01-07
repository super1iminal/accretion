using accretion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Natives
{
    public interface Native
    {
        public string Name { get; }
        public AccType Type { get; }
        public object Value { get; }

        public string ToString();
    }
}
