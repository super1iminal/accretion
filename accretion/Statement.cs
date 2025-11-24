namespace accretion
{
    public abstract class Stmt
    {

        public interface IVisitor
        {
            void VisitExpressionStmt(Expression stmt);
            void VisitPrintStmt(Print stmt);
        }

        public abstract void Accept(IVisitor visitor);


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


    }
}
