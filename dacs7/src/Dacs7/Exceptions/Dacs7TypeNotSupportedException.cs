using System;

namespace Dacs7
{
    public class Dacs7TypeNotSupportedException : Exception
    {
        public Type NotSupportedType { get; private set; }

        public Dacs7TypeNotSupportedException(Type notSupportedType) : base($"The Type {notSupportedType.Name} is not supported for read or write operations!")
        {
            NotSupportedType = notSupportedType;
        }
    }
}
