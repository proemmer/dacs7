using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7InvalidResultException : Exception
    {
        public Dacs7InvalidResultException() : base($"Invalid result or Timeout!")
        {
        }
    }
}