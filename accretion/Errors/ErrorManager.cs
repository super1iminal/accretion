using accretion.Domain;
using accretion.Utilities;
using System;
using System.Collections.Generic;

namespace accretion.Errors
{

    public class ErrorManager
    {
        private readonly Logger logger;
        private enum EType
        {
            COMPILER_ERROR,
            COMPILER_WARNING,
            RUNTIME_ERROR
        }

        private Dictionary<EType, string> errorNames = new()
        {
            {EType.COMPILER_WARNING, "Warning" },
            {EType.COMPILER_ERROR, "Error" },
            {EType.RUNTIME_ERROR, "Runtime error" }
        };

        public bool HasCompilerError { get; private set; }
        public bool HasCompilerWarning { get; private set; }
        public bool HasRuntimeError { get; private set; }

        public ErrorManager(Logger logger)
        {
            this.logger = logger;
        }

        public virtual void Reset()
        {
            HasCompilerError = false;
            HasCompilerWarning = false;
            HasRuntimeError = false;
        }

        public virtual void CompilerError(int line, string message, string where = "")
        {
            Report(line, where, message, EType.COMPILER_ERROR);
            HasCompilerError = true;
        }

        public virtual void CompilerWarning(int line, string message, string where = "")
        {
            Report(line, message, where, EType.COMPILER_WARNING);
            HasCompilerWarning = true;
        }

        public virtual void CompilerError(Token token, string message)
        {
            CompilerError(token.Line, message, Where(token));
        }

        public virtual void CompilerWarning(Token token, string message)
        {
            CompilerWarning(token.Line, message, Where(token));
        }


        public virtual void RuntimeError(RuntimeError error)
        {
            Report(error.Token.Line, error.Message, Where(error.Token), EType.RUNTIME_ERROR);
            HasRuntimeError = true;
        }




        private void Report(int line, string message, string where, EType level)
        {
            logger.Log($"[line {line}] {errorNames[level]}: {where}: {message}");
        }

        private string Where(Token token)
        {
            return token.Type == TokenType.EOF ? "at end" : $"at '{token.Lexeme}'";
        }
    }
}
