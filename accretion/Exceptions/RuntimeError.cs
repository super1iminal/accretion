using System;


namespace accretion.Exceptions
{
    public class RuntimeError : Exception
    {
        public readonly Token Token;

        public RuntimeError(Token token, string message) : base(message)
        {
            this.Token = token;
        }
    }
}
