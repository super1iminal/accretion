using System;
using System.IO;

namespace accretion.Tests
{
    /// <summary>
    /// Entry point for running Accretion tests.
    /// </summary>
    public static class TestProgram
    {
        public static int Main(string[] args)
        {
            string testDirectory = GetTestDirectory(args);
            bool verbose = Array.Exists(args, arg => arg == "-v" || arg == "--verbose");
            string tagFilter = GetTagFilter(args);

            Console.WriteLine("Accretion Test Runner");
            Console.WriteLine($"Test Directory: {testDirectory}");
            Console.WriteLine();

            var runner = new TestRunner(testDirectory, verbose);
            TestSummary summary;

            if (!string.IsNullOrEmpty(tagFilter))
            {
                Console.WriteLine($"Running tests with tag: {tagFilter}");
                summary = runner.RunTestsByTag(tagFilter);
            }
            else
            {
                summary = runner.RunAllTests();
            }

            // Return non-zero exit code if any tests failed
            return summary.FailedTests > 0 ? 1 : 0;
        }

        private static string GetTestDirectory(string[] args)
        {
            // Look for -d or --directory argument
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-d" || args[i] == "--directory")
                {
                    return args[i + 1];
                }
            }

            // Default: look for Tests/TestScripts relative to current directory
            var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "Tests", "TestScripts");
            if (Directory.Exists(defaultPath))
                return defaultPath;

            // Alternative: look relative to the project root
            var projectRoot = FindProjectRoot();
            if (projectRoot != null)
            {
                return Path.Combine(projectRoot, "Tests", "TestScripts");
            }

            return defaultPath;
        }

        private static string GetTagFilter(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-t" || args[i] == "--tag")
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string FindProjectRoot()
        {
            var current = Directory.GetCurrentDirectory();
            while (current != null)
            {
                // Look for a .csproj file or .sln file
                if (Directory.GetFiles(current, "*.csproj").Length > 0 ||
                    Directory.GetFiles(current, "*.sln").Length > 0)
                {
                    return current;
                }
                current = Directory.GetParent(current)?.FullName;
            }
            return null;
        }

        /// <summary>
        /// Prints usage information.
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine("Usage: accretion-tests [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -d, --directory <path>  Specify the test scripts directory");
            Console.WriteLine("  -t, --tag <tag>         Run only tests with the specified tag");
            Console.WriteLine("  -v, --verbose           Show detailed output for each test");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  accretion-tests                           Run all tests");
            Console.WriteLine("  accretion-tests -d ./my-tests             Run tests from custom directory");
            Console.WriteLine("  accretion-tests -t parser                 Run only parser tests");
            Console.WriteLine("  accretion-tests -v                        Run tests with verbose output");
        }
    }
}
