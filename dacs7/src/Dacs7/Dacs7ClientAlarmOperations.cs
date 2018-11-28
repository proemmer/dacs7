using Dacs7.Alarms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Alarms
{
    public static class Dacs7ClientAlarmExtensions
    {
        public static Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync(this Dacs7Client client)
            => client.ProtocolHandler.ReadPendingAlarmsAsync();

        public static Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(this Dacs7Client client, CancellationToken ct)
            => client.ProtocolHandler.ReceiveAlarmUpdatesAsync(ct);
    }
}
