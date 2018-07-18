using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7ReadTimeoutException : Exception
    {
        public Dacs7ReadTimeoutException(ushort id) : base($"Read operation timeout for job {id}")
        {
        }
    }
}