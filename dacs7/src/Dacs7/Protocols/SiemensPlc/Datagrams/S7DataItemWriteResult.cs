// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7DataItemWriteResult
    {
        public byte ReturnCode { get; set; }


        public int GetSpecificationLength() => 1;



        public static Memory<byte> TranslateToMemory(S7DataItemWriteResult datagram, Memory<byte> memory)
        {
            var result = memory.IsEmpty ? new Memory<byte>(new byte[1]) : memory;  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.ReturnCode;

            return result;
        }

        public static S7DataItemWriteResult TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7DataItemWriteResult
            {
                ReturnCode = span[0]
            };
            return result;
        }
    }
}
