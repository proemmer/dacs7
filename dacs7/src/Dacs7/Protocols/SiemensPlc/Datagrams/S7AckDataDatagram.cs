﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7AckDataDatagram
    {
        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x03, //Ack_Data - > Should be a marker
        };

        public S7HeaderErrorCodesDatagram Error { get; set; } = new S7HeaderErrorCodesDatagram();





        public int GetParameterOffset()
        {
            return Header.GetHeaderSize() + Error.GetSize();
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7AckDataDatagram datagram, out int memoryLength)
        {
            IMemoryOwner<byte> result = S7HeaderDatagram.TranslateToMemory(datagram.Header, datagram.Header.GetHeaderSize() + datagram.Error.GetSize() + datagram.Header.ParamLength + datagram.Header.DataLength, out memoryLength);
            int take = memoryLength - datagram.Header.GetHeaderSize();
            S7HeaderErrorCodesDatagram.TranslateToMemory(datagram.Error, result.Memory.Slice(datagram.Header.GetHeaderSize(), take));
            return result;
        }

        public static S7AckDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            S7AckDataDatagram result = new()
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data)
            };
            result.Error = S7HeaderErrorCodesDatagram.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));

            return result;
        }
    }
}
