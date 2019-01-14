using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Alarms
{
    public interface IPlcAlarmDetails
    {
        DateTime Timestamp { get;  }
        IPlcAlarmAssotiatedValue AssotiatedValues { get;  } 
    }
}
