// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{

    internal class S7HeaderErrorCodesDatagram
    {
        public byte ErrorClass { get; set; }

        public byte ErrorCode { get; set; }

        public int GetSize()
        {
            return 2;
        }

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
