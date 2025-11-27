using System.Collections.Generic;

namespace accretion
{
    public abstract class Expr
    {

        public interface IVisitor<T>
        {
            T VisitAssignExpr(Assign expr);
            T VisitTernaryExpr(Ternary expr);
            T VisitBinaryExpr(Binary expr);
            T VisitCallExpr(Call expr);
            T VisitGroupingExpr(Grouping expr);
            T VisitLiteralExpr(Literal expr);
            T VisitLogicalExpr(Logical expr);
            T VisitUnaryExpr(Unary expr);
            T VisitVariableExpr(Variable expr);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);


        public class Assign : Expr
        {
            public Assign(Token name, Expr value)
            {
                this.Name = name;
                this.Value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitAssignExpr(this);
            }
            public readonly Token Name;
            public readonly Expr Value;
        }


        public class Ternary : Expr
        {
            public Ternary(Expr condition, Expr consequent, Expr alternative)
            {
                this.Condition = condition;
                this.Consequent = consequent;
                this.Alternative = alternative;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitTernaryExpr(this);
            }
            public readonly Expr Condition;
            public readonly Expr Consequent;
            public readonly Expr Alternative;
        }


        public class Binary : Expr
        {
            public Binary(Expr left, Token op, Expr right)
            {
                this.Left = left;
                this.Op = op;
                this.Right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitBinaryExpr(this);
            }
            public readonly Expr Left;
            public readonly Token Op;
            public readonly Expr Right;
        }


        public class Call : Expr
        {
            public Call(Expr callee, Token paren, List<Expr> arguments)
            {
                this.Callee = callee;
                this.Paren = paren;
                this.Arguments = arguments;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitCallExpr(this);
            }
            public readonly Expr Callee;
            public readonly Token Paren;
            public readonly List<Expr> Arguments;
        }


        public class Grouping : Expr
        {
            public Grouping(Expr expression)
            {
                this.Expression = expression;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitGroupingExpr(this);
            }
            public readonly Expr Expression;
        }


        public class Literal : Expr
        {
            public Literal(object value)
            {
                this.Value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLiteralExpr(this);
            }
            public readonly object Value;
        }


        public class Logical : Expr
        {
            public Logical(Expr left, Token op, Expr right)
            {
                this.Left = left;
                this.Op = op;
                this.Right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLogicalExpr(this);
            }
            public readonly Expr Left;
            public readonly Token Op;
            public readonly Expr Right;
        }


        public class Unary : Expr
        {
            public Unary(Token op, Expr right)
            {
                this.Op = op;
                this.Right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitUnaryExpr(this);
            }
            public readonly Token Op;
            public readonly Expr Right;
        }


        public class Variable : Expr
        {
            public Variable(Token name)
            {
                this.Name = name;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitVariableExpr(this);
            }
            public readonly Token Name;
        }


    }
}
