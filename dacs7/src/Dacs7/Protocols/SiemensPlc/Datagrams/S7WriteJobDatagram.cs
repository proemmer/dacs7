// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7WriteJobDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public byte Function { get; set; } = 0x05; //Write Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();

        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();

        public static S7WriteJobDatagram BuildWrite(SiemensPlcProtocolContext context, int id, IEnumerable<WriteItem> vars)
        {
            int numberOfItems = 0;
            S7WriteJobDatagram result = new();
            result.Header.ProtocolDataUnitReference = (ushort)id;
            if (vars != null)
            {
                foreach (WriteItem item in vars)
                {
                    numberOfItems++;
                    result.Items.Add(new S7AddressItemSpecificationDatagram
                    {
                        TransportSize = S7AddressItemSpecificationDatagram.GetTransportSize(item.Area, item.VarType),
                        ItemSpecLength = item.NumberOfItems,
                        DbNumber = item.DbNumber,
                        Area = (byte)item.Area,
                        Address = S7AddressItemSpecificationDatagram.GetAddress(item.Offset, item.VarType)
                    });
                }

                foreach (WriteItem item in vars)
                {
                    numberOfItems--;
                    result.Data.Add(new S7DataItemSpecification
                    {
                        ReturnCode = 0x00,
                        TransportSize = S7DataItemSpecification.GetTransportSize(item.Area, item.VarType),
                        Length = item.NumberOfItems,
                        Data = item.Data,
                        FillByte = numberOfItems == 0 || item.NumberOfItems % 2 == 0 ? Array.Empty<byte>() : new byte[1],
                        ElementSize = item.ElementSize
                    });
                }
            }
            result.Header.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.Header.DataLength = (ushort)(S7DataItemSpecification.GetDataLength(vars) + result.Items.Count * 4);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }



        public static IMemoryOwner<byte> TranslateToMemory(S7WriteJobDatagram datagram, out int memoryLength)
        {
            IMemoryOwner<byte> result = S7HeaderDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            Memory<byte> mem = result.Memory.Slice(0, memoryLength);
            Span<byte> span = mem.Span;
            int offset = datagram.Header.GetHeaderSize();
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;

            foreach (S7AddressItemSpecificationDatagram item in datagram.Items)
            {
                S7AddressItemSpecificationDatagram.TranslateToMemory(item, mem.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            foreach (S7DataItemSpecification item in datagram.Data)
            {
                S7DataItemSpecification.TranslateToMemory(item, mem.Slice(offset));
                offset += item.GetSpecificationLength();
                if (offset % 2 != 0)
                {
                    offset++;
                }
            }

            return result;
        }

        public static S7WriteJobDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7WriteJobDatagram result = new()
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data),
            };
            int offset = result.Header.GetHeaderSize();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (int i = 0; i < result.ItemCount; i++)
            {
                S7AddressItemSpecificationDatagram res = S7AddressItemSpecificationDatagram.TranslateFromMemory(data.Slice(offset));
                result.Items.Add(res);
                offset += res.GetSpecificationLength();
            }

            for (int i = 0; i < result.ItemCount; i++)
            {
                S7DataItemSpecification res = S7DataItemSpecification.TranslateFromMemory(data.Slice(offset));
                result.Data.Add(res);
                offset += res.GetSpecificationLength();
                if (offset % 2 != 0)
                {
                    offset++;
                }
            }

            return result;
        }
    }
}
