using System.Collections.Generic;

namespace accretion
{
    public abstract class Stmt
    {

        public interface IVisitor
        {
            void VisitBlockStmt(Block stmt);
            void VisitExpressionStmt(Expression stmt);
            void VisitFunctionStmt(Function stmt);
            void VisitIfStmt(If stmt);
            void VisitJumpStmt(Jump stmt);
            void VisitPrintStmt(Print stmt);
            void VisitReturnStmt(Return stmt);
            void VisitVarStmt(Var stmt);
            void VisitWhileStmt(While stmt);
        }

        public abstract void Accept(IVisitor visitor);


        public class Block : Stmt
        {
            public Block(List<Stmt> statements)
            {
                this.Statements = statements;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitBlockStmt(this);
                return;
            }
            public readonly List<Stmt> Statements;
        }


        public class Expression : Stmt
        {
            public Expression(Expr expression)
            {
                this.ExpressionValue = expression;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitExpressionStmt(this);
                return;
            }
            public readonly Expr ExpressionValue;
        }


        public class Function : Stmt
        {
            public Function(Token name, List<Token> parameters, List<Stmt> body)
            {
                this.Name = name;
                this.Parameters = parameters;
                this.Body = body;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitFunctionStmt(this);
                return;
            }
            public readonly Token Name;
            public readonly List<Token> Parameters;
            public readonly List<Stmt> Body;
        }


        public class If : Stmt
        {
            public If(Expr condition, Stmt consequent, Stmt alternative)
            {
                this.Condition = condition;
                this.Consequent = consequent;
                this.Alternative = alternative;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitIfStmt(this);
                return;
            }
            public readonly Expr Condition;
            public readonly Stmt Consequent;
            public readonly Stmt Alternative;
        }


        public class Jump : Stmt
        {
            public Jump(Token label)
            {
                this.Label = label;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitJumpStmt(this);
                return;
            }
            public readonly Token Label;
        }


        public class Print : Stmt
        {
            public Print(Expr expression)
            {
                this.ExpressionValue = expression;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitPrintStmt(this);
                return;
            }
            public readonly Expr ExpressionValue;
        }


        public class Return : Stmt
        {
            public Return(Token keyword, Expr value)
            {
                this.Keyword = keyword;
                this.Value = value;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitReturnStmt(this);
                return;
            }
            public readonly Token Keyword;
            public readonly Expr Value;
        }


        public class Var : Stmt
        {
            public Var(Token name, Expr initializer)
            {
                this.Name = name;
                this.Initializer = initializer;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitVarStmt(this);
                return;
            }
            public readonly Token Name;
            public readonly Expr Initializer;
        }


        public class While : Stmt
        {
            public While(Expr condition, Stmt body)
            {
                this.Condition = condition;
                this.Body = body;
            }

            public override void Accept(IVisitor visitor)
            {
                visitor.VisitWhileStmt(this);
                return;
            }
            public readonly Expr Condition;
            public readonly Stmt Body;
        }


    }
}
