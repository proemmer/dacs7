using System;

namespace Dacs7
{
    public class Dacs7ParameterException : Exception
    {
        public ErrorParameter ErrorCode { get; private set; }

        public Dacs7ParameterException(ushort errorCode) :
            base($"No success error code: <{Dacs7Exception.ResolveErrorCode<ErrorParameter>(errorCode)}>")
        {
            ErrorCode = (ErrorParameter)errorCode;
        }
    }
}
