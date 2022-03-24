// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7WriteJobAckDatagram
    {

        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();


        public byte Function { get; set; } = 0x05; //Write Var


        public byte ItemCount { get; set; } = 0x00;


        public List<S7DataItemWriteResult> Data { get; set; } = new List<S7DataItemWriteResult>();


        public static S7WriteJobAckDatagram Build(SiemensPlcProtocolContext context, int id, IEnumerable<WriteResultItem> vars)
        {
            S7WriteJobAckDatagram result = new();
            result.Header.Header.ProtocolDataUnitReference = (ushort)id;

            if (vars != null)
            {
                result.ItemCount = (byte)vars.Count();
                byte numberOfItems = result.ItemCount;
                foreach (WriteResultItem item in vars)
                {
                    numberOfItems--;
                    result.Data.Add(new S7DataItemWriteResult
                    {
                        ReturnCode = (byte)item.ReturnCode
                    });
                }
            }

            result.Header.Header.ParamLength = 2;
            result.Header.Header.DataLength = (ushort)result.Data.Count;
            result.ItemCount = (byte)result.Data.Count;
            return result;
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7WriteJobAckDatagram datagram, out int memoryLength)
        {
            IMemoryOwner<byte> result = S7AckDataDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            int take = memoryLength - datagram.Header.GetParameterOffset();
            Memory<byte> mem = result.Memory.Slice(datagram.Header.GetParameterOffset(), take);
            Span<byte> span = mem.Span;
            int offset = 0;
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;


            foreach (S7DataItemWriteResult item in datagram.Data)
            {
                S7DataItemWriteResult.TranslateToMemory(item, mem.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            return result;
        }

        public static S7WriteJobAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7WriteJobAckDatagram result = new()
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            int offset = result.Header.GetParameterOffset();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (int i = 0; i < result.ItemCount; i++)
            {
                S7DataItemWriteResult res = S7DataItemWriteResult.TranslateFromMemory(data.Slice(offset));
                result.Data.Add(res);
                offset += res.GetSpecificationLength();
            }

            return result;
        }
    }
}
