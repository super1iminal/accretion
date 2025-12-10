using accretion.Exceptions;
using accretion.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Callables
{
    public class NativeCallable : AccretionCallable
    {
        public int Arity { get { return ParameterTypes.Count; } }
        public AccType ReturnType { get; }
        public List<AccType> ParameterTypes { get; }

        private Func<List<object>, object> func;

        public NativeCallable()
        {
            this.func = func;
            ReturnType = returnType;
            ParameterTypes = paramTypes;
        }

        private object CallFn(List<object> arguments)
        {
            return func(arguments);
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            return CallFn(arguments);
        }

        override public string ToString()
        {
            return "<native fn>";
        }

    }

    public static class NativeFunctions
    {
        public static object Clock(List<object> args)
        {
            return (double)DateTime.Now.Second;
        }

        public static object AbsD(List<object> args)
        {
            // assume typing has resolved our list of args to a single double/int number
            object num = args[0];

            if (num is double dnum)
            {
                return (double)MathF.Abs((float)dnum);
            }
            return null;
        }

        public static object SinD(List<object> args)
        {
            // assume typing has resolved our list of args
            object num = args[0];

            if (num is double dnum)
            {
                return (double)MathF.Sin((float)dnum);
            }

            if (num is int inum)
            {
                return (double)MathF.Sin(inum);
            }

            return null;
        }

        public static object Cos(List<object> args)
        {
            object num = args[0];

            if (num is double dnum)
            {
                return (double)MathF.Cos((float)dnum);
            }

            if (num is int inum)
            {
                return (double)MathF.Cos(inum);
            }

            return null;
        }

        public static object Sqrt(List<object> args)
        {
            object num = args[0];

            if (num is double dnum)
            {
                return (double)MathF.Sqrt((float)dnum);
            }

            if (num is int inum)
            {
                return (double)MathF.Sqrt((float)inum);
            }

            return null;
        }


        public static object Square(List<object> args)
        {
            object num = args[0];

            if (num is double dnum)
            {
                return (double)MathF.Pow((float)dnum, 2);
            }

            if (num is int inum)
            {
                return (double)MathF.Pow(inum, 2);
            }

            return null;
        }

        public static object Pow(List<object> args)
        {
            object num = args[0];
            object pow = args[0];

            if (num is double dnum && pow is double dpow)
            {
                return (double)MathF.Pow((float)dnum, (float)dpow);
            }
            else if (num is double dnum2 && pow is int ipow)
            {
                return (double)MathF.Pow((float)dnum2, ipow);
            }
            else if (num is int inum && pow is double dpow2)
            {
                return (double)MathF.Pow(inum, (float)dpow2);
            }
            else if (num is int inum2 && pow is int ipow2)
            {
                return (double)MathF.Pow(inum2, ipow2);
            }

            return null;
        }
    }


    //public class ABSCallable : AccretionCallable
    //{
    //    public int Arity { get { return 1; } }

    //    public object Call(Interpreter interpreter, List<object> arguments)
    //    {
    //        if (arguments.Count != 1)
    //        {
    //            throw new RuntimeError("")
    //        }
    //    }

}
