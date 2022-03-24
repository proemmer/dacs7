// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7ReadJobDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public byte Function { get; set; } = 0x04; //Read Var

        public byte ItemCount { get; set; } //= 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();



        public static S7ReadJobDatagram BuildRead(SiemensPlcProtocolContext context, int id, IEnumerable<ReadItem> vars)
        {
            S7ReadJobDatagram result = new();
            result.Header.ProtocolDataUnitReference = (ushort)id;
            if (vars != null)
            {
                foreach (ReadItem item in vars)
                {
                    result.Items.Add(new S7AddressItemSpecificationDatagram
                    {
                        TransportSize = S7AddressItemSpecificationDatagram.GetTransportSize(item.Area, item.VarType),
                        ItemSpecLength = item.NumberOfItems,
                        DbNumber = item.DbNumber,
                        Area = (byte)item.Area,
                        Offset = item.Offset,
                        Address = S7AddressItemSpecificationDatagram.GetAddress(item.Offset, item.VarType)
                    });
                }
            }
            result.Header.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }





        public static IMemoryOwner<byte> TranslateToMemory(S7ReadJobDatagram datagram, out int memoryLength)
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

            return result;
        }

        public static S7ReadJobDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7ReadJobDatagram result = new()
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

            return result;
        }

    }
}
