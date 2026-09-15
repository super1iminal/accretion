using accretion.Core;

namespace accretion.Natives
{
    public interface Native
    {
        public string Name { get; }
        public AccType Type { get; }
        public object Value { get; }

        public string ToString();
    }
}
