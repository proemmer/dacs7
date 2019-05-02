// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7HeaderErrorCodesDatagram
    {
        private const int _size = 2;


        public byte ErrorClass { get; set; }

        public byte ErrorCode { get; set; }

        public int GetSize() => _size;

        public static Memory<byte> TranslateToMemory(S7HeaderErrorCodesDatagram datagram, Memory<byte> memory)
        {
            var result = memory.IsEmpty ? new Memory<byte>(new byte[2]) : memory;  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.ErrorClass;
            span[1] = datagram.ErrorCode;

            return result;
        }

        public static S7HeaderErrorCodesDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7HeaderErrorCodesDatagram
            {
                ErrorClass = span[0],
                ErrorCode = span[1]
            };

            return result;
        }
    }
}
