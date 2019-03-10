using Dacs7.Alarms;
using System;

namespace Dacs7.Protocols.SiemensPlc
{ 
    internal sealed class S7AlarmIndicationDatagram
    {
        public S7UserDataDatagram UserData { get; set; }

        public S7AlarmMessage AlarmMessage { get; set; }

        public static S7AlarmIndicationDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var current = new S7AlarmIndicationDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };

            current.AlarmMessage = S7AlarmMessage.TranslateFromMemory(current.UserData.Data.Data, (AlarmMessageType)current.UserData.Parameter.SubFunction);

            return current;
        }
    }
}