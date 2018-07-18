using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7WriteTimeoutException : Exception
    {
        public Dacs7WriteTimeoutException(ushort id) : base($"Write operation timeout for job {id}")
        {
        }
    }
}