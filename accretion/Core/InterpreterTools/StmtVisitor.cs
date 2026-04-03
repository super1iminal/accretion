using accretion.Callables;
using accretion.Domain;
using accretion.Errors;
using accretion.Exceptions;
using accretion.Utilities;

namespace accretion.Core.InterpreterTools
{
    public class StmtVisitor : Stmt.IVisitor
    {
        private readonly ExprVisitor exprVisitor;

        private readonly Interpreter interpreter;
        private readonly Logger logger;
        private readonly ErrorManager errors;

        public StmtVisitor(Interpreter interpreter, Logger logger, ErrorManager errors)
        {
            this.interpreter = interpreter;
            this.logger = logger;
            this.errors = errors;

            this.exprVisitor = new(interpreter, logger, errors);
        }

        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            exprVisitor.Evaluate(stmt.ExpressionValue); // resolve the expression using the visitor
                                            // (recall: pass myself to the expression, expression accepts me and
                                            // passes itself to one of my functions with itself and runs it, returns output of my function

            return;
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            object value = exprVisitor.Evaluate(stmt.ExpressionValue);
            logger.Log(Utility.Stringify(value));
            return;
        }

        // variable declaration
        public void VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.Initializer != null)
            {
                value = exprVisitor.Evaluate(stmt.Initializer);
            }

            interpreter.Env.Define(stmt.Name.Lexeme, value);
            return;
        }

        public void VisitBlockStmt(Stmt.Block stmt)
        {
            interpreter.ExecuteBlock(stmt.Statements, new LayeredEnvironment(interpreter.Env));
        }

        public void VisitIfStmt(Stmt.If stmt)
        {
            if (Utility.IsTruthy(exprVisitor.Evaluate(stmt.Condition)))
            {
                Execute(stmt.Consequent);
            }
            else if (stmt.Alternative != null)
            {
                Execute(stmt.Alternative);
            }
            return;
        }


        public void VisitWhileStmt(Stmt.While stmt)
        {
            while (Utility.IsTruthy(exprVisitor.Evaluate(stmt.Condition)))
            {
                try
                {
                    Execute(stmt.Body);
                }
                catch (Jump je)
                {
                    if (je.Token.Type == TokenType.BREAK)
                    {
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return;
        }

        public void VisitJumpStmt(Stmt.Jump stmt)
        {
            throw new Jump(stmt.Label, $"{stmt.Label.Lexeme} must be used inside a loop.");
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            AccretionFunction function = new(stmt, interpreter.Env); // capture current environment when function is *declared* (different from env during call)
            interpreter.Env.Define(stmt.Name.Lexeme, function);
            return;
        }

        public void VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.Value != null) value = exprVisitor.Evaluate(stmt.Value);

            throw new Return(value, stmt.Keyword, "You cannot return from outside of a function.");
        }





        // HELPERS
        public void Execute(Stmt stmt)
        {
            stmt.Accept(this);
            return;
        }
    }
}
