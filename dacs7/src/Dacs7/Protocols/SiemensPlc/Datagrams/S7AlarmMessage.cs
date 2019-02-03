using Dacs7.Alarms;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7AlarmMessage
    {
        private enum SyntaxIds
        {
            AlarmInd = 0x16,
            AlarmAck = 0x19
        }

        public DateTime Timestamp { get; set; }
        public byte FunctionIdentifier { get; set; }
        public byte NumberOfMessages { get; set; }

        public IEnumerable<IPlcAlarm> Alarms { get; set; }

        public static S7AlarmMessage TranslateFromMemory(Memory<byte> data, AlarmMessageType subfunction)
        {
            var span = data.Span;
            var current = new S7AlarmMessage();
            var offset = 0;

            current.Timestamp = GetDt(span.Slice(offset)); offset += 8;
            current.FunctionIdentifier = span[offset++];
            current.NumberOfMessages = span[offset++];

            var alarms = new List<S7PlcAlarmItemDatagram>();

            for (int i = 0; i < current.NumberOfMessages; i++)
            {
                var alarm = new S7PlcAlarmItemDatagram
                {
                    AlarmType = subfunction
                };
                var varspec = span[offset++];
                alarm.Length = span[offset++];

                if (alarm.Length > 0)
                {

                    var syntaxId = span[offset++];

                    switch (syntaxId)
                    {
                        case (byte)SyntaxIds.AlarmInd:
                        case (byte)SyntaxIds.AlarmAck:
                            {

                                var numberOfAssociatedValues = span[offset++];
                                alarm.MsgNumber = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
                                offset += 2; // 2 is correct, we use the offset twice
                                alarm.Id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                                offset += 2;


                                switch (subfunction)
                                {
                                    case AlarmMessageType.Alarm_SQ: // ALARM_SQ
                                    case AlarmMessageType.Alarm_S: // ALARM_S
                                        {
                                            alarm.EventState = span[offset++];// 0x00 == going   0x01  == coming
                                            alarm.State = span[offset++];// isAck
                                            alarm.AckStateGoing = span[offset++];
                                            alarm.AckStateComing = span[offset++]; // 0x00 == no ack  0x01  == ack

                                            var details = new S7PlcAlarmDetails { Timestamp = current.Timestamp };
                                            S7PlcAlarmDetails.ExtractAssotiatedValue(ref span, ref offset, details);

                                            if (alarm.EventState == 0x00)
                                            {
                                                alarm.Going = details;
                                            }
                                            else if(alarm.EventState == 0x01)
                                            {
                                                alarm.Coming = details;
                                            }
                                        }
                                        break;
                                    case AlarmMessageType.Alarm_Ack: // ALARM ack
                                        {
                                            alarm.AckStateGoing = span[offset++];
                                            alarm.AckStateComing = span[offset++]; // 0x00 == no ack  0x01  == ack
                                        }
                                        break;
                                    default:
                                        {
                                            ExceptionThrowHelper.ThrowUnknownAlarmSubfunction(subfunction);
                                            break;
                                        }
                                }
                                break;
                            }
                        default:
                            {
                                ExceptionThrowHelper.ThrowUnknownAlarmSyntax(syntaxId);
                                break;
                            }
                    }
                }

                alarms.Add(alarm);
                
            }
            current.Alarms = alarms;
            return current;
        }


        public static DateTime GetDt(Span<byte> b)
        {
            var str = string.Format("{2:X2}/{1:X2}/{0:X2} {3:X2}:{4:X2}:{5:X2}.{6:X2}{7:X2}", b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]);
            if (DateTime.TryParseExact(str, "dd/MM/yy HH:mm:ss.ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                return parsedDate;
            return DateTime.MinValue;
        }
    }


}