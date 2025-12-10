//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace accretion.Resolvers
//{
//    /// <summary>
//    /// if the parser is syntactic analysis, this is a *semantic* analysis. 
//    /// walk the AST and resolves variable scoping
//    /// 
//    /// interesting nodes:
//    /// block statement creates a new scope for its contained statements
//    /// function declaration creates a new scope for its body and its parameters are also in that scope
//    /// 
//    /// variable declaration adds a new variable to current scope
//    /// variable and assignment expression need to have their variables resolved
//    /// 
//    /// we still need to traverse other nodes because they may contain the above nodes
//    /// 
//    /// calculates what scope the current variable is in 
//    /// also checks for variable use before definition
//    /// </summary>
//    public abstract class Resolver<T> : Stmt.IVisitor, Expr.IVisitor<T>
//    {
//        protected readonly Interpreter interpreter;
//        protected readonly Stack<HashSet<T>> scopes = new(); // keeps track of the stack of scopes currently in scope

//        // each element is a dict representing a block's scope (e.g., [{"a": true}, {"b": false}]
//        // keys are variable names, values tell us whether we've finished resolving the variable's initializer
//        // only tracks local vars (globals are not tracked)

//        public Resolver(Interpreter interpreter)
//        {
//            this.interpreter = interpreter;
//        }

//        protected abstract T ProduceDeclared(Token t);
//        protected abstract T ProduceDefined(Token t);

//        // STATEMENTS
//        virtual public void VisitBlockStmt(Stmt.Block stmt)
//        {
//            BeginScope();
//            Resolve(stmt.Statements);
//            EndScope();
//            return;
//        }

//        // variable declaration
//        virtual public void VisitVarStmt(Stmt.Var stmt)
//        {
//            Declare(stmt.Name);
//            if (stmt.Initializer != null)
//            {
//                Resolve(stmt.Initializer); // there may be variable assignment here, but we don't actually care what the result is
//            }

//            Define(stmt.Name);
//            return;
//        }

//        virtual public void VisitFunctionStmt(Stmt.Function stmt)
//        {

//            Declare(stmt.Name);
//            Define(stmt.Name); // define before resolution to allow function to refer to itself inside its own body

//            ResolveFunction(stmt);
//            return;
//        }

//        // nothing statements
//        abstract public void VisitExpressionStmt(Stmt.Expression stmt);

//        virtual public void VisitIfStmt(Stmt.If stmt)
//        {
//            Resolve(stmt.Condition);
//            Resolve(stmt.Consequent);

//            if (stmt.Alternative != null) Resolve(stmt.Alternative); // no control flow, we evaluate both branches
//            return;
//        }

//        virtual public void VisitPrintStmt(Stmt.Print stmt)
//        {
//            Resolve(stmt.ExpressionValue);
//            return;
//        }

//        virtual public void VisitReturnStmt(Stmt.Return stmt)
//        {

//            if (stmt.Value != null) Resolve(stmt.Value);

//            return;
//        }

//        virtual public void VisitWhileStmt(Stmt.While stmt)
//        {
//            Resolve(stmt.Condition);
//            Resolve(stmt.Body);
//            return;
//        }

//        virtual public void VisitJumpStmt(Stmt.Jump stmt)
//        {
//            return;
//        }


//        // EXPRESSIONS
//        virtual public T VisitVariableExpr(Expr.Variable expr)
//        {
//            ResolveVar(expr, expr.Name);
//            return default;
//        }

//        virtual public T VisitAssignExpr(Expr.Assign expr)
//        {
//            Resolve(expr.Value); // value could have another assignment op
//            ResolveVar(expr, expr.Name);
//            return default;
//        }

//        // nothing expressions
//        virtual public T VisitBinaryExpr(Expr.Binary expr)
//        {
//            Resolve(expr.Left);
//            Resolve(expr.Right);
//            return default;
//        }

//        virtual public T VisitCallExpr(Expr.Call expr)
//        {
//            Resolve(expr.Callee); // remember, callee can be an expression, but (should) resolve to a variable in interpreter
//            foreach (Expr argument in expr.Arguments)
//            {
//                Resolve(argument);
//            }

//            return default;
//        }

//        virtual public T VisitGroupingExpr(Expr.Grouping expr)
//        {
//            Resolve(expr.Expression);
//            return default;
//        }

//        virtual public T VisitLiteralExpr(Expr.Literal expr)
//        {
//            return default;
//        }

//        virtual public T VisitLogicalExpr(Expr.Logical expr)
//        {
//            Resolve(expr.Left);
//            Resolve(expr.Right);
//            return default;
//        }

//        virtual public T VisitUnaryExpr(Expr.Unary expr)
//        {
//            Resolve(expr.Right);
//            return default;
//        }

//        virtual public T VisitTernaryExpr(Expr.Ternary expr)
//        {
//            Resolve(expr.Condition);
//            Resolve(expr.Consequent);
//            Resolve(expr.Alternative);

//            return default;
//        }



//        // HELPERS
//        virtual public void BeginResolve(List<Stmt> statements)
//        {
//            BeginScope();
//            Resolve(statements);
//            EndScope();
//        }
//        virtual protected void Resolve(List<Stmt> statements)
//        {
//            foreach (Stmt stmt in statements)
//            {
//                Resolve(stmt);
//            }
//        }

//        virtual protected void Resolve(Stmt stmt)
//        {
//            stmt.Accept(this);
//        }

//        virtual protected void Resolve(Expr expr)
//        {
//            expr.Accept(this);
//        }


//        virtual protected void BeginScope()
//        {
//            scopes.Push(new HashSet<T>());
//        }

//        virtual protected HashSet<T> EndScope()
//        {
//            return scopes.Pop();
//        }


//        virtual protected void Declare(T add)
//        {
//            if (scopes.Count == 0) return;

//            HashSet<T> scope = scopes.Peek();
//            scope.Add(add);
//            //if (scope.Contains(Produce(name)))
//            //{
//            //    Accretion.Error(name, "Already a variable with this name in this scope.");
//            //} TODO: add check
//        }

//        virtual protected void Define(T add, params T[] remove)
//        {
//            if (scopes.Count == 0) return;

//            HashSet<T> scope = scopes.Peek();
//            foreach (T t in remove)
//            {
//                scope.Remove(t);
//            }
//            scope.Add(add);
//        }

//        virtual protected void ResolveVar(Expr expr, Token name)
//        {
//            for (int i = 0; i < scopes.Count; i++)
//            {
//                if (scopes.ElementAt(i).Contains(Produce(name)))
//                {
//                    interpreter.Resolve(expr, i); // tells interpreter how many scopes are between the current scope and scope where var is defined
//                    return;
//                }
//            }

//            Accretion.Error(name, "There is no declared variable with that name.");
//        }

//        virtual protected void ResolveFunction(Stmt.Function function)
//        {
//            BeginScope();
//            foreach (Token param in function.Parameters)
//            {
//                Declare(param);
//                Define(param);
//                // okay to declare AND define because no initializer to accidentally refer to undefined variable
//            }

//            Resolve(function.Body); // different from how interepreter handles function declarations
//                                    // at runtime, declaring a function doesn't do anything with function body
//                                    // in static analysis, we traverse body
//            EndScope();

//        }
//    }
//}
