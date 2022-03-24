// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal sealed class S7UserDataParameter
    {
        public byte[] ParamHeader { get; set; } = new byte[] { 0x00, 0x01, 0x12 }; // Always 0x00 0x01 0x12
        public byte ParamDataLength { get; set; } // par len 0x04 or 0x08
        public byte ParameterType { get; set; } // unknown

        public byte TypeAndGroup { get; set; }
        // type and group  (4 bits type and 4 bits group)     0000 ....   = Type: Follow  (0) // .... 0100   = SZL functions (4)

        public byte SubFunction { get; set; } // subfunction
        public byte SequenceNumber { get; set; } // sequence

        public byte DataUnitReferenceNumber { get; set; }
        public byte LastDataUnit { get; set; }
        public ushort ParamErrorCode { get; set; }  // present if plen=0x08 (S7 manager online functions)  -> we do not need this at the moment

        internal int GetParamSize()
        {
            return 4 + ParamDataLength;
        }

        public static Memory<byte> TranslateToMemory(S7UserDataParameter datagram, Memory<byte> memory)
        {
            Memory<byte> result = memory.IsEmpty ? new Memory<byte>(new byte[datagram.ParamDataLength]) : memory;  // check if we could use ArrayBuffer
            Span<byte> span = result.Span;

            datagram.ParamHeader.CopyTo(span.Slice(0, 3));
            span[3] = datagram.ParamDataLength;
            span[4] = datagram.ParameterType;
            span[5] = datagram.TypeAndGroup;
            span[6] = datagram.SubFunction;
            span[7] = datagram.SequenceNumber;

            if (datagram.ParamDataLength == 0x08)
            {
                span[8] = datagram.DataUnitReferenceNumber;
                span[9] = datagram.LastDataUnit;
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(10, 2), datagram.ParamErrorCode);
            }

            return result;
        }

        public static S7UserDataParameter TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7UserDataParameter result = new();
            span.Slice(0, 3).CopyTo(result.ParamHeader);
            result.ParamDataLength = span[3];
            result.ParameterType = span[4];
            result.TypeAndGroup = span[5];
            result.SubFunction = span[6];
            result.SequenceNumber = span[7];
            if (result.ParamDataLength == 0x08)
            {
                result.DataUnitReferenceNumber = span[8];
                result.LastDataUnit = span[9];
                result.ParamErrorCode = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(10, 2));
            }

            return result;
        }

    }
}
