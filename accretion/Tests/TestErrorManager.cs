using accretion.Domain;
using accretion.Errors;
using System.Collections.Generic;
using System.Text;

namespace accretion.Tests
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
        private readonly TestLogger _internalLogger;

        public TestErrorManager() : base(new TestLogger())
        {
            _internalLogger = new TestLogger();
        }

        public override void Reset()
        {
            base.Reset();
            _compilerErrors.Clear();
            _compilerWarnings.Clear();
            _runtimeErrors.Clear();
        }

        public override void CompilerError(int line, string message, string where = "")
        {
            string formattedError = FormatMessage(line, message, where, "Error");
            _compilerErrors.Add(formattedError);
            base.CompilerError(line, message, where);
        }

        public override void CompilerWarning(int line, string message, string where = "")
        {
            string formattedWarning = FormatMessage(line, message, where, "Warning");
            _compilerWarnings.Add(formattedWarning);
            base.CompilerWarning(line, message, where);
        }

        public override void CompilerError(Token token, string message)
        {
            string where = Where(token);
            string formattedError = FormatMessage(token.Line, message, where, "Error");
            _compilerErrors.Add(formattedError);
            base.CompilerError(token, message);
        }

        public override void CompilerWarning(Token token, string message)
        {
            string where = Where(token);
            string formattedWarning = FormatMessage(token.Line, message, where, "Warning");
            _compilerWarnings.Add(formattedWarning);
            base.CompilerWarning(token, message);
        }

        public override void RuntimeError(RuntimeError error)
        {
            string where = Where(error.Token);
            string formattedError = FormatMessage(error.Token.Line, error.Message, where, "Runtime error");
            _runtimeErrors.Add(formattedError);
            base.RuntimeError(error);
        }

        private static string FormatMessage(int line, string message, string where, string level)
        {
            return $"[line {line}] {level}: {where}: {message}";
        }

        private static string Where(Token token)
        {
            return token.Type == TokenType.EOF ? "at end" : $"at '{token.Lexeme}'";
        }

        // Properties to access captured errors/warnings

        /// <summary>
        /// Gets all captured compiler errors as a list.
        /// </summary>
        public List<string> CompilerErrors => new(_compilerErrors);

        /// <summary>
        /// Gets all captured compiler warnings as a list.
        /// </summary>
        public List<string> CompilerWarnings => new(_compilerWarnings);

        /// <summary>
        /// Gets all captured runtime errors as a list.
        /// </summary>
        public List<string> RuntimeErrors => new(_runtimeErrors);

        /// <summary>
        /// Gets all compiler errors as a single string.
        /// </summary>
        public string CompilerErrorOutput
        {
            get
            {
                if (_compilerErrors.Count == 0) return string.Empty;
                return string.Join("\n", _compilerErrors);
            }
        }

        /// <summary>
        /// Gets all compiler warnings as a single string.
        /// </summary>
        public string CompilerWarningOutput
        {
            get
            {
                if (_compilerWarnings.Count == 0) return string.Empty;
                return string.Join("\n", _compilerWarnings);
            }
        }

        /// <summary>
        /// Gets all runtime errors as a single string.
        /// </summary>
        public string RuntimeErrorOutput
        {
            get
            {
                if (_runtimeErrors.Count == 0) return string.Empty;
                return string.Join("\n", _runtimeErrors);
            }
        }

        /// <summary>
        /// Gets all errors and warnings combined.
        /// </summary>
        public string AllOutput
        {
            get
            {
                var sb = new StringBuilder();
                if (_compilerErrors.Count > 0)
                    sb.AppendLine(CompilerErrorOutput);
                if (_compilerWarnings.Count > 0)
                    sb.AppendLine(CompilerWarningOutput);
                if (_runtimeErrors.Count > 0)
                    sb.AppendLine(RuntimeErrorOutput);
                return sb.ToString().TrimEnd();
            }
        }
    }
}
