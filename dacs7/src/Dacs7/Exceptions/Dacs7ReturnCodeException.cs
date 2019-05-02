// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7ReturnCodeException : Exception
    {
        public byte ReturnCode { get; private set; }
        public int ItemNumber { get; set; }

        public Dacs7ReturnCodeException(byte returnCode, int itemNumber = -1) :
            base(string.Format($"No success return code {returnCode}: <{(itemNumber != -1 ? string.Format(" for item {0}", itemNumber) : "")}>")) => ReturnCode = returnCode;
    }
}
