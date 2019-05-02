// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7ContentException : Exception
    {
        public int ErrorIndex { get; private set; }
        public ItemResponseRetValue ErrorCode { get; private set; }

        public Dacs7ContentException()
        {
        }

        public Dacs7ContentException(string message) : base(message)
        {
        }

        public Dacs7ContentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public Dacs7ContentException(byte errorCode, int itemIndex) :
            this($"No success return code from item {itemIndex}: <{Dacs7Exception.ResolveErrorCode<ItemResponseRetValue>(errorCode)}>")
        {
            ErrorCode = (ItemResponseRetValue)errorCode;
            ErrorIndex = itemIndex;
        }
    }
}
