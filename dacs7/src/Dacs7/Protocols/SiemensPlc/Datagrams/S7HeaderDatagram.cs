// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7HeaderDatagram
    {

        public byte ProtocolId { get; set; } = 0x32;

        public byte PduType { get; set; }

        public ushort RedundancyIdentification { get; set; } = ushort.MinValue;

        public ushort ProtocolDataUnitReference { get; set; } = ushort.MinValue;

        public ushort ParamLength { get; set; }

        public ushort DataLength { get; set; }



        public int GetMemorySize()
        {
            return GetHeaderSize() + ParamLength + DataLength;
        }

        public int GetHeaderSize()
        {
            return 10;
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7HeaderDatagram datagram, out int memoryLength)
        {
            return TranslateToMemory(datagram, -1, out memoryLength);
        }

        public static IMemoryOwner<byte> TranslateToMemory(S7HeaderDatagram datagram, int length, out int memoryLength)
        {
            memoryLength = length == -1 ? datagram.GetMemorySize() : length;
            IMemoryOwner<byte> result = MemoryPool<byte>.Shared.Rent(memoryLength);
            Span<byte> span = result.Memory.Slice(0, memoryLength).Span;

            span[0] = datagram.ProtocolId;
            span[1] = datagram.PduType;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.RedundancyIdentification);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), datagram.ProtocolDataUnitReference);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), datagram.ParamLength);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(8, 2), datagram.DataLength);

            return result;
        }

        public static S7HeaderDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7HeaderDatagram result = new()
            {
                ProtocolId = span[0],
                PduType = span[1],
                RedundancyIdentification = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2)),
                ProtocolDataUnitReference = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2)),
                ParamLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2)),
                DataLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8, 2)),
            };

            return result;
        }
    }
}
