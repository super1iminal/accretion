using accretion.Core;
using accretion.Core.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace accretion.Tests
{
    /// <summary>
    /// Main test runner that discovers, executes, and reports on test cases.
    /// </summary>
    public class TestRunner
    {
        private readonly string _testScriptsDirectory;
        private readonly bool _verbose;

        public TestRunner(string testScriptsDirectory, bool verbose = false)
        {
            _testScriptsDirectory = testScriptsDirectory;
            _verbose = verbose;
        }

        /// <summary>
        /// Runs all tests in the TestScripts directory.
        /// </summary>
        public TestSummary RunAllTests()
        {
            var summary = new TestSummary();
            var testFiles = DiscoverTestFiles();

            Console.WriteLine($"Discovered {testFiles.Count} test file(s) in {_testScriptsDirectory}");
            Console.WriteLine(new string('=', 60));

            foreach (var testFile in testFiles)
            {
                var result = RunTestFile(testFile);
                summary.Results.Add(result);
                summary.TotalTests++;

                if (result.Passed)
                {
                    summary.PassedTests++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[PASS] {result.TestName}");
                }
                else
                {
                    summary.FailedTests++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAIL] {result.TestName}");
                    foreach (var failure in result.Failures)
                    {
                        Console.WriteLine($"       - {failure}");
                    }
                }
                Console.ResetColor();

                if (_verbose)
                {
                    PrintVerboseOutput(result);
                }
            }

            PrintSummary(summary);
            return summary;
        }

        /// <summary>
        /// Runs tests filtered by tag.
        /// </summary>
        public TestSummary RunTestsByTag(string tag)
        {
            var summary = new TestSummary();
            var testFiles = DiscoverTestFiles();

            foreach (var testFile in testFiles)
            {
                var testCase = LoadTestCase(testFile);
                if (testCase == null || !testCase.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    continue;

                var result = RunTest(testCase, testFile);
                summary.Results.Add(result);
                summary.TotalTests++;

                if (result.Passed)
                    summary.PassedTests++;
                else
                    summary.FailedTests++;

                PrintTestResult(result);
            }

            PrintSummary(summary);
            return summary;
        }

        /// <summary>
        /// Runs a single test file.
        /// </summary>
        public TestResult RunTestFile(string testFilePath)
        {
            var testCase = LoadTestCase(testFilePath);
            if (testCase == null)
            {
                return new TestResult
                {
                    TestName = Path.GetFileName(testFilePath),
                    TestFile = testFilePath,
                    Passed = false,
                    Failures = { $"Failed to load test file: {testFilePath}" }
                };
            }

            return RunTest(testCase, testFilePath);
        }

        /// <summary>
        /// Executes a test case and returns the result.
        /// </summary>
        private TestResult RunTest(TestCase testCase, string testFilePath)
        {
            var result = new TestResult
            {
                TestName = testCase.Name,
                TestFile = testFilePath,
                Passed = true
            };

            // Set up test infrastructure
            var testLogger = new TestLogger();
            var testErrorManager = new TestErrorManager();

            try
            {
                // Execute the script
                ExecuteScript(testCase.Script, testErrorManager, testLogger);

                // Capture actual results
                result.ActualOutput = testLogger.FullOutput;
                result.ActualCompilerErrors = testErrorManager.CompilerErrorOutput;
                result.ActualCompilerWarnings = testErrorManager.CompilerWarningOutput;
                result.ActualRuntimeErrors = testErrorManager.RuntimeErrorOutput;
                result.HadCompilerError = testErrorManager.HasCompilerError;
                result.HadCompilerWarning = testErrorManager.HasCompilerWarning;
                result.HadRuntimeError = testErrorManager.HasRuntimeError;

                // Validate results
                ValidateCompilerErrors(testCase, result);
                ValidateCompilerWarnings(testCase, result);
                ValidateRuntimeErrors(testCase, result);
                ValidateOutput(testCase, result);
            }
            catch (Exception ex)
            {
                result.AddFailure($"Unexpected exception: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Executes the Accretion script using the provided error manager and logger.
        /// </summary>
        private void ExecuteScript(string source, TestErrorManager errors, TestLogger logger)
        {
            errors.Reset();

            // Scan
            var scanner = new Scanner(source, errors);
            var tokens = scanner.ScanTokens();

            // Parse
            var parser = new Parser(tokens, errors);
            var statements = parser.Parse();

            if (errors.HasCompilerError) return;

            // Create interpreter
            var interpreter = new Interpreter(errors, logger);

            // Resolve
            var resolver = new Resolver(interpreter, errors);
            resolver.BeginResolve(statements);

            if (errors.HasCompilerError) return;

            // Type check
            var typer = new Typer(errors);
            typer.BeginResolve(statements);

            if (errors.HasCompilerError) return;

            // Interpret
            interpreter.Interpret(statements);
        }

        private void ValidateCompilerErrors(TestCase testCase, TestResult result)
        {
            if (testCase.ExpectCompilerError)
            {
                if (!result.HadCompilerError)
                {
                    result.AddFailure("Expected compiler error but none occurred");
                }
                else if (!string.IsNullOrEmpty(testCase.ExpectedCompilerError))
                {
                    if (!MatchOutput(result.ActualCompilerErrors, testCase.ExpectedCompilerError, testCase))
                    {
                        result.AddFailure($"Compiler error mismatch.\n       Expected: {testCase.ExpectedCompilerError}\n       Actual: {result.ActualCompilerErrors}");
                    }
                }
            }
            else if (result.HadCompilerError)
            {
                result.AddFailure($"Unexpected compiler error: {result.ActualCompilerErrors}");
            }
        }

        private void ValidateCompilerWarnings(TestCase testCase, TestResult result)
        {
            if (testCase.ExpectCompilerWarning)
            {
                if (!result.HadCompilerWarning)
                {
                    result.AddFailure("Expected compiler warning but none occurred");
                }
                else if (!string.IsNullOrEmpty(testCase.ExpectedCompilerWarning))
                {
                    if (!MatchOutput(result.ActualCompilerWarnings, testCase.ExpectedCompilerWarning, testCase))
                    {
                        result.AddFailure($"Compiler warning mismatch.\n       Expected: {testCase.ExpectedCompilerWarning}\n       Actual: {result.ActualCompilerWarnings}");
                    }
                }
            }
            // Note: We don't fail if there's an unexpected warning - warnings are informational
        }

        private void ValidateRuntimeErrors(TestCase testCase, TestResult result)
        {
            if (testCase.ExpectRuntimeError)
            {
                if (!result.HadRuntimeError)
                {
                    result.AddFailure("Expected runtime error but none occurred");
                }
                else if (!string.IsNullOrEmpty(testCase.ExpectedRuntimeError))
                {
                    if (!MatchOutput(result.ActualRuntimeErrors, testCase.ExpectedRuntimeError, testCase))
                    {
                        result.AddFailure($"Runtime error mismatch.\n       Expected: {testCase.ExpectedRuntimeError}\n       Actual: {result.ActualRuntimeErrors}");
                    }
                }
            }
            else if (result.HadRuntimeError)
            {
                result.AddFailure($"Unexpected runtime error: {result.ActualRuntimeErrors}");
            }
        }

        private void ValidateOutput(TestCase testCase, TestResult result)
        {
            // Only validate output if there was no compiler error (unless we expected one)
            // or if there was a runtime error with partial output
            if (result.HadCompilerError && !testCase.ExpectCompilerError)
                return;

            if (result.HadCompilerError && testCase.ExpectCompilerError)
                return; // Don't check output when we expected a compiler error

            if (!string.IsNullOrEmpty(testCase.ExpectedOutput) || !string.IsNullOrEmpty(result.ActualOutput))
            {
                if (!MatchOutput(result.ActualOutput, testCase.ExpectedOutput, testCase))
                {
                    result.AddFailure($"Output mismatch.\n       Expected: {FormatForDisplay(testCase.ExpectedOutput)}\n       Actual: {FormatForDisplay(result.ActualOutput)}");
                }
            }
        }

        private bool MatchOutput(string actual, string expected, TestCase testCase)
        {
            var actualProcessed = testCase.TrimWhitespace ? actual.Trim() : actual;
            var expectedProcessed = testCase.TrimWhitespace ? expected.Trim() : expected;

            // Normalize line endings
            actualProcessed = actualProcessed.Replace("\r\n", "\n").Replace("\r", "\n");
            expectedProcessed = expectedProcessed.Replace("\r\n", "\n").Replace("\r", "\n");

            if (testCase.ExactMatch)
            {
                return string.Equals(actualProcessed, expectedProcessed, StringComparison.Ordinal);
            }
            else
            {
                return actualProcessed.Contains(expectedProcessed, StringComparison.Ordinal);
            }
        }

        private string FormatForDisplay(string text)
        {
            if (string.IsNullOrEmpty(text)) return "(empty)";
            return text.Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private List<string> DiscoverTestFiles()
        {
            if (!Directory.Exists(_testScriptsDirectory))
            {
                Console.WriteLine($"Test directory not found: {_testScriptsDirectory}");
                return new List<string>();
            }

            return Directory.GetFiles(_testScriptsDirectory, "*.json", SearchOption.AllDirectories)
                           .OrderBy(f => f)
                           .ToList();
        }

        private TestCase LoadTestCase(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                return JsonSerializer.Deserialize<TestCase>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading test file {filePath}: {ex.Message}");
                return null;
            }
        }

        private void PrintTestResult(TestResult result)
        {
            if (result.Passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[PASS] {result.TestName}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {result.TestName}");
                foreach (var failure in result.Failures)
                {
                    Console.WriteLine($"       - {failure}");
                }
            }
            Console.ResetColor();
        }

        private void PrintVerboseOutput(TestResult result)
        {
            Console.WriteLine($"       File: {result.TestFile}");
            if (!string.IsNullOrEmpty(result.ActualOutput))
                Console.WriteLine($"       Output: {FormatForDisplay(result.ActualOutput)}");
            if (!string.IsNullOrEmpty(result.ActualCompilerErrors))
                Console.WriteLine($"       Compiler Errors: {FormatForDisplay(result.ActualCompilerErrors)}");
            if (!string.IsNullOrEmpty(result.ActualCompilerWarnings))
                Console.WriteLine($"       Compiler Warnings: {FormatForDisplay(result.ActualCompilerWarnings)}");
            if (!string.IsNullOrEmpty(result.ActualRuntimeErrors))
                Console.WriteLine($"       Runtime Errors: {FormatForDisplay(result.ActualRuntimeErrors)}");
            Console.WriteLine();
        }

        private void PrintSummary(TestSummary summary)
        {
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Test Results: {summary.PassedTests}/{summary.TotalTests} passed ({summary.PassRate:F1}%)");

            if (summary.FailedTests > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed Tests: {summary.FailedTests}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All tests passed!");
                Console.ResetColor();
            }
        }
    }
}
