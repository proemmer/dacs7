﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Alarms;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7PendingAlarmAckDatagram
    {
        public S7UserDataDatagram UserData { get; set; }

        public ushort TotalLength { get; set; }


        public static S7PendingAlarmAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            S7PendingAlarmAckDatagram current = new()
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };
            return current;
        }



        public static List<IPlcAlarm> TranslateFromSslData(Memory<byte> memory, int size)
        {
            // We do not need the header
            List<IPlcAlarm> result = new();
            int offset = 6;
            Span<byte> span = memory.Span;
            while (offset < size)
            {
                S7PlcAlarmItemDatagram item = new()
                {
                    Length = span[offset++],
                    TransportSize = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2))
                };
                offset += 2;
                item.AlarmType = span[offset++] == 4 ? AlarmMessageType.AlarmS : AlarmMessageType.Unknown;
                item.MsgNumber = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
                offset += 2; // 2 is correct, we use the offset twice
                item.Id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                offset += 2;

                item.EventState = span[offset++];// 0x00 == going   0x01  == coming
                item.State = span[offset++];// isAck
                item.AckStateGoing = span[offset++];
                item.AckStateComing = span[offset++]; // 0x00 == no ack  0x01  == ack

                if (size >= offset + 12)
                {
                    item.Coming = S7PlcAlarmDetails.ExtractDetails(ref span, ref offset);

                }
                if (size >= offset + 12)
                {
                    item.Going = S7PlcAlarmDetails.ExtractDetails(ref span, ref offset);
                }

                result.Add(item);
            }

            return result;
        }




    }
}
