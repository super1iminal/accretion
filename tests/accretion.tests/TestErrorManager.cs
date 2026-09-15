using accretion.Domain;
using accretion.Errors;
using System.Collections.Generic;

namespace accretion.tests
{
    /// <summary>
    /// An ErrorManager implementation that captures error and warning output
    /// for testing purposes instead of writing to the console.
    /// </summary>
    public class TestErrorManager : ErrorManager
    {
        private readonly List<string> _compilerErrors = new();
        private readonly List<string> _compilerWarnings = new();
        private readonly List<string> _runtimeErrors = new();

        public TestErrorManager() : base(new TestLogger())
        {
        }

        public override void Reset()
        {
            base.Reset();
            _compilerErrors.Clear();
            _compilerWarnings.Clear();
            _runtimeErrors.Clear();
        }

        public override void CompilerError(int line, int index, int length, string message, string where = "")
        {
            _compilerErrors.Add(FormatMessage(line, message, where, "Error"));
            base.CompilerError(line, index, length, message, where);
        }

        public override void CompilerWarning(int line, int index, int length, string message, string where = "")
        {
            _compilerWarnings.Add(FormatMessage(line, message, where, "Warning"));
            base.CompilerWarning(line, index, length, message, where);
        }

        public override void CompilerError(Token token, string message)
        {
            _compilerErrors.Add(FormatMessage(token.Line, message, Where(token), "Error"));
            base.CompilerError(token, message);
        }

        public override void CompilerWarning(Token token, string message)
        {
            _compilerWarnings.Add(FormatMessage(token.Line, message, Where(token), "Warning"));
            base.CompilerWarning(token, message);
        }

        public override void RuntimeError(RuntimeError error)
        {
            _runtimeErrors.Add(FormatMessage(error.Token.Line, error.Message, Where(error.Token), "Runtime error"));
            base.RuntimeError(error);
        }

        private static string FormatMessage(int line, string message, string where, string level)
        {
            return $"[line {line}] {level} {where}: {message}";
        }

        private static string Where(Token token)
        {
            return token.Type == TokenType.EOF ? "at end" : $"at '{token.Lexeme}'";
        }

        public string CompilerErrorOutput => string.Join("\n", _compilerErrors);

        public string CompilerWarningOutput => string.Join("\n", _compilerWarnings);

        public string RuntimeErrorOutput => string.Join("\n", _runtimeErrors);
    }
}
