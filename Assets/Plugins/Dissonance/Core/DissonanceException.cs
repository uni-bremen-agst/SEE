using System;

namespace Dissonance
{
    public class DissonanceException
        : Exception
    {
        public DissonanceException(string message)
            : base(message)
        {
        }
    }
}
