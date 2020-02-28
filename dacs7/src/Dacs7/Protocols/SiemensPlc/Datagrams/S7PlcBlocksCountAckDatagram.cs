// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Metadata;
using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal class S7PlcBlocksCountAckDatagram
    {

        public S7UserDataDatagram UserData { get; set; }

        public PlcBlocksCount Counts { get; set; }


        public static S7PlcBlocksCountAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var result = new S7PlcBlocksCountAckDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
                Counts = new PlcBlocksCount()
            };

            if (result.UserData.Parameter.ParamErrorCode == 0)
            {
                if (result.UserData.Data.ReturnCode == 0xff)
                {
                    var offset = 0;
                    var span = result.UserData.Data.Data.Span;

                    while ((offset + 4) < result.UserData.Data.Data.Length)
                    {
                        if (result.UserData.Data.Data.Span[offset++] == 0x30)
                        {
                            var type = (PlcBlockType)result.UserData.Data.Data.Span[offset++];
                            var value = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;

                            switch (type)
                            {
                                case PlcBlockType.Ob:
                                    result.Counts.Ob = value;
                                    break;
                                case PlcBlockType.Fb:
                                    result.Counts.Fb = value;
                                    break;
                                case PlcBlockType.Fc:
                                    result.Counts.Fc = value;
                                    break;
                                case PlcBlockType.Db:
                                    result.Counts.Db = value;
                                    break;
                                case PlcBlockType.Sdb:
                                    result.Counts.Sdb = value;
                                    break;
                                case PlcBlockType.Sfc:
                                    result.Counts.Sfc = value;
                                    break;
                                case PlcBlockType.Sfb:
                                    result.Counts.Sfb = value;
                                    break;
                                default:
                                    offset++; // unknown
                                    break;
                            }
                        }
                    }

                }
            }
            return result;
        }

    }
}
