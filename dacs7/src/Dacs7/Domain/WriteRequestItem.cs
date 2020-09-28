// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{
    public class WriteRequestItem
    {
        public WriteRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, ItemDataTransportSize transportSize, Memory<byte> address, Memory<byte> data)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            TransportSize = GetDataTransportSizeFromItemTransportSize(area, transportSize);
            Address = address;
            Data = data;
        }

        internal WriteRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, Memory<byte> address, Memory<byte> data)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            TransportSize = transportSize;
            Address = address;
            Data = data;
        }


        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public ushort NumberOfItems { get; internal set; }
        public int Offset { get; private set; }
        public Memory<byte> Address { get; private set; }
        public Memory<byte> Data { get; private set; }
        public DataTransportSize TransportSize { get; private set; }


        internal static DataTransportSize GetDataTransportSizeFromItemTransportSize(PlcArea area, ItemDataTransportSize t)
        {
            if (area == PlcArea.CT || area == PlcArea.TM)
                return DataTransportSize.OctetString;

            if (t == ItemDataTransportSize.Bit)
                return DataTransportSize.Bit;

            if (t == ItemDataTransportSize.Int)
                return DataTransportSize.Int;

            if (t == ItemDataTransportSize.Real)
                return DataTransportSize.Real;


            return DataTransportSize.Byte;
        }

    }
}
