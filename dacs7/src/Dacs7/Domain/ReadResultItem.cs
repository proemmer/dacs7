// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7
{

    public class ReadResultItem : ReadRequestItem
    {

        public ItemResponseRetValue ReturnCode { get; private set; }
        public Memory<byte> Data { get; private set; }

        public ReadResultItem(ReadRequestItem req, ItemResponseRetValue returnCode, Memory<byte> data = default) : base(req.Area, req.DbNumber, req.NumberOfItems, req.Offset, req.TransportSize, req.ElementSize, req.Address)
        {
            ReturnCode = returnCode;
            Data = data;
        }
    }
}
