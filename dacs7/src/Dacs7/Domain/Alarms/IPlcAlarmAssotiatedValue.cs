using System;

namespace Dacs7.Alarms
{
    public interface IPlcAlarmAssotiatedValue
    {
        int Length { get; }
        Memory<byte> Data { get;  }
    }
}