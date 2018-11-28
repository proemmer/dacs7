using System;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7AlarmUpdateAckDatagram
    {
        public S7UserDataDatagram UserData { get; set; }

        public byte SubscribedEvents { get; set; }
        public byte Unknown { get; set; }
        public Memory<byte> Username { get; set; } = new byte[8];
        public byte AlarmType { get; set; }
        public byte FillByte { get; set; }

        public static S7AlarmUpdateAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var current = new S7AlarmUpdateAckDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };
            var offset = 0;
            current.SubscribedEvents = span[offset++];
            current.Unknown = span[offset++];
            data.Slice(offset, current.Username.Length).CopyTo(current.Username);
            offset += current.Username.Length;
            current.AlarmType = span[offset++];
            current.FillByte = span[offset++];

            return current;
        }
    }
}