namespace Dacs7.Alarms
{

    /// <summary>
    /// Represent a alarm of the alarm interface of Step7
    /// </summary>
    public interface IPlcAlarm
    {
        byte Length { get; }
        ushort TransportSize { get; }
        AlarmMessageType AlarmType { get;}
        uint MsgNumber { get;}
        ushort Id { get; }
        ushort Unknown2 { get; }


        byte EventState { get; }

        byte State { get; set; }

        byte AckStateGoing { get;  }

        byte AckStateComing { get; }


        IPlcAlarmDetails Coming { get;  }
        IPlcAlarmDetails Going { get; }


        bool IsAck { get; }
    }
}