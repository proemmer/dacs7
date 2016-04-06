using System;
using InacS7Core.Arch;
using InacS7Core.Helper;

namespace InacS7Core.Domain
{


    public enum UserDataParameterType : byte
    {
        Push = 0x00,
        Req = 0x04,
        Res = 0x08
    }


    public class PlcAlarm :  IPlcAlarm
    {
        public int Id { get; set; }
        public uint MsgNumber { get; set; }
        public byte[] AssotiatedValue { get; set; }
        public int CountAlarms { get; set; }

        //Update is Coming update
        public bool IsComing { get; set; }

        //Update is Ack update
        public bool IsAck { get; set; }

        //Ack has been set to true
        public bool Ack { get; set; }

        public int AlarmSource { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: AssotiatedValue = {1}, Timestamp = {2}", MsgNumber, AssotiatedValue.ToHexString(),Timestamp);
        }
    }
}
