using System;

namespace Dacs7.Arch
{
    public interface IPlcAlarm
    {
        int Id { get; }
        uint MsgNumber { get; }
        byte[] AssotiatedValue { get; }
        int CountAlarms { get; }
        //Update is Coming update
        bool IsComing { get; }

        //Update is Ack update
        bool IsAck { get; }

        //Ack has been set to true
        bool Ack { get; }
        int AlarmSource { get; }
        DateTime Timestamp { get; }
    }
}