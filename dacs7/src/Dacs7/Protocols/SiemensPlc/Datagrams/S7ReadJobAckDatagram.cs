// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7ReadJobAckDatagram
    {
        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();

        public byte Function { get; set; } = 0x04; //Read Var ACK

        public byte ItemCount { get; set; } = 0x00;


        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();


        public static S7ReadJobAckDatagram Build(SiemensPlcProtocolContext context, int id, IEnumerable<ReadResultItem> vars)
        {
            var result = new S7ReadJobAckDatagram();
            ushort dataLength = 0;
            result.Header.Header.ProtocolDataUnitReference = (ushort)id;

            if (vars != null)
            {
                result.ItemCount = (byte)vars.Count();
                var numberOfItems = result.ItemCount;
                foreach (var item in vars)
                {
                    numberOfItems--;
                    result.Data.Add(new S7DataItemSpecification
                    {
                        ReturnCode = (byte)item.ReturnCode,
                        TransportSize = (byte)item.TransportSize,
                        Length = item.NumberOfItems,
                        Data = item.Data,
                        FillByte = numberOfItems == 0 || item.NumberOfItems % 2 == 0 ? Array.Empty<byte>() : new byte[1],
                        ElementSize = 1 // ??
                    });


                    if ((dataLength % 2) != 0)
                        dataLength++;
                    dataLength += (ushort)item.Data.Length;
                }
            }

            result.Header.Header.ParamLength = (ushort)2;
            result.Header.Header.DataLength = (ushort)(dataLength + result.Data.Count * 4);
            result.ItemCount = (byte)result.Data.Count;
            return result;
        }


        public static IMemoryOwner<byte> TranslateToMemory(S7ReadJobAckDatagram datagram, out int memoryLength)
        {
            var result = S7AckDataDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            var take = memoryLength - datagram.Header.GetParameterOffset();
            var mem = result.Memory.Slice(datagram.Header.GetParameterOffset(), take);
            var span = mem.Span;
            var offset = 0;
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;

            foreach (var item in datagram.Data)
            {
                S7DataItemSpecification.TranslateToMemory(item, mem.Slice(offset));
                offset += item.GetSpecificationLength();
                if (offset % 2 != 0) offset++;
            }

            return result;
        }

        public static S7ReadJobAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7ReadJobAckDatagram
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            var offset = result.Header.GetParameterOffset();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (var i = 0; i < result.ItemCount; i++)
            {
                var res = S7DataItemSpecification.TranslateFromMemory(data.Slice(offset));
                result.Data.Add(res);
                offset += res.GetSpecificationLength();
                if (offset % 2 != 0) offset++;
            }

            return result;
        }
    }
}
