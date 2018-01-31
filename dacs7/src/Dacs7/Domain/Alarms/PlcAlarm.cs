using System;
using System.Collections.Generic;
using System.Text;
using Dacs7.Helper;

namespace Dacs7.Alarms
{


    public enum UserDataParameterType : byte
    {
        Push = 0x00,
        Req = 0x04,
        Res = 0x08
    }


    internal class PlcAlarm :  IPlcAlarm
    {
        public AlarmMessageType AlarmMessageType { get; set; }
        public int Id { get; set; }
        public UInt32 MsgNumber { get; set; }
        public List<byte[]> AssociatedValue { get; set; }
        public byte EventState { get; set; }
        public byte State { get; set; }
        public byte AckStateGoing { get; set; }
        public byte AckStateComing { get; set; }

        public byte AlarmSource { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsAck { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - Id {8}: \n\tTimestamp = \t\t{2}, \n\tEventState = \t\t{3:X2}, \n\tState = \t\t{4:X2}, \n\tAckStateGoing = \t{5:X2}, \n\tAckStateComing = \t{6:X2}, \n\tAlarmSource = \t\t{7:X2}, \n\tIsAck = \t\t{9}, \n\tAssVal = \t{1},", MsgNumber, PrintAssociatedValues(), Timestamp, EventState, State, AckStateGoing, AckStateComing, AlarmSource, Id, IsAck);
        }

        public string PrintAssociatedValues()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Number of Associated values: \t{0}", AssociatedValue.Count));
            for (int i = 0; i < AssociatedValue.Count; i++)
            {
                sb.AppendLine(string.Format("\tValue {0} = \tlenght={1}\t0x{2}", i + 1, AssociatedValue[i].Length, AssociatedValue[i].ToHexString("", 0, Int32.MaxValue, false)));
            }
            return sb.ToString();
        }


        internal static List<byte[]> ExtractAssociatedValue(IMessage msg, int alarmindex, int valueindex = -1)
        {
            var result = new List<byte[]>();
            var nosub = false;
            if (valueindex == -1)
            {
                nosub = true;
                valueindex = 0;
            }
            var subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].", alarmindex, valueindex) + "{0}";
            var items = msg.GetAttribute(string.Format(subItemName, "NumberOfAssociatedValues"), (byte)0);

            if (items >= valueindex)
            {
                if (!nosub)
                    subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].AssociatedValue[{2}].", alarmindex, valueindex, valueindex) + "{0}";


                for (int i = 0; i < items; i++)
                {
                    result.Add(msg.GetAttribute(string.Format(subItemName, "AssociatedValue"), new byte[0]));
                }

            }
            return result;
        }

        internal static DateTime ExtractTimestamp(IMessage msg, int alarmindex, int tsIdx = 0)
        {
            var subItemName = $"Alarm[{alarmindex}].ExtendedData[{tsIdx}]." + "{0}";
            return msg.GetAttribute(string.Format(subItemName, "Timestamp"), DateTime.MinValue);
        }



    }
}
