using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion
{
    /// <summary>
    /// if the parser is syntactic analysis, this is a *semantic* analysis. 
    /// walk the AST and resolves variable scoping
    /// 
    /// interesting nodes:
    /// block statement creates a new scope for its contained statements
    /// function declaration creates a new scope for its body and its parameters are also in that scope
    /// 
    /// variable declaration adds a new variable to current scope
    /// variable and assignment expression need to have their variables resolved
    /// 
    /// we still need to traverse other nodes because they may contain the above nodes
    /// </summary>
    public class Resolver : Expr.IVisitor<object>, Stmt.IVisitor
    {
        private readonly Interpreter interpreter;
        private readonly Stack<Dictionary<Token, bool>> scopes = new(); // keeps track of the stack of scopes currently in scope
        private Stack<HashSet<Token>> accessedVars = new();
        private FunctionType currentFunction = FunctionType.NONE;
        private bool inLoop = false;
        // each element is a dict representing a block's scope (e.g., [{"a": true}, {"b": false}]
        // keys are variable names, values tell us whether we've finished resolving the variable's initializer
        // only tracks local vars (globals are not tracked)

        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        private enum FunctionType
        {
            NONE,
            FUNCTION
        }



        // STATEMENTS
        public void VisitBlockStmt(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return;
        }

        // variable declaration
        public void VisitVarStmt(Stmt.Var stmt)
        {
            Declare(stmt.Name);
            if (stmt.Initializer != null)
            {
                Resolve(stmt.Initializer); // there may be variable assignment here, but we don't actually care what the result is
            }

            Define(stmt.Name);
            return;
        }

        public void VisitFunctionStmt(Stmt.Function stmt)
        {

            Declare(stmt.Name);
            Define(stmt.Name); // define before resolution to allow function to refer to itself inside its own body

            ResolveFunction(stmt, FunctionType.FUNCTION);
            return;
        }

        // nothing statements
        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitIfStmt(Stmt.If stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Consequent);

            if (stmt.Alternative != null) Resolve(stmt.Alternative); // no control flow, we evaluate both branches
            return;
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            Resolve(stmt.ExpressionValue);
            return;
        }

        public void VisitReturnStmt(Stmt.Return stmt)
        {
            if (currentFunction == FunctionType.NONE)
            {
                Accretion.Error(stmt.Keyword, "Can't return from top-level code.");
            }
            
            if (stmt.Value != null) Resolve(stmt.Value);

            return;
        }

        public void VisitWhileStmt(Stmt.While stmt)
        {
            bool enclosingLoop = inLoop;
            inLoop = true;

            Resolve(stmt.Condition);
            Resolve(stmt.Body);

            inLoop = enclosingLoop;
            return;
        }

        public void VisitJumpStmt(Stmt.Jump stmt)
        {
            if (!inLoop)
            {
                Accretion.Error(stmt.Label, "Can't jump outside of a loop.");
            }
            return;
        }









        // EXPRESSIONS
        public object VisitVariableExpr(Expr.Variable expr)
        {
            if ((scopes.Count > 0) && scopes.Peek().TryGetValue(expr.Name, out bool defined) && (defined == false))
            {
                Accretion.Error(expr.Name, "Can't read local variable in its own initializer");
            }
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            Resolve(expr.Value); // value could have another assignment op
            ResolveLocal(expr, expr.Name);
            return null;
        }

        // nothing expressions
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            Resolve(expr.Callee); // remember, callee can be an expression, but (should) resolve to a variable in interpreter
            foreach (Expr argument in expr.Arguments)
            {
                Resolve(argument);
            }

            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            Resolve(expr.Expression);
            return null;
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            Resolve(expr.Right);
            return null;
        }

        public object VisitTernaryExpr(Expr.Ternary expr)
        {
            Resolve(expr.Condition);
            Resolve(expr.Consequent);
            Resolve(expr.Alternative);

            return null;
        }









        // HELPERS
        public void Resolve(List<Stmt> statements)
        {
            foreach (Stmt stmt in statements)
            {
                Resolve(stmt);
            }
        }

        private void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        private void Resolve(Expr expr)
        {
            expr.Accept(this);
        }

        private void BeginScope()
        {
            scopes.Push(new Dictionary<Token, bool>());
            accessedVars.Push(new HashSet<Token>());
        }

        private void EndScope()
        {
            Dictionary<Token, bool> closedScope = scopes.Pop();
            HashSet<Token> accessedVarsScope = accessedVars.Pop();
            if (accessedVarsScope.Count != closedScope.Count)
            {
                foreach (Token token in closedScope.Keys)
                {
                    if (!accessedVarsScope.Contains(token)) Accretion.Warning(token, "Local variable unused.");
                }
            }
        }


        private void Declare(Token name)
        {
            if (scopes.Count == 0) return;

            Dictionary<Token, bool> scope = scopes.Peek();
            if (scope.ContainsKey(name))
            {
                Accretion.Error(name, "Already a variable with this name in this scope.");
            }
            scope[name] = false; // not ready to be used yet, has only been declared
        }

        private void Define(Token name)
        {
            if (scopes.Count == 0) return;

            scopes.Peek()[name] = true; // now available for use, after initializer
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                if (scopes.ElementAt(i).ContainsKey(name))
                {
                    interpreter.Resolve(expr, i); // tells interpreter how many scopes are between the current scope and scope where var is defined
                    HashSet<Token> accessedVarsScope = accessedVars.ElementAt(i);
                    accessedVarsScope.Add(name);
                    return;
                }
            }
        }

        private void ResolveFunction(Stmt.Function function, FunctionType type)
        {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;

            BeginScope();
            foreach (Token param in function.Parameters)
            {
                Declare(param);
                Define(param);
                // okay to declare AND define because no initializer to accidentally refer to undefined variable
            }

            Resolve(function.Body); // different from how interepreter handles function declarations
                // at runtime, declaring a function doesn't do anything with function body
                // in static analysis, we traverse body
            EndScope();
            currentFunction = enclosingFunction;
        }
    }
}
