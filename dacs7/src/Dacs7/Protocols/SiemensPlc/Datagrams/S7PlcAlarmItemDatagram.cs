﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Alarms;
using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Globalization;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7PlcAlarmItemDatagram : IPlcAlarm
    {
        public byte Length { get; set; }
        public ushort TransportSize { get; set; }
        public AlarmMessageType AlarmType { get; set; }
        public uint MsgNumber { get; set; }
        public ushort Id { get; set; }
        public ushort Unknown2 { get; set; }

        /// <summary>
        /// 0x00 == going   0x01  == coming
        /// </summary>
        public byte EventState { get; set; }

        public byte State { get; set; }

        public byte AckStateGoing { get; set; }

        public byte AckStateComing { get; set; }


        public IPlcAlarmDetails Coming { get; set; }
        public IPlcAlarmDetails Going { get; set; }


        public bool IsAck => AlarmType == AlarmMessageType.AlarmAck;
        public bool IsGoing => EventState == 0x00;
        public bool IsComing => EventState == 0x01;
    }

    public class S7PlcAlarmDetails : IPlcAlarmDetails
    {
        public DateTime Timestamp { get; set; }
        public IPlcAlarmAssotiatedValue AssotiatedValues { get; set; } = new S7PlcAlarmAssotiatedValue();


        internal static IPlcAlarmDetails ExtractDetails(ref Span<byte> span, ref int itemOffset)
        {
            S7PlcAlarmDetails item = new()
            {
                Timestamp = GetDt(span.Slice(itemOffset))
            };
            itemOffset += 8;
            ExtractAssotiatedValue(ref span, ref itemOffset, item);
            return item;
        }

        internal static void ExtractAssotiatedValue(ref Span<byte> span, ref int itemOffset, S7PlcAlarmDetails item)
        {
            S7PlcAlarmAssotiatedValue assotiatedValue = new()
            {
                ReturnCode = span[itemOffset++],
                TransportSize = (DataTransportSize)span[itemOffset++]
            };
            ushort length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(itemOffset, 2)); itemOffset += 2;
            int lengthInByte = assotiatedValue.TransportSize <= DataTransportSize.Int ? length / 8 : length;
            assotiatedValue.Length = lengthInByte;
            assotiatedValue.Data = new byte[lengthInByte];
            span.Slice(itemOffset, lengthInByte).CopyTo(assotiatedValue.Data.Span);
            itemOffset += lengthInByte;
            item.AssotiatedValues = assotiatedValue;
        }

        internal static DateTime GetDt(Span<byte> b)
        {
            string str = string.Format(CultureInfo.InvariantCulture, "{2:X2}/{1:X2}/{0:X2} {3:X2}:{4:X2}:{5:X2}.{6:X2}{7:X2}", b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]);
            if (DateTime.TryParseExact(str, "dd/MM/yy HH:mm:ss.ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }

            return DateTime.MinValue;
        }
    }

    public class S7PlcAlarmAssotiatedValue : IPlcAlarmAssotiatedValue
    {
        public byte ReturnCode { get; set; }
        public DataTransportSize TransportSize { get; set; }
        public int Length { get; set; }
        public Memory<byte> Data { get; set; }
    }
}
