using accretion.Domain;
using System;


namespace accretion.Errors
{
    public class RuntimeError : Exception
    {
        public readonly Token Token;

        public RuntimeError(Token token, string message) : base(message)
        {
            Token = token;
        }
    }
}
