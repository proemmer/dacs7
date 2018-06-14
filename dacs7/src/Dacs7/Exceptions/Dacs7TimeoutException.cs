using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7TimeoutException : Exception
    {
        public Dacs7TimeoutException() : base($"Operation timeout")
        {
        }
    }
}