using accretion.Utilities;
using System.Collections.Generic;
using System.Text;

namespace accretion.tests
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

        public List<string> OutputLines => new(_outputLines);

        public string FullOutput => _fullOutput.ToString().TrimEnd();
    }
}
