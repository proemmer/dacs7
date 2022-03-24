// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

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
            S7AlarmIndicationDatagram current = new()
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data),
            };

            current.AlarmMessage = S7AlarmMessage.TranslateFromMemory(current.UserData.Data.Data, (AlarmMessageType)current.UserData.Parameter.SubFunction);

            return current;
        }
    }
}