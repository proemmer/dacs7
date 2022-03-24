﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7CommSetupParameterDatagram
    {
        public byte Function { get; set; } = 0xF0; //Setup communication
        public byte Reserved { get; set; } = 0x00;

        public ushort MaxAmQCalling { get; set; }

        public ushort MaxAmQCalled { get; set; }

        public ushort PduLength { get; set; }




        public static Memory<byte> TranslateToMemory(S7CommSetupParameterDatagram datagram, Memory<byte> memory)
        {
            Memory<byte> result = memory.IsEmpty ? new Memory<byte>(new byte[2]) : memory;  // check if we could use ArrayBuffer
            Span<byte> span = result.Span;

            span[0] = datagram.Function;
            span[1] = datagram.Reserved;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.MaxAmQCalling);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), datagram.MaxAmQCalled);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), datagram.PduLength);

            return result;
        }

        public static S7CommSetupParameterDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7CommSetupParameterDatagram result = new()
            {
                Function = span[0],
                Reserved = span[1],
                MaxAmQCalling = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2)),
                MaxAmQCalled = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2)),
                PduLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2))
            };

            return result;
        }
    }
}
