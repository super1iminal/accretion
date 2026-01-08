using accretion.Domain;
using accretion.Errors;

namespace accretion.Exceptions
{
    public class Return : RuntimeError
    {
        public readonly object Value;

        public Return(object value, Token label, string message) : base(label, message)
        {
            this.Value = value;
        }
    }
}
