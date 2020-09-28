// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{

    public class WriteResultItem : WriteRequestItem
    {
        public ItemResponseRetValue ReturnCode { get; private set; }

        public WriteResultItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, byte[] address,  ItemResponseRetValue returnCode, Memory<byte> data ) : base(area, dbNumber, numberOfItems, offset, transportSize, address, data)
        {
            ReturnCode = returnCode;
        }

        public WriteResultItem(WriteRequestItem req, ItemResponseRetValue returnCode) : base(req.Area, req.DbNumber, req.NumberOfItems, req.Offset, req.TransportSize, req.Address, req.Data)
        {
            ReturnCode = returnCode;
        }
    }
}
