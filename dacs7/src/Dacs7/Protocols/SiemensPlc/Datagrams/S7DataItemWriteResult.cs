// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    internal struct S7DataItemWriteResult
    {
        public byte ReturnCode { get; set; }


        public int GetSpecificationLength()
        {
            return 1;
        }

        public static Memory<byte> TranslateToMemory(S7DataItemWriteResult datagram, Memory<byte> memory)
        {
            Memory<byte> result = memory.IsEmpty ? new Memory<byte>(new byte[1]) : memory;  // normaly the got the memory, to the allocation should not occure
            Span<byte> span = result.Span;

            span[0] = datagram.ReturnCode;

            return result;
        }

        public static S7DataItemWriteResult TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7DataItemWriteResult result = new()
            {
                ReturnCode = span[0]
            };
            return result;
        }
    }
}
