using accretion.Core;

namespace accretion.Natives
{
    public class NativeValue : Native // ie constant
    {
        public string Name { get; }

        public AccType Type { get; }

        public object Value { get; }

        public NativeValue(string name, object value, AccType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            return "<constant>";
        }
    }
}
