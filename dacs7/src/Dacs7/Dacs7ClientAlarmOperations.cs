using Dacs7.Alarms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {
        public Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync()
            => _protocolHandler.ReadPendingAlarmsAsync();

        public Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(CancellationToken ct)
            => _protocolHandler.ReceiveAlarmUpdatesAsync(ct);
    }
}
