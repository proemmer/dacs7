// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Dacs7
{
    [Serializable]
    public class Dacs7ParameterException : Exception, ISerializable
    {
        public ErrorParameter ErrorCode { get; private set; }

        public Dacs7ParameterException(ushort errorCode) :
            base($"No success error code: <{Dacs7Exception.ResolveErrorCode<ErrorParameter>(errorCode)}>")
        {
            ErrorCode = (ErrorParameter)errorCode;
        }

        public Dacs7ParameterException()
        {
        }

        public Dacs7ParameterException(string message) : base(message)
        {
        }

        public Dacs7ParameterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected Dacs7ParameterException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            ErrorCode = (ErrorParameter)serializationInfo.GetValue("ErrorCode", typeof(ErrorParameter));
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorCode", ErrorCode);
        }
    }
}
