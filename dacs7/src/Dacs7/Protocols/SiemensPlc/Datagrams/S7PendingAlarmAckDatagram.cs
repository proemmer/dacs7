using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    internal class S7PendingAlarmAckDatagram
    {
        public S7UserDataDatagram UserData { get; set; }

        public static S7PendingAlarmAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var current = new S7PendingAlarmAckDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };
            return current;
        }


        public static List<S7PlcAlarmItemDatagram> TranslateFromSslData(ReadOnlySequence<byte> sequence)
        {
            var functionCode = sequence.Slice(0);

            // TODO: find out how we can work with ReadOnlySequence<byte>  !!!!!!

            //              result.BlockType = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            //result.LengthOfInfo = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            //result.Unknown1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            //result.Const1 = span[offset++];
            //result.Const2 = span[offset++];

            return new List<S7PlcAlarmItemDatagram>();
        }


        public static DateTime GetDt(Span<byte> b)
        {
            var dt = new DateTime(1984, 1, 1, 0, 0, 0, 0);
            dt = dt.AddMilliseconds(BinaryPrimitives.ReadUInt32BigEndian(b));
            dt = dt.AddDays(BinaryPrimitives.ReadUInt16BigEndian(b.Slice(4)));
            return dt;
        }
    }
}
