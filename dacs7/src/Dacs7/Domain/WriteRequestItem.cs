// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{
    public class WriteRequestItem : RequestItem
    {
        public Memory<byte> Data { get; private set; }

        public WriteRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, ItemDataTransportSize transportSize, Memory<byte> address, Memory<byte> data)
            : base(area, dbNumber, numberOfItems, offset, transportSize, address)
        {
            Data = data;
        }

        internal WriteRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, ushort elementSize, Memory<byte> address, Memory<byte> data)
            : base(area, dbNumber, numberOfItems, offset, transportSize, elementSize, address)
        {
            Data = data;
        }


    }
}
