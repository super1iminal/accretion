using System;
using System.Text;

namespace accretion
{
    //public class ASTPrinterPrinter
    //{
    ////    static void Main(string[] args)
    ////    {
    ////        Expr expression = new Expr.Binary(
    ////            new Expr.Unary(
    ////                new Token(TokenType.MINUS, "-", null, 1),
    ////                new Expr.Literal(123)),
    ////            new Token(TokenType.STAR, "*", null, 1),
    ////            new Expr.Grouping(
    ////                new Expr.Literal(45.67))
    ////        );

    ////        Console.WriteLine(new ASTVisitor().Print(expression));
    ////    }
    //}


    //public class ASTPrinter : Expr.IVisitor<string>
    //{
    //    // endpoint to use printer
    //    public string Print(Expr expr)
    //    {
    //        return expr.Accept(this);
    //    }

    //    // visiting endpoints
    //    public string VisitBinaryExpr(Expr.Binary expr)
    //    {
    //        return Paranthesize(expr.Op.Lexeme, expr.Left, expr.Right);
    //    }

    //    public string VisitGroupingExpr(Expr.Grouping expr)
    //    {
    //        return Paranthesize("group", expr.Expression);
    //    }

    //    public string VisitLiteralExpr(Expr.Literal expr)
    //    {
    //        if (expr.Value == null) return "nil";
    //        return expr.Value.ToString();
    //    }

    //    public string VisitTernaryExpr(Expr.Ternary expr)
    //    {
    //        return Paranthesize("?:", expr.Condition, expr.Consequent, expr.Alternative);
    //    }

    //    public string VisitUnaryExpr(Expr.Unary expr)
    //    {
    //        return Paranthesize(expr.Op.Lexeme, expr.Right);
    //    }

    //    private string Paranthesize(string name, params Expr[] exprs)
    //    {
    //        StringBuilder builder = new StringBuilder();

    //        builder.Append($"({name}");
    //        foreach (Expr e in exprs)
    //        {
    //            builder.Append($" {e.Accept(this)}"); // recursive
    //        }
    //        builder.Append(")");

    //        return builder.ToString();
    //    }
    //}
}
