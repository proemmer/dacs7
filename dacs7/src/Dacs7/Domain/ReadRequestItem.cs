// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{
    public class ReadRequestItem : RequestItem
    {
        public ReadRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, ItemDataTransportSize transportSize, Memory<byte> address)
            : base(area, dbNumber, numberOfItems, offset, transportSize, address)
        {
        }

        internal ReadRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, ushort elementSize, Memory<byte> address)
            : base(area, dbNumber, numberOfItems, offset, transportSize, elementSize, address)
        {
        }
    }
}
