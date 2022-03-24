// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Dacs7
{
    [Serializable]
    public class Dacs7TypeNotSupportedException : Exception, ISerializable
    {
        public Type NotSupportedType { get; private set; }

        public Dacs7TypeNotSupportedException(Type notSupportedType) : base($"The Type {notSupportedType.Name} is not supported for read or write operations!")
        {
            NotSupportedType = notSupportedType;
        }

        public Dacs7TypeNotSupportedException()
        {
        }

        public Dacs7TypeNotSupportedException(string message) : base(message)
        {
        }

        public Dacs7TypeNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected Dacs7TypeNotSupportedException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            NotSupportedType = (Type)serializationInfo.GetValue("NotSupportedType", typeof(Type));
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("NotSupportedType", NotSupportedType);
        }
    }
}
