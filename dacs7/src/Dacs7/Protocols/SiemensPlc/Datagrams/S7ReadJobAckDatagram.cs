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

        public byte ItemCount { get; set; } //= 0x00;


        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();


        public static S7ReadJobAckDatagram Build(SiemensPlcProtocolContext context, int id, IEnumerable<ReadResultItem> vars)
        {
            S7ReadJobAckDatagram result = new();
            ushort dataLength = 0;
            result.Header.Header.ProtocolDataUnitReference = (ushort)id;

            if (vars != null)
            {
                result.ItemCount = (byte)vars.Count();
                byte numberOfItems = result.ItemCount;
                foreach (ReadResultItem item in vars)
                {
                    numberOfItems--;
                    var length = item.ReturnCode == ItemResponseRetValue.Success ? item.NumberOfItems : (ushort)0;
                    result.Data.Add(new S7DataItemSpecification
                    {
                        ReturnCode = (byte)item.ReturnCode,
                        TransportSize = item.ReturnCode == ItemResponseRetValue.Success ? (byte)item.TransportSize : (byte)0x0,
                        Length = length,
                        Data = item.Data,
                        FillByte = numberOfItems == 0 || length % 2 == 0 ? Array.Empty<byte>() : new byte[1],
                        ElementSize = item.ElementSize
                    });


                    if ((dataLength % 2) != 0)
                    {
                        dataLength++;
                    }

                    dataLength += (ushort)item.Data.Length;
                }
            }

            result.Header.Header.ParamLength = 2;
            result.Header.Header.DataLength = (ushort)(dataLength + result.Data.Count * 4);
            result.ItemCount = (byte)result.Data.Count;
            return result;
        }


        public static IMemoryOwner<byte> TranslateToMemory(S7ReadJobAckDatagram datagram, out int memoryLength)
        {
            IMemoryOwner<byte> result = S7AckDataDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            int take = memoryLength - datagram.Header.GetParameterOffset();
            Memory<byte> mem = result.Memory.Slice(datagram.Header.GetParameterOffset(), take);
            Span<byte> span = mem.Span;
            int offset = 0;
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;

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

        public static S7ReadJobAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7ReadJobAckDatagram result = new()
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            int offset = result.Header.GetParameterOffset();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

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
