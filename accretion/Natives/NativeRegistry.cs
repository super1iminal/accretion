using accretion.Core;
using accretion.Core.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Natives
{
    public static class NativeRegistry
    {
        public static readonly List<Native> All = new()
        {
            // FUNCTIONS

            new NativeFunction(
                "clock",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>(),
                args => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0
            ),

            new NativeFunction(
                "absd",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => Math.Abs((double)args[0])
            ),
            
            new NativeFunction(
                "absi",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT },
                args => (double)Math.Abs((int)args[0])
            ),




            

            // CONSTANTS

            new NativeValue(
                "PI",
                Math.PI,
                NativeAccTypeFactory.DOUBLE
            ),

            new NativeValue(
                "E",
                Math.E,
                NativeAccTypeFactory.DOUBLE
            ),
        };
    }
}
