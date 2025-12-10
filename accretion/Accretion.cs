using accretion.Exceptions;
using accretion.Resolvers;
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
        static bool hadWarning = false;
        static bool hadRuntimeError = false;

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: acc [script file path]");
                System.Environment.Exit(64);
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
                        RunFile("C:\\Users\\asher\\Documents\\Coding\\accretion\\scripts\\" + path);
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

            if (hadError) System.Environment.Exit(65);

            if (hadRuntimeError) System.Environment.Exit(70);
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

            Resolver resolver = new Resolver(interpreter);
            resolver.BeginResolve(statements);

            if (hadError) return; // need both this and previous check because shouldn't resolve if there are syntax errors

            Typer typer = new Typer();
            typer.BeginResolve(statements);

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
            ReportError(line, "", message);
        }

        public static void Error(Token token, string message)
        {
            if (token.Type == TokenType.EOF)
            {
                ReportError(token.Line, "at end", message);
            }
            else
            {
                ReportError(token.Line, $" at '{token.Lexeme}'", message);
            }
        }

        public static void Warning(int line, string message)
        {
            ReportWarning(line, "", message);
        }

        public static void Warning(Token token, string message)
        {
            if (token.Type == TokenType.EOF)
            {
                ReportWarning(token.Line, "at end", message);
            }
            else
            {
                ReportWarning(token.Line, $" at '{token.Lexeme}'", message);
            }
        }

        public static void AccretionRuntimeError(RuntimeError error)
        {
            Console.WriteLine($"{error.Message}\n[line {error.Token.Line}]");

            hadRuntimeError = true;
        }

        static void ReportError(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Error {where}: {message}");
            hadError = true;
        }

        static void ReportWarning(int line, string where, string message)
        {
            Console.WriteLine($"[line {line}] Warning {where}: {message}");
            hadWarning = true;
        }
    }
}
