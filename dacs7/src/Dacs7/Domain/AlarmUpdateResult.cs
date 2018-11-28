using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.Alarms
{
    public class AlarmUpdateResult : IDisposable
    {
        private readonly Func<Task> _closeAction;

        public AlarmUpdateResult(bool channelCompleted, Func<Task> closeAction) : this(channelCompleted, null, closeAction)
        {
            ChannelClosed = channelCompleted;
        }

        public AlarmUpdateResult(bool channelCompleted, IEnumerable<IPlcAlarm> alarms, Func<Task> closeAction)
        {
            ChannelClosed = channelCompleted;
            Alarms = alarms;
            _closeAction = closeAction;
        }

        public bool HasAlarms => Alarms != null;
        public IEnumerable<IPlcAlarm> Alarms { get; }
        public bool ChannelClosed { get; }

        public Task CloseUpdateChannel () => _closeAction?.Invoke();

        public void Dispose()
        {
            CloseUpdateChannel().ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
        }
    }
}