
using System;

namespace accretion
{
    public class Logger
    {
        public virtual void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
