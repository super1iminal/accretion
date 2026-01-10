using accretion.Errors;
using accretion.Utilities;
using System;
using System.IO;


namespace accretion
{
    public static class Runtime
    {
        static Accretion env;
        static ErrorManager errors;
        static void Main(string[] args)
        {
            Logger errorLogger = new Logger();
            errors = new ErrorManager(errorLogger);
            Logger outputLogger = new Logger();
            env = new Accretion(errors, outputLogger);

            if (args.Length > 1)
            {
                Console.WriteLine("Usage: acc [script file name]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrePrompt();
            }
        }

        private static void RunPrePrompt()
        {

            Console.WriteLine("Path:");
            string path = Console.ReadLine();
            if (path != null)
            {
                RunFile("C:\\Users\\asher\\Documents\\Coding\\accretion\\scripts\\" + path);
            }


            Console.WriteLine("Quitting...");
        }

        private static void RunFile(string path)
        {
            string fileText = File.ReadAllText(path);
            env.Execute(fileText);

            if (errors.HasCompilerError) System.Environment.Exit(65);

            if (errors.HasRuntimeError) System.Environment.Exit(70);
        }
    }
}
