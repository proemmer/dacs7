// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Dacs7
{
    [Serializable]
    public class Dacs7ReturnCodeException : Exception, ISerializable
    {
        public byte ReturnCode { get; private set; }
        public int ItemNumber { get; set; }

        public Dacs7ReturnCodeException(byte returnCode, int itemNumber = -1) :
            base($"No success return code {returnCode}: <{(itemNumber != -1 ? ($" for item {itemNumber}") : "")}>") => ReturnCode = returnCode;

        public Dacs7ReturnCodeException()
        {
        }

        public Dacs7ReturnCodeException(string message) : base(message)
        {
        }

        public Dacs7ReturnCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected Dacs7ReturnCodeException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            ReturnCode = (byte)serializationInfo.GetValue("ReturnCode", typeof(byte));
            ItemNumber = (int)serializationInfo.GetValue("ItemNumber", typeof(int));
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ReturnCode", ReturnCode);
            info.AddValue("ItemNumber", ItemNumber);
        }
    }
}
