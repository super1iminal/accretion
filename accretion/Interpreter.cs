using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    // tree-walk interpreter
    // need a type check for each cast
    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor // remember, object is the return value of the visitor
    {
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
                Accretion.AccretionRuntimeError(e);
            }
        }

        // API HELPERS



        // ======== EXPR VISITOR ======== 
        // VISITOR METHODS
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.GREATER:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left <= (double)right;
                case TokenType.MINUS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left - (double)right;
                case TokenType.PLUS:
                    if ((left is double ld) && (right is double rd))
                    {
                        return ld + rd;
                    }
                    else if ((left is string ls) && (right is string rs))
                    {
                        return ls + rs;
                    }
                    else
                    {
                        throw new RuntimeError(expr.Op, "Operands must be two numbers or two strings");
                    }
                case TokenType.SLASH:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left / (double)right;
                case TokenType.STAR:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left * (double)right;

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
                    CheckNumberOperand(expr.Op, right);
                    return -(double)right; // although right should be a number, we can't statically know that, so we cast it
            }

            return null; // unreachable, but required to satisfy all paths must return a value
        }





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
            Console.WriteLine(Stringify(value));
            return;
        }










        // ======== HELPERS ======== 
        private object Evaluate(Expr expr)
        {
            return expr.Accept(this); // recursive
        }

        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
            return;
        }

        private static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            if (obj is double d) return d != ((double)0.0);
            if (obj is string s) return s != "";
            return true;
        }

        private static bool IsEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            return Equals(left, right);
        }

        private static void CheckNumberOperand(Token optr, object opnd)
        {
            if (opnd is double) return;

            throw new RuntimeError(optr, "Operand must be a number.");
        }

        private static void CheckNumberOperands(Token optr, object left, object right)
        {
            if (left is double && right is double) return;

            throw new RuntimeError(optr, "Operands must be numbers.");
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
