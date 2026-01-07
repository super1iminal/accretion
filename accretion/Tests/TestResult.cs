using System.Collections.Generic;

namespace accretion.Tests
{
    /// <summary>
    /// Represents the result of executing a single test case.
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public string TestFile { get; set; } = "";
        public bool Passed { get; set; }
        public List<string> Failures { get; set; } = new();

        // Actual outputs captured during execution
        public string ActualOutput { get; set; } = "";
        public string ActualCompilerErrors { get; set; } = "";
        public string ActualCompilerWarnings { get; set; } = "";
        public string ActualRuntimeErrors { get; set; } = "";

        // Flags indicating what actually happened
        public bool HadCompilerError { get; set; }
        public bool HadCompilerWarning { get; set; }
        public bool HadRuntimeError { get; set; }

        public void AddFailure(string message)
        {
            Failures.Add(message);
            Passed = false;
        }
    }

    /// <summary>
    /// Represents a summary of all test results.
    /// </summary>
    public class TestSummary
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public List<TestResult> Results { get; set; } = new();
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    }
}
