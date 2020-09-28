// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{

    public class ReadResultItem : ReadRequestItem
    {

        public ItemResponseRetValue ReturnCode { get; private set; }
        public Memory<byte> Data { get; private set; }

        public ReadResultItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, byte[] address,  ItemResponseRetValue returnCode, Memory<byte> data = default) : base(area, dbNumber, numberOfItems, offset, transportSize, address)
        {
            ReturnCode = returnCode;
            Data = data;
        }

        public ReadResultItem(ReadRequestItem req, ItemResponseRetValue returnCode, Memory<byte> data = default) : base(req.Area, req.DbNumber, req.NumberOfItems, req.Offset, req.TransportSize, req.Address)
        {
            ReturnCode = returnCode;
            Data = data;
        }
    }
}
