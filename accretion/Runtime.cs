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

            Console.WriteLine("Script name (e.g., abstest.acc):");
            string path = Console.ReadLine();
            if (path != null)
            {
                RunFile(FindScriptsDir() + "/" + path);
            }


            Console.WriteLine("Quitting...");
        }

        private static string FindScriptsDir()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "scripts");
                if (Directory.Exists(candidate)) return candidate;
            }
            throw new DirectoryNotFoundException("No 'scripts' folder found above " + AppContext.BaseDirectory);
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
