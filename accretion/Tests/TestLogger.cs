using System.Collections.Generic;
using System.Text;

namespace accretion.Tests
{
    /// <summary>
    /// A Logger implementation that captures output for testing purposes
    /// instead of writing to the console.
    /// </summary>
    public class TestLogger : Logger
    {
        private readonly List<string> _outputLines = new();
        private readonly StringBuilder _fullOutput = new();

        public override void Log(string message)
        {
            _outputLines.Add(message);
            _fullOutput.AppendLine(message);
        }

        /// <summary>
        /// Gets all logged output as a list of individual lines.
        /// </summary>
        public List<string> OutputLines => new(_outputLines);

        /// <summary>
        /// Gets all logged output as a single string with newlines.
        /// </summary>
        public string FullOutput => _fullOutput.ToString().TrimEnd();

        /// <summary>
        /// Clears all captured output.
        /// </summary>
        public void Clear()
        {
            _outputLines.Clear();
            _fullOutput.Clear();
        }

        /// <summary>
        /// Returns whether any output was captured.
        /// </summary>
        public bool HasOutput => _outputLines.Count > 0;

        /// <summary>
        /// Gets the number of output lines captured.
        /// </summary>
        public int LineCount => _outputLines.Count;
    }
}
