using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7PlcAlarmItemDatagram
    {
        public byte AlarmType { get; set; }
        public ushort Id{ get; set; }

        public uint MsgNumber{ get; set; }


        public byte EventState{ get; set; }

        public byte State{ get; set; }

        public byte AckStateGoing{ get; set; }

        public byte AckStateComing{ get; set; }

        public bool IsAck{ get; set; }
        public AlarmMessageType AlarmMessageType { get; set; }

        public byte AlarmSource{ get; set; }

        public List<S7PlcAlarmDetails> Details { get; set; }
    }

    internal class S7PlcAlarmDetails
    {
        public DateTime Timestamp { get; set; }
        public byte SuccessCode { get; set; }
        public DataTransportSize TransportSize { get; set; }
        public int SubItemLength { get; set; }

        public int ValueLength { get; set; }
        public List<Memory<byte>> AssociatedValue { get; set; }
    }
}
