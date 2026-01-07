using System;
using System.Collections.Generic;

namespace accretion.Core
{
    public static class NativeAccTypeFactory
    {
        public readonly static AccType VOID = new("void");
        public readonly static AccType DOUBLE = new("double");
        public readonly static AccType STRING = new("string");
        public readonly static AccType BOOL = new("bool");
        public readonly static AccType INT = new("int");

        public readonly static HashSet<AccType> nativeAccTypes = new() { VOID, DOUBLE, STRING, BOOL, INT };

        public static AccType AccTypeFromObject(object value)
        {
            switch (value)
            {
                case double:
                    return DOUBLE;
                case string:
                    return STRING;
                case null:
                    return VOID;
                case bool:
                    return BOOL;
                case int:
                    return INT;
                default:
                    throw new NotImplementedException("Missing implementation for NativeType constructor. Error code 12348.");
            }
        }
    }
    public class AccType
    {
        public readonly string Value;

        public AccType(string value) { Value = value; }

        public override bool Equals(object obj)
        {
            if (obj is not AccType type) return false;
            if (type.Value == null) return false;

            return Equals(Value, type.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class FunType : AccType
    {
        // value is also ReturnType for consistency and resolvability
        public readonly AccType ReturnType; // change this to FunType to allow for returning functions
        public readonly List<AccType> ParamTypes;

        public FunType(AccType returnType, List<AccType> ParamTypes) : base(returnType.Value)
        {
            ReturnType = returnType;
            this.ParamTypes = ParamTypes;
        }

        public FunType(Token returnTypeToken, List<Token> paramTypeTokens) : base(returnTypeToken.Lexeme)
        {
            ReturnType = new AccType(returnTypeToken.Lexeme);
            ParamTypes = new();
            foreach (Token paramTypeToken in paramTypeTokens)
            {
                ParamTypes.Add(new AccType(paramTypeToken.Lexeme));
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FunType type)) return false;
            if (obj == null) return false;

            if (!Equals(ReturnType, type.ReturnType)) return false;
            if (ParamTypes.Count != type.ParamTypes.Count) return false;

            for (int i = 0; i < ParamTypes.Count; i++)
            {
                if (!Equals(ParamTypes[i], type.ParamTypes[i])) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ReturnType.GetHashCode(), ParamTypes.GetHashCode());
        }
    }

}
