using System;
using System.Collections.Generic;
using System.IO;

namespace accretion
{
    public class Accretion
    {
        static bool hadError = false;
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: acc [script file path]");
                Environment.Exit(64);
            }

            RunFile(args[0]);

            if (hadError)
            {
                Environment.Exit(65);
            }
        }

        private static void RunFile(string path)
        {
            string fileText = File.ReadAllText(path);
            Run(fileText);
        }

        private static void Run(string source)
        {
            Scanner scanner = new(source);
            List<Token> tokens = scanner.ScanTokens();

            foreach (Token token in tokens)
            {
                Console.WriteLine(token);
            }
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        static void Report(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Error {where}: {message}");
            hadError = true;
        }
    }
}
