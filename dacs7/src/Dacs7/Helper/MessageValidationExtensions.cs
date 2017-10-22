using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Helper
{
    internal static class MessageValidationExtensions
    {
        internal static void EnsureValidParameterErrorCode(this IMessage msg, ushort valid = 0)
        {
            var errorCode = msg.GetAttribute("ParamErrorCode", (ushort)0);
            if (errorCode != valid)
                throw new Dacs7ParameterException(errorCode);
        }

        internal static void EnsureValidReturnCode(this IMessage msg, byte valid = 0xff)
        {
            var returnCode = msg.GetAttribute("ReturnCode", (byte)0);
            if (returnCode != valid)
                throw new Dacs7ReturnCodeException(returnCode);
        }

        internal static void EnsureValidErrorClass(this IMessage msg, byte valid = 0x00)
        {
            var errorClass = msg.GetAttribute("ErrorClass", (byte)0);
            if (errorClass != valid)
            {
                var errorCode = msg.GetAttribute("ErrorCode", (byte)0);
                throw new Dacs7Exception(errorClass, errorCode);
            }
        }
    }
}
