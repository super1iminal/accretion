using accretion.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace accretion.tests
{
    /// <summary>
    /// Shared logic for running a .acc script (paired with a same-named
    /// .json file of expectations) through the full scan -> parse ->
    /// type-check -> interpret pipeline and validating the expected output,
    /// warnings, and errors. Used by the per-category test classes so that
    /// Test Explorer / CI reports group results by TestScripts subdirectory.
    /// </summary>
    public static class ScriptTestRunner
    {
        public static readonly string TestScriptsDirectory =
            Path.Combine(AppContext.BaseDirectory, "TestScripts");

        public static IEnumerable<object[]> DiscoverCases(string subdirectory)
        {
            var dir = Path.Combine(TestScriptsDirectory, subdirectory);
            foreach (var file in Directory.GetFiles(dir, "*.acc", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                var relativePath = Path.GetRelativePath(TestScriptsDirectory, file).Replace('\\', '/');
                yield return new object[] { relativePath };
            }
        }

        public static void Run(string relativeTestFilePath)
        {
            var testCase = LoadTestCase(Path.Combine(TestScriptsDirectory, relativeTestFilePath));

            var logger = new TestLogger();
            var errors = new TestErrorManager();
            errors.Reset();

            ExecuteScript(testCase.Script, errors, logger);

            if (testCase.ExpectCompilerError)
            {
                Assert.True(errors.HasCompilerError, "Expected compiler error but none occurred");
                if (!string.IsNullOrEmpty(testCase.ExpectedCompilerError))
                {
                    AssertOutputMatches(errors.CompilerErrorOutput, testCase.ExpectedCompilerError, testCase);
                }
                return;
            }

            Assert.False(errors.HasCompilerError, $"Unexpected compiler error: {errors.CompilerErrorOutput}");

            if (testCase.ExpectCompilerWarning)
            {
                Assert.True(errors.HasCompilerWarning, "Expected compiler warning but none occurred");
                if (!string.IsNullOrEmpty(testCase.ExpectedCompilerWarning))
                {
                    AssertOutputMatches(errors.CompilerWarningOutput, testCase.ExpectedCompilerWarning, testCase);
                }
            }

            if (testCase.ExpectRuntimeError)
            {
                Assert.True(errors.HasRuntimeError, "Expected runtime error but none occurred");
                if (!string.IsNullOrEmpty(testCase.ExpectedRuntimeError))
                {
                    AssertOutputMatches(errors.RuntimeErrorOutput, testCase.ExpectedRuntimeError, testCase);
                }
            }
            else
            {
                Assert.False(errors.HasRuntimeError, $"Unexpected runtime error: {errors.RuntimeErrorOutput}");
            }

            var actualOutput = logger.FullOutput;
            if (!string.IsNullOrEmpty(testCase.ExpectedOutput) || !string.IsNullOrEmpty(actualOutput))
            {
                AssertOutputMatches(actualOutput, testCase.ExpectedOutput, testCase);
            }
        }

        private static void ExecuteScript(string source, TestErrorManager errors, TestLogger logger)
        {
            var scanner = new Scanner(source, errors);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens, errors);
            var statements = parser.Parse();

            if (errors.HasCompilerError) return;

            var interpreter = new Interpreter(errors, logger);

            var typer = new Typer(interpreter, errors);
            typer.BeginResolve(statements);

            if (errors.HasCompilerError) return;

            interpreter.Interpret(statements);
        }

        private static TestCase LoadTestCase(string accFilePath)
        {
            var jsonFilePath = Path.ChangeExtension(accFilePath, ".json");
            var json = File.ReadAllText(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var testCase = JsonSerializer.Deserialize<TestCase>(json, options);
            testCase.Script = File.ReadAllText(accFilePath);
            return testCase;
        }

        private static void AssertOutputMatches(string actual, string expected, TestCase testCase)
        {
            var actualProcessed = Normalize(actual, testCase.TrimWhitespace);
            var expectedProcessed = Normalize(expected, testCase.TrimWhitespace);

            if (testCase.ExactMatch)
            {
                Assert.Equal(expectedProcessed, actualProcessed);
            }
            else
            {
                Assert.Contains(expectedProcessed, actualProcessed, StringComparison.Ordinal);
            }
        }

        private static string Normalize(string text, bool trim)
        {
            var processed = trim ? text.Trim() : text;
            return processed.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
