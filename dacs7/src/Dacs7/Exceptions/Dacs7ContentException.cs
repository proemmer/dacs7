// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7ContentException : Exception
    {
        public int ErrorIndex { get; private set; }
        public ItemResponseRetValue ErrorCode { get; private set; }

        public Dacs7ContentException(byte errorCode, int itemIndex) :
            base($"No success return code from item {itemIndex}: <{Dacs7Exception.ResolveErrorCode<ItemResponseRetValue>(errorCode)}>")
        {
            ErrorCode = (ItemResponseRetValue)errorCode;
            ErrorIndex = itemIndex;
        }
    }
}
