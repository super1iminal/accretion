using accretion.Core;
using accretion.Core.Resolvers;
using accretion.Errors;
using System.Collections.Generic;

namespace accretion
{
    public class Accretion
    {
        private readonly ErrorManager errors;
        private readonly Interpreter interpreter;
        public Accretion(ErrorManager errorManager, Logger logger)
        {
            interpreter = new(errorManager, logger);
            errors = errorManager;
        }

        public void Execute(string source)
        {
            errors.Reset();
            Scanner scanner = new Scanner(source, errors);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens, errors);
            List<Stmt> statements = parser.Parse();

            if (errors.HasCompilerError) return;

            Resolver resolver = new Resolver(interpreter, errors);
            resolver.BeginResolve(statements);

            if (errors.HasCompilerError) return;

            Typer typer = new Typer(errors);
            typer.BeginResolve(statements);

            if (errors.HasCompilerError) return;

            interpreter.Interpret(statements);
        }
    }
}
