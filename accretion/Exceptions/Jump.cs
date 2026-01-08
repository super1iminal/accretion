using accretion.Domain;
using accretion.Errors;

namespace accretion.Exceptions
{
    public class Jump : RuntimeError
    {
        public Jump(Token label, string message) : base(label, message)
        {

        }
    }
}
