// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7AlarmUpdateAckDatagram
    {
        public S7UserDataDatagram UserData { get; set; }

        public byte SubscribedEvents { get; set; }
        public byte Unknown { get; set; }
        public Memory<byte> Username { get; set; } = new byte[8];
        public byte AlarmType { get; set; }
        public byte FillByte { get; set; }

        public static S7AlarmUpdateAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            Span<byte> span = data.Span;
            S7AlarmUpdateAckDatagram current = new()
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };
            int offset = 0;
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