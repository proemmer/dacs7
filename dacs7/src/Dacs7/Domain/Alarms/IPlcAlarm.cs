using System;
using System.Collections.Generic;

namespace Dacs7
{

    public enum AlarmMessageType
    {
        Unknown = 0,
        Alarm_Ack = 12,
        Alarm_SQ = 17,
        Alarm_S = 18
    }


    /// <summary>
    /// Represent a alarm of the alarm interface of Step7
    /// </summary>
    public interface IPlcAlarm
    {
        /// <summary>
        /// Id of the alarm
        /// </summary>
        ushort Id { get; }

        /// <summary>
        /// Message number of the alarm
        /// </summary>
        uint MsgNumber { get; }

        /// <summary>
        /// Associate values
        /// </summary>
        List<byte[]> AssociatedValue { get; }

        byte EventState { get; }
        byte State { get; }
        byte AckStateGoing { get; }
        byte AckStateComing { get; }

        bool IsAck { get; set; }

        AlarmMessageType AlarmMessageType { get; set; }

        /// <summary>
        /// Source of the alarm
        /// </summary>
        byte AlarmSource { get; }

        /// <summary>
        /// Timestamp of the alarm
        /// </summary>
        DateTime Timestamp { get; }
    }
}