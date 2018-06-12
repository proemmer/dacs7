using System;

namespace Dacs7
{
    public class Dacs7InvalidTagException : Exception
    {
        public string Tag { get; private set; }

        public Dacs7InvalidTagException(string tag) : base($"The given Tag '{tag}'could not be parsed!")
        {
            Tag = tag;
        }
    }
}
