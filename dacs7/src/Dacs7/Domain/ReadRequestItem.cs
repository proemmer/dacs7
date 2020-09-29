// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{
    public class ReadRequestItem
    {
        public ReadRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, ItemDataTransportSize transportSize, Memory<byte> address)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            Address = address;
            DetermineTransportAndElementSize(area, transportSize);
        }

        internal ReadRequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, ushort elementSize, Memory<byte> address)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            TransportSize = transportSize;
            Address = address;
            ElementSize = elementSize;
        }


        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public ushort NumberOfItems { get; internal set; }
        public int Offset { get; private set; }
        public ushort ElementSize { get; set; }
        public Memory<byte> Address { get; private set; }
        public DataTransportSize TransportSize { get; private set; }


        private void DetermineTransportAndElementSize(PlcArea area, ItemDataTransportSize t)
        {
            if (area == PlcArea.CT || area == PlcArea.TM)
            {
                TransportSize = DataTransportSize.OctetString;
                ElementSize = 2;
                return;
            }

            switch(t)
            {
                case ItemDataTransportSize.Bit:
                    {
                    TransportSize = DataTransportSize.Bit;
                    ElementSize = 1;
                    }
                    break;
                case ItemDataTransportSize.Byte:
                case ItemDataTransportSize.Char:
                    {
                        TransportSize = DataTransportSize.Byte;
                        ElementSize = 1;
                    }
                    break;
                case ItemDataTransportSize.Word:
                    {
                        TransportSize = DataTransportSize.Byte;
                        ElementSize = 2;
                    }
                    break;
                case ItemDataTransportSize.Int:
                    {
                        TransportSize = DataTransportSize.Int;
                        ElementSize = 2;
                    }
                    break;
                case ItemDataTransportSize.Dword:
                    {
                        TransportSize = DataTransportSize.Byte;
                        ElementSize = 4;
                    }
                    break;
                case ItemDataTransportSize.Dint:
                    {
                        TransportSize = DataTransportSize.Dint;
                        ElementSize = 4;
                    }
                    break;
                case ItemDataTransportSize.Real:
                    {
                        TransportSize = DataTransportSize.Real;
                        ElementSize = 4;
                    }
                    break;
                default:
                    {
                        TransportSize = DataTransportSize.Byte;
                        ElementSize = 1;
                    }
                    break;
            }
        }

    }
}
