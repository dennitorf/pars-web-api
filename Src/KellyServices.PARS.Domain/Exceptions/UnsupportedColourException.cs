using System;

namespace KellyServices.PARS.Domain.Exceptions
{
    public class UnsupportedColourException : Exception
    {
        public UnsupportedColourException(string code) : base($"Colour \"{code}\" is unsupported.")
        {
        }
    }
}
