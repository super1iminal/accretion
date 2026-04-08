using accretion.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Utilities
{
    public static class Utility
    {
        public static string Stringify(object obj)
        {
            if (obj == null) return "nil";

            if (obj is double d)
            {
                if (double.IsPositiveInfinity(d)) return "Infinity";
                if (double.IsNegativeInfinity(d)) return "-Infinity";
                if (double.IsNaN(d)) return "NaN";

                string text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }

            if (obj is bool b)
            {
                return b ? "true" : "false";
            }

            return obj.ToString();
        }
        public static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            if (obj is double d) return d != (double)0.0;
            if (obj is int i) return i != 0;
            if (obj is string s) return s != "";
            return true;
        }

        public static bool IsEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            return Equals(left, right);
        }

    }
}
