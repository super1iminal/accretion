using accretion.Core.ResolverTools;
using accretion.Domain;
using accretion.Errors;
using accretion.Natives;
using System;
using System.Collections.Generic;
using System.Text;

namespace accretion.Core
{

    public class Typer
    {
        // typer stuff
        public struct Signature
        {
            public Token SToken; // token is used for error reporting purposes
            public string Identifier; // used as canon ID
            public AccType AType;

            public Signature(string identifier, AccType AType)
            {
                this.Identifier = identifier;
                this.AType = AType;
                this.SToken = null;
            }

            public Signature(Token token, AccType AType)
            {
                this.SToken = token;
                this.AType = AType;
                this.Identifier = token.Lexeme;
            }

            public override bool Equals(object obj)
            {
                if (obj is not Signature otherSig) return false;
                return Equals(Identifier, otherSig.Identifier) && Equals(AType, otherSig.AType);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Identifier, AType.GetHashCode());
            }
        }

        internal readonly Stack<HashSet<Signature>> scopes = new();


        internal readonly HashSet<AccType> validTypes = NativeAccTypeFactory.nativeAccTypes;
        internal readonly AccType ignoreType = new("ignore"); // anytime a variable or function is ignored due to non-existent types, it is set to ignore
                                                             // so the user doesn't get flooded with compile errors

        // resolver stuff
        internal readonly Interpreter interpreter;


        // heuristic stuff
        // also, tokens w/ same lexeme are diff, so no worries about overloaded funcs here
        internal Stack<Dictionary<string, Token>> notAccessedYet = new(); // check whether all vars in a scope have been used (key: lexeme for vars, mangled name for funs)
        // private Stack<HashSet<Token>> notDefinedYet = new(); // check whether var is defined yet
        internal bool inFunction = false; // to see whether we're returning outside of a function
        internal bool inloop = false;
        internal AccType currentFunctionType = null; // to see if return value matches stated function return value


        // shared stuff
        internal readonly ErrorManager errors;

        private readonly StmtVisitor stmtVisitor;

        public Typer(Interpreter interpreter, ErrorManager errors)
        {
            BeginScope();
            SetupNatives();
            this.errors = errors;
            this.interpreter = interpreter;

            stmtVisitor = new(this);
        }


        // PUBLIC API
        public void BeginResolve(List<Stmt> statements)
        {
            BeginScope();
            stmtVisitor.Resolve(statements);
            EndScope();
        }


        // SHARED HELPERS

        public AccType ImplicitCast(AccType valueType, AccType sourceType)
        {
            // hardcoded (native) implicit casts:
            if (Equals(valueType, NativeAccTypeFactory.INT) && Equals(sourceType, NativeAccTypeFactory.DOUBLE))
            {
                return NativeAccTypeFactory.DOUBLE;
            }

            // todo: if classes, maybe do implcit casting here?

            return valueType;
        }

        /// <summary>
        ///  helper to check if any of the given types are of ignoreType. if so,
        ///  the ignore type should be propagated up the expression tree, since part of the expression is corrupted.
        /// </summary>
        public bool PropagateIgnore(params AccType[] types)
        {
            foreach (AccType type in types)
            {
                if (Equals(type, ignoreType)) return true;
            }
            return false;
        }

        internal void BeginScope()
        {
            scopes.Push(new());
            notAccessedYet.Push(new Dictionary<string, Token>());
            // notDefinedYet.Push(new HashSet<Token>());
        }

        internal void EndScope()
        {
            HashSet<Signature> closedScope = scopes.Pop();
            Dictionary<string, Token> varsNotAccessedYet = notAccessedYet.Pop();
            // HashSet<Token> varsNotDefinedYet = notDefinedYet.Pop();

            foreach (Token token in varsNotAccessedYet.Values) errors.CompilerWarning(token, "Variable/Function is never used");
            // foreach (Token token in varsNotDefinedYet) errors.CompilerWarning(token, "Variable is declared but never assigned a value");
        }

        internal void Define(Token name) // unused
        {
            // notDefinedYet.Peek().Remove(name);
        }

        public static string MangleName(string name, List<AccType> paramTypes)
        {
            StringBuilder sb = new();
            sb.Append(name);
            foreach (AccType pType in paramTypes)
            {
                sb.Append(pType.Value);
            }

            return sb.ToString();
        }

        private void SetupNatives()
        {
            foreach (var native in NativeRegistry.All)
            {
                scopes.Peek().Add(new Signature(native.Name, native.Type));  // true because it's already been defined, obv
            }
        }
    }
}

// todo: combination of resolver and type mostly complete. still need to integrate interpreter and typer tho for resolutions
