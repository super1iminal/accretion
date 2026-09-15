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
    /// Runs every JSON-defined script under TestScripts/ through the full
    /// scan -> parse -> type-check -> interpret pipeline and validates the
    /// expected output, warnings, and errors declared in the file.
    /// </summary>
    public class ScriptTests
    {
        private static readonly string TestScriptsDirectory =
            Path.Combine(AppContext.BaseDirectory, "TestScripts");

        public static IEnumerable<object[]> TestCases()
        {
            foreach (var file in Directory.GetFiles(TestScriptsDirectory, "*.json", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                var relativePath = Path.GetRelativePath(TestScriptsDirectory, file).Replace('\\', '/');
                yield return new object[] { relativePath };
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunScript(string relativeTestFilePath)
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
                    Assert.True(
                        MatchOutput(errors.CompilerErrorOutput, testCase.ExpectedCompilerError, testCase),
                        $"Compiler error mismatch.\nExpected: {testCase.ExpectedCompilerError}\nActual: {errors.CompilerErrorOutput}");
                }
                return;
            }

            Assert.False(errors.HasCompilerError, $"Unexpected compiler error: {errors.CompilerErrorOutput}");

            if (testCase.ExpectCompilerWarning)
            {
                Assert.True(errors.HasCompilerWarning, "Expected compiler warning but none occurred");
                if (!string.IsNullOrEmpty(testCase.ExpectedCompilerWarning))
                {
                    Assert.True(
                        MatchOutput(errors.CompilerWarningOutput, testCase.ExpectedCompilerWarning, testCase),
                        $"Compiler warning mismatch.\nExpected: {testCase.ExpectedCompilerWarning}\nActual: {errors.CompilerWarningOutput}");
                }
            }

            if (testCase.ExpectRuntimeError)
            {
                Assert.True(errors.HasRuntimeError, "Expected runtime error but none occurred");
                if (!string.IsNullOrEmpty(testCase.ExpectedRuntimeError))
                {
                    Assert.True(
                        MatchOutput(errors.RuntimeErrorOutput, testCase.ExpectedRuntimeError, testCase),
                        $"Runtime error mismatch.\nExpected: {testCase.ExpectedRuntimeError}\nActual: {errors.RuntimeErrorOutput}");
                }
            }
            else
            {
                Assert.False(errors.HasRuntimeError, $"Unexpected runtime error: {errors.RuntimeErrorOutput}");
            }

            var actualOutput = logger.FullOutput;
            if (!string.IsNullOrEmpty(testCase.ExpectedOutput) || !string.IsNullOrEmpty(actualOutput))
            {
                Assert.True(
                    MatchOutput(actualOutput, testCase.ExpectedOutput, testCase),
                    $"Output mismatch.\nExpected: {FormatForDisplay(testCase.ExpectedOutput)}\nActual: {FormatForDisplay(actualOutput)}");
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

        private static TestCase LoadTestCase(string filePath)
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

        private static bool MatchOutput(string actual, string expected, TestCase testCase)
        {
            var actualProcessed = testCase.TrimWhitespace ? actual.Trim() : actual;
            var expectedProcessed = testCase.TrimWhitespace ? expected.Trim() : expected;

            actualProcessed = actualProcessed.Replace("\r\n", "\n").Replace("\r", "\n");
            expectedProcessed = expectedProcessed.Replace("\r\n", "\n").Replace("\r", "\n");

            return testCase.ExactMatch
                ? string.Equals(actualProcessed, expectedProcessed, StringComparison.Ordinal)
                : actualProcessed.Contains(expectedProcessed, StringComparison.Ordinal);
        }

        private static string FormatForDisplay(string text)
        {
            return string.IsNullOrEmpty(text) ? "(empty)" : text.Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
