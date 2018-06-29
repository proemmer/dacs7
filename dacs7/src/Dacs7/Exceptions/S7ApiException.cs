using System;

namespace Dacs7.Exceptions
{
    public class S7ApiException : Exception
    {
        public S7ApiException(int result, string message) : base(message)
        {
        }
    }
}
