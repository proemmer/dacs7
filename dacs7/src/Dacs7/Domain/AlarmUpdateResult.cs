// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.Alarms
{
    public class AlarmUpdateResult : IDisposable
    {
        private readonly Func<Task> _closeAction;
        private bool _disposed;

        public AlarmUpdateResult(bool channelCompleted, Func<Task> closeAction) : this(channelCompleted, null, closeAction) => ChannelClosed = channelCompleted;

        public AlarmUpdateResult(bool channelCompleted, IEnumerable<IPlcAlarm> alarms, Func<Task> closeAction)
        {
            ChannelClosed = channelCompleted;
            Alarms = alarms;
            _closeAction = closeAction;
        }

        public bool HasAlarms => Alarms != null;
        public IEnumerable<IPlcAlarm> Alarms { get; }
        public bool ChannelClosed { get; }

        [Obsolete("This method is obsolet if you use the alarm subscription.")]
        public Task CloseUpdateChannel() => _closeAction?.Invoke();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CloseUpdateChannel().ConfigureAwait(false)
                                    .GetAwaiter()
                                    .GetResult();
            }

            _disposed = true;
        }
    }
}