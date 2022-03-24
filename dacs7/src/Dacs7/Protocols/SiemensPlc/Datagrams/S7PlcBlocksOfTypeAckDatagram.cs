// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Metadata;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal class S7PlcBlocksOfTypeAckDatagram
    {

        public S7UserDataDatagram UserData { get; set; }

        public ushort TotalLength { get; set; }

        public static S7PlcBlocksOfTypeAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            S7PlcBlocksOfTypeAckDatagram result = new()
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };

            return result;
        }


        public static List<IPlcBlock> TranslateFromSslData(Memory<byte> memory, int size)
        {
            // We do not need the header
            List<IPlcBlock> result = new();
            int offset = 0;
            Span<byte> span = memory.Span;
            while ((offset + 4) < size)
            {
                ushort number = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
                byte flags = span[offset++];
                string lng = PlcBlockInfo.GetLanguage(span[offset++]);

                result.Add(new PlcBlock
                {
                    Number = number,
                    Flags = flags,
                    Language = lng
                });
            }

            return result;
        }


    }
}
