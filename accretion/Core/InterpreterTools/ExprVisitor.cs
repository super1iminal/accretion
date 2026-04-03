using accretion.Callables;
using accretion.Domain;
using accretion.Errors;
using accretion.Utilities;
using System;
using System.Collections.Generic;

namespace accretion.Core.InterpreterTools
{
    public class ExprVisitor : Expr.IVisitor<object>
    {
        private readonly Interpreter interpreter;
        private readonly Logger logger;
        private readonly ErrorManager errors;

        public ExprVisitor(Interpreter interpreter, Logger logger, ErrorManager errors)
        {
            this.interpreter = interpreter;
            this.logger = logger;
            this.errors = errors;
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);
            Token op = expr.Op;

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                    return CompareOp(op, left, right, (l, r) => l > r);
                case TokenType.GREATER_EQUAL:
                    return CompareOp(op, left, right, (l, r) => l >= r);
                case TokenType.LESS:
                    return CompareOp(op, left, right, (l, r) => l < r);
                case TokenType.LESS_EQUAL:
                    return CompareOp(op, left, right, (l, r) => l <= r);
                case TokenType.MINUS:
                    return NumericOp(op, left, right, (l, r) => l - r);
                case TokenType.PLUS:
                    if ((left is string) || (right is string)) return StringOp(op, left, right, (l, r) => l + r);
                    else return NumericOp(op, left, right, (l, r) => l + r);
                case TokenType.SLASH:
                    return NumericOp(op, left, right, (l, r) => l / r, true); // force double because division
                case TokenType.STAR:
                    return NumericOp(op, left, right, (l, r) => l * r);

                case TokenType.BANG_EQUAL:
                    return !Utility.IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return Utility.IsEqual(left, right);
            }

            throw new NotImplementedException("Oops... Haven't done that yet. Error code 0075."); // unreachable
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
            if (Utility.IsTruthy(cond))
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
                    return !Utility.IsTruthy(right);
                case TokenType.MINUS:
                    if (IsInt(right))
                    {
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

            if (interpreter.Locals.TryGetValue(expr, out int distance))
            {
                interpreter.Env.AssignAt(distance, expr.Name, value);
            }
            else if (interpreter.Globals.TryGet(expr.Name, out object _))
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
                if (Utility.IsTruthy(left)) return true;
            }
            else
            {
                if (!Utility.IsTruthy(left)) return false;
            }

            return Utility.IsTruthy(Evaluate(expr.Right));
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.Callee); // uses variable evaluation, gets fn from environment

            List<object> arguments = new();
            foreach (Expr argument in expr.Arguments)
            {
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


            return function.Call(interpreter, arguments);

        }







        // HELPERS
        public object Evaluate(Expr expr)
        {
            return expr.Accept(this); // recursive
        }

        public object LookupVariable(Token name, Expr expr)
        {
            if (interpreter.Locals.TryGetValue(expr, out int distance))
            {
                return interpreter.Env.GetAt(distance, name.Lexeme);
            }
            else if (interpreter.Globals.TryGet(name, out object value))
            {
                return value;
            }
            else
            {
                throw new RuntimeError(name, "Attempting to get a variable that does not exist.");
            }
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

        private static bool IsNum(params object[] objects)
        {
            foreach (object o in objects)
            {
                if ((o is not int) && (o is not double))
                {
                    return false;
                }
            }

            return true;
        }

        private static double ToDouble(object obj)
        {
            double result;

            if (obj is int) result = (double)(int)obj;
            else if (obj is double) result = (double)obj;
            else throw new ApplicationException("You should not be here. Error code 67676.");

            return result;
        }

        private static object NumericOp(Token op, object left, object right, Func<double, double, double> operation, bool forceDouble = false)
        {
            if (!IsNum(left, right))
            {
                throw new RuntimeError(op, "Operands must be numbers");
            }

            double result = operation(ToDouble(left), ToDouble(right));

            if ((!forceDouble) && (left is int) && (right is int))
            {
                return (int)Math.Floor(result);
            }

            return result;
        }

        public object CompareOp(Token op, object left, object right, Func<double, double, bool> operation)
        {
            if (!IsNum(left, right))
            {
                throw new RuntimeError(op, "Operands must be numbers");
            }

            return operation(ToDouble(left), ToDouble(right));
        }

        private static object StringOp(Token op, object left, object right, Func<string, string, string> operation)
        {
            return operation(left.ToString(), right.ToString());
        }
    }
}
