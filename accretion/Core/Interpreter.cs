using accretion.Callables;
using accretion.Errors;
using accretion.Exceptions;
using accretion.Natives;
using System;
using System.Collections.Generic;

namespace accretion.Core
{
    // tree-walk interpreter
    // need a type check for each cast
    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor // remember, object is the return value of the visitor
    {
        private readonly Logger logger;
        private readonly ErrorManager errors;
        
        private SingleEnvironment globals = new();
        private LayeredEnvironment environment = new();
        private readonly Dictionary<Expr, int> locals = new(); // used indirectly (through Resolve() below) by Resolver
                                                               // (resolves scope of variables, e.g., Expr x is 5 scopes away,
                                                               // but we know that Expr x (different) is 1 scope away)
        

        // native function

        public Interpreter(ErrorManager errors, Logger logger)
        {
            foreach (var native in NativeRegistry.All)
            {
                globals.Define(native.Name, native.Value);
            }

            this.errors = errors;
            this.logger = logger;
        }
        // PUBLIC API
        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                } 
            }
            catch (RuntimeError e)
            {
                errors.RuntimeError(e);
            }
        }

        // API HELPERS










        // ======== STMT VISITOR ======== 
        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.ExpressionValue); // resolve the expression using the visitor
                                            // (recall: pass myself to the expression, expression accepts me and
                                            // passes itself to one of my functions with itself and runs it, returns output of my function

            return;
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.ExpressionValue);
            logger.Log(Stringify(value));
            return;
        }

        // variable declaration
        public void VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.Initializer != null)
            {
                value = Evaluate(stmt.Initializer);
            }

            environment.Define(stmt.Name.Lexeme, value);
            return;
        }

        public void VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.Statements, new LayeredEnvironment(environment));
        }

        public void VisitIfStmt(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.Condition)))
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
            while (IsTruthy(Evaluate(stmt.Condition)))
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
            AccretionFunction function = new(stmt, environment); // capture current environment when function is *declared* (different from env during call)
            environment.Define(stmt.Name.Lexeme, function);
            return;
        }

        public void VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.Value != null) value = Evaluate(stmt.Value);

            throw new Return(value, stmt.Keyword, "You cannot return from outside of a function.");
        }

















        // ======== EXPR VISITOR ======== 
        // VISITOR METHODS
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                    if (IsDouble(left, right)) return (double)left > (double)right;
                    if (IsInt(left, right)) return (int)left > (int)right;
                    throw new RuntimeError(expr.Op, "Operands must be numbers.");
                case TokenType.GREATER_EQUAL:
                    if (IsDouble(left, right)) return (double)left >= (double)right;
                    if (IsInt(left, right)) return (int)left >= (int)right;
                    throw new RuntimeError(expr.Op, "Operands must be numbers.");
                case TokenType.LESS:
                    if (IsDouble(left, right)) return (double)left < (double)right;
                    if (IsInt(left, right)) return (int)left < (int)right;
                    throw new RuntimeError(expr.Op, "Operands must be numbers.");
                case TokenType.LESS_EQUAL:
                    if (IsDouble(left, right)) return (double)left <= (double)right;
                    if (IsInt(left, right)) return (int)left <= (int)right;
                    throw new RuntimeError(expr.Op, "Operands must be numbers.");
                case TokenType.MINUS:
                    if (IsDouble(left, right)) return (double)left - (double)right;
                    if (IsInt(left, right)) return (int)left - (int)right;
                    throw new RuntimeError(expr.Op, "Operands must be numbers.");
                case TokenType.PLUS:
                    return HandleAddition(expr.Op, left, right);
                case TokenType.SLASH:
                    if (IsDouble(left, right))
                    {
                        return (double)left / (double)right;
                    }
                    else if (IsInt(left, right))
                    {
                        return (int)left / (double)(int)right;
                    }
                    else if (IsInt(left) && IsDouble(right))
                    {
                        return (int)left / (double)right;
                    }
                    else if (IsDouble(left) && IsInt(right))
                    {
                        return (double)left / (int)right;
                    }
                    else
                    {
                        throw new RuntimeError(expr.Op, "Operands must be numbers.");
                    }

                case TokenType.STAR:
                    if (IsDouble(left, right))
                    {
                        return (double)left * (double)right;
                    }
                    else if (IsInt(left, right))
                    {
                        return (int)left * (double)(int)right;
                    }
                    else if (IsInt(left) && IsDouble(right))
                    {
                        return (int)left * (double)right;
                    }
                    else if (IsDouble(left) && IsInt(right))
                    {
                        return (double)left * (int)right;
                    }
                    else
                    {
                        throw new RuntimeError(expr.Op, "Operands must be numbers.");
                    }

                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);

            }

            return null; // unreachable, but required to satisfy all paths must return a value
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.Expression);
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.Value;
        }

        public object VisitTernaryExpr(Expr.Ternary expr)
        {
            object cond = Evaluate(expr.Condition);

            // evaluate in if statement because cond/alt could not work when cond not true
            if (IsTruthy(cond))
            {
                return Evaluate(expr.Consequent);
            } 
            else
            {
                return Evaluate(expr.Alternative);
            }
        }

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = Evaluate(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.BANG:
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    if (IsInt(right)) {
                        return -(int)right;
                    }
                    else if (IsDouble(right))
                    {
                        return -(double)right; // although right should be a number, we can't statically know that, so we cast it
                    }
                    else
                    {
                        throw new RuntimeError(expr.Op, "Operand must be a number.");
                    }
                        
            }

            return null; // unreachable, but required to satisfy all paths must return a value
        }
        
        // overloaded, functions are also technically "variables"
        public object VisitVariableExpr(Expr.Variable expr)
        {
            return LookupVariable(expr.Name, expr);
        }

        // the reason why assignments are expressions is because they produce a value
        public object VisitAssignExpr(Expr.Assign expr)
        {
            object value = Evaluate(expr.Value);

            if (locals.TryGetValue(expr, out int distance))
            {
                environment.AssignAt(distance, expr.Name, value);
            }
            else if (globals.TryGet(expr.Name, out object _))
            {
                throw new RuntimeError(expr.Name, "Attempting to assign to a global variable.");
            }
            else
            {
                throw new RuntimeError(expr.Name, "Attempting to assign to a variable that does not exist.");
            }
            return value;
        }


        // not within binary due to short-circuiting logic
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.Left);

            // short circuiting
            if (expr.Op.Type == TokenType.OR)
            {
                if (IsTruthy(left)) return true;
            } else
            {
                if (!IsTruthy(left)) return false;
            }

            return IsTruthy(Evaluate(expr.Right));
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.Callee); // uses variable evaluation, gets fn from environment

            List<object> arguments = new();
            foreach (Expr argument in expr.Arguments) {
                arguments.Add(Evaluate(argument));
            }

            if (!(callee is AccretionCallable))
            {
                throw new ApplicationException("Typing should have been caught by the Typer. Error code 67695.");
            }

            AccretionCallable function = (AccretionCallable)callee;
            if (arguments.Count != function.Arity)
            {
                throw new RuntimeError(expr.Paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");
            }


            return function.Call(this, arguments);

        }











        // ======== HELPERS ======== 
        // recursively evaluates an expression and returns the result
        private object Evaluate(Expr expr)
        {
            return expr.Accept(this); // recursive
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
            return;
        }

        public void Resolve(Expr expr, int depth)
        {
            locals[expr] = depth;
            // since we're using expr and not name, the difference between vars (if multiple of same name) is builtin to our locals dict
        }

        public object LookupVariable(Token name, Expr expr)
        {
            if (locals.TryGetValue(expr, out int distance))
            {
                return environment.GetAt(distance, name.Lexeme);
            }
            else if (globals.TryGet(name, out object value))
            {
                return value;
            }
            else
            {
                throw new RuntimeError(name, "Attempting to get a variable that does not exist.");
            }
        }

        public void ExecuteBlock(List<Stmt> statements, LayeredEnvironment environment)
        {
            LayeredEnvironment previous = this.environment;

            try
            {
                this.environment = environment;

                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }

        private static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            if (obj is double d) return d != (double)0.0;
            if (obj is int i) return i != 0;
            if (obj is string s) return s != "";
            return true;
        }

        private static bool IsEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            return Equals(left, right);
        }

        private static bool IsDouble(params object[] objs)
        {
            foreach (object obj in objs)
            {
                if (obj is not double) return false;
            }
            return true;
        }

        private static bool IsInt(params object[] objs)
        {
            foreach (object obj in objs)
            {
                if (obj is not int) return false;
            }
            return true;
        }

        private static bool IsString(params object[] objs)
        {
            foreach (object obj in objs)
            {
                if (obj is not string) return false;
            }
            return true;
        }

        private object HandleAddition(Token op, object left, object right)
        {
            if ((IsString(left) || IsString(right)) && IsAlphaNum(left, right))
            {
                return left.ToString() + right.ToString();
            }
            else if (IsDouble(left, right))
            {
                return (double)left + (double)right;
            }
            else if (IsInt(left, right))
            {
                return (int)left + (int)right;
            }
            else if (IsInt(left) && IsDouble(right))
            {
                return (int)left + (double)right;
            }
            else if (IsDouble(left) && IsInt(right))
            {
                return (double)left + (int)right;
            }
            else
            {
                throw new RuntimeError(op, "Operands must be a combination of numbers and strings");
            }
        }

        private static bool IsAlphaNum(params object[] objs)
        {
            foreach (object obj in objs)
            {
                if (!(IsString(obj) || IsInt(obj) || IsDouble(obj))) return false;
            }
            return true;
        }

        public string Stringify(object obj)
        {
            if (obj == null) return "nil";

            if (obj is double)
            {
                string text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }

            return obj.ToString();
        }

    }
}
