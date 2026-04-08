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
                "abs",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => Math.Abs((double)args[0])
            ),
            
            new NativeFunction(
                "abs",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT },
                args => (int)Math.Abs((int)args[0])
            ),

            new NativeFunction(
                "sin",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Sin((double)args[0])
            ),

            new NativeFunction(
                "sin",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.INT},
                args => (double)Math.Sin((int)args[0])
            ),

            new NativeFunction(
                "cos",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Cos((double)args[0])
            ),

            new NativeFunction(
                "cos",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.INT},
                args => (double)Math.Cos((int)args[0])
            ),

            // tan
            new NativeFunction(
                "tan",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Tan((double)args[0])
            ),

            new NativeFunction(
                "tan",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.INT},
                args => (double)Math.Tan((int)args[0])
            ),

            // inverse trig
            new NativeFunction(
                "asin",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Asin((double)args[0])
            ),

            new NativeFunction(
                "acos",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Acos((double)args[0])
            ),

            new NativeFunction(
                "atan",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Atan((double)args[0])
            ),

            new NativeFunction(
                "atan2",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE, NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Atan2((double)args[0], (double)args[1])
            ),

            // exponential / logarithmic
            new NativeFunction(
                "sqrt",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Sqrt((double)args[0])
            ),

            new NativeFunction(
                "sqrt",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.INT},
                args => (double)Math.Sqrt((int)args[0])
            ),

            new NativeFunction(
                "pow",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE, NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Pow((double)args[0], (double)args[1])
            ),

            new NativeFunction(
                "pow",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.INT, NativeAccTypeFactory.INT},
                args => (double)Math.Pow((int)args[0], (int)args[1])
            ),

            new NativeFunction(
                "exp",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Exp((double)args[0])
            ),

            new NativeFunction(
                "log",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Log((double)args[0])
            ),

            new NativeFunction(
                "log10",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Log10((double)args[0])
            ),

            new NativeFunction(
                "log2",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Log2((double)args[0])
            ),

            // rounding
            new NativeFunction(
                "floor",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (int)Math.Floor((double)args[0])
            ),

            new NativeFunction(
                "ceil",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (int)Math.Ceiling((double)args[0])
            ),

            new NativeFunction(
                "round",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (int)Math.Round((double)args[0])
            ),

            // min / max
            new NativeFunction(
                "min",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE, NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Min((double)args[0], (double)args[1])
            ),

            new NativeFunction(
                "min",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT, NativeAccTypeFactory.INT},
                args => (int)Math.Min((int)args[0], (int)args[1])
            ),

            new NativeFunction(
                "max",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE, NativeAccTypeFactory.DOUBLE},
                args => (double)Math.Max((double)args[0], (double)args[1])
            ),

            new NativeFunction(
                "max",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT, NativeAccTypeFactory.INT},
                args => (int)Math.Max((int)args[0], (int)args[1])
            ),

            // sign / modulo
            new NativeFunction(
                "sign",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE},
                args => (int)Math.Sign((double)args[0])
            ),

            new NativeFunction(
                "sign",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT},
                args => (int)Math.Sign((int)args[0])
            ),

            new NativeFunction(
                "mod",
                NativeAccTypeFactory.DOUBLE,
                new List<AccType>() {NativeAccTypeFactory.DOUBLE, NativeAccTypeFactory.DOUBLE},
                args => (double)args[0] % (double)args[1]
            ),

            new NativeFunction(
                "mod",
                NativeAccTypeFactory.INT,
                new List<AccType>() {NativeAccTypeFactory.INT, NativeAccTypeFactory.INT},
                args => (int)args[0] % (int)args[1]
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

            new NativeValue(
                "TAU",
                Math.Tau,
                NativeAccTypeFactory.DOUBLE
            ),

            new NativeValue(
                "INF",
                double.PositiveInfinity,
                NativeAccTypeFactory.DOUBLE
            ),
        };
    }
}
