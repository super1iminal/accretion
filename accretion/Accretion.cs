using System;
using System.Collections.Generic;
using System.IO;

namespace accretion
{
    // TODO: add ternary operator ?:
    public class Accretion
    {
        private static readonly Interpreter interpreter = new();
        static bool hadError = false;
        static bool hadRuntimeError = false;

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: acc [script file path]");
                Environment.Exit(64);
            } else if (args.Length == 1)
            {
                RunFile(args[0]);
            } else
            {
                RunPrePrompt();
            }
        }

        private static void RunPrePrompt()
        {
            Console.WriteLine("1 for file and 2 for REPL");
            string line = Console.ReadLine();
            if (line != null)
            {
                if (line == "1")
                {
                    Console.WriteLine("Path:");
                    string path = Console.ReadLine();
                    if (path != null)
                    {
                        RunFile(path);
                    }
                }
                else if (line == "2")
                {
                    RunPrompt();
                }
            }

            Console.WriteLine("Quitting...");
        }

        private static void RunFile(string path)
        {
            string fileText = File.ReadAllText(path);
            Run(fileText);

            if (hadError) Environment.Exit(65);

            if (hadRuntimeError) Environment.Exit(70);
        }

        private static void RunPrompt()
        {
            for (; ;)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == null) break;
                Run(line);
                hadError = false;
            }
        }

        private static void Run(string source)
        {
            Scanner scanner = new(source);
            List<Token> tokens = scanner.ScanTokens();
            
            Parser parser = new(tokens);
            List<Stmt> statements = parser.Parse();

            if (hadError) return;

            interpreter.Interpret(statements);

            //Console.WriteLine(new ASTPrinter().Print(expression));


            //foreach (Token token in tokens)
            //{
            //    Console.WriteLine(token);
            //}
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        public static void Error(Token token, string message)
        {
            if (token.Type == TokenType.EOF)
            {
                Report(token.Line, "at end", message);
            }
            else
            {
                Report(token.Line, $" at '{token.Lexeme}'", message);
            }
        }

        public static void AccretionRuntimeError(RuntimeError error)
        {
            Console.WriteLine($"{error.Message}\n[line {error.Token.Line}]");

            hadRuntimeError = true;
        }

        static void Report(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Error {where}: {message}");
            hadError = true;
        }
    }
}
