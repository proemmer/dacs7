// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7
{
    public abstract class RequestItem
    {
        public PlcArea Area { get; protected set; }
        public ushort DbNumber { get; protected set; }
        public ushort NumberOfItems { get; protected set; }
        public int Offset { get; protected set; }
        public ushort ElementSize { get; protected set; }
        internal Memory<byte> Address { get; set; }
        internal DataTransportSize TransportSize { get; set; }


        public RequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, ItemDataTransportSize transportSize, Memory<byte> address)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            Address = address;
            DetermineTransportAndElementSize(area, transportSize);
        }

        protected RequestItem(PlcArea area, ushort dbNumber, ushort numberOfItems, int offset, DataTransportSize transportSize, ushort elementSize, Memory<byte> address)
        {
            Area = area;
            DbNumber = dbNumber;
            NumberOfItems = numberOfItems;
            Offset = offset;
            TransportSize = transportSize;
            Address = address;
            ElementSize = elementSize;
        }

        public string ToTag()
        {
            string area;
            switch (Area)
            {
                case PlcArea.DB:
                    {
                        area = $"DB{DbNumber}";
                        break;
                    }
                case PlcArea.IB:
                    {
                        area = "I";
                        break;
                    }
                case PlcArea.FB:
                    {
                        area = "M";
                        break;
                    }
                case PlcArea.QB:
                    {
                        area = "Q";
                        break;
                    }
                case PlcArea.TM:
                    {
                        area = "T";
                        break;
                    }
                case PlcArea.CT:
                    {
                        area = "C";
                        break;
                    }
                default: return string.Empty;
            }

            return $"{area}.{Offset},B,{NumberOfItems * ElementSize}";
        }

        protected void DetermineTransportAndElementSize(PlcArea area, ItemDataTransportSize t)
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
