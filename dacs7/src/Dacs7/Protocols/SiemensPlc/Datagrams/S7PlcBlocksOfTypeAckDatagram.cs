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
            var result = new S7PlcBlocksOfTypeAckDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };

            return result;
        }


        public static List<IPlcBlock> TranslateFromSslData(Memory<byte> memory, int size)
        {
            // We do not need the header
            var result = new List<IPlcBlock>();
            var offset = 0;
            var span = memory.Span;
            while ((offset + 4) < size)
            {
                var number = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
                var flags = span[offset++];
                var lng = PlcBlockInfo.GetLanguage(span[offset++]);

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
