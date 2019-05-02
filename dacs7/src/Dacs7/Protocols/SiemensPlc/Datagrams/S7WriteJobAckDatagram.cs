// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7WriteJobAckDatagram
    {

        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();


        public byte Function { get; set; } = 0x05; //Write Var


        public byte ItemCount { get; set; } = 0x00;


        public List<S7DataItemWriteResult> Data { get; set; } = new List<S7DataItemWriteResult>();


        public static IMemoryOwner<byte> TranslateToMemory(S7WriteJobAckDatagram datagram, out int memoryLength)
        {
            var result = S7AckDataDatagram.TranslateToMemory(datagram.Header, out memoryLength);
            var mem = result.Memory.Slice(0, memoryLength);
            var span = mem.Span;
            var offset = datagram.Header.Header.GetHeaderSize();
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;


            foreach (var item in datagram.Data)
            {
                S7DataItemWriteResult.TranslateToMemory(item, mem.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            return result;
        }

        public static S7WriteJobAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7WriteJobAckDatagram
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            var offset = result.Header.GetParameterOffset();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (var i = 0; i < result.ItemCount; i++)
            {
                var res = S7DataItemWriteResult.TranslateFromMemory(data.Slice(offset));
                result.Data.Add(res);
                offset += res.GetSpecificationLength();
            }

            return result;
        }
    }
}
