using Dacs7.Protocols;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Concurrent;

namespace Dacs7.Alarms
{

    public class AlarmSubscription : IDisposable
    {
        private bool _disposedValue;
        private readonly ConcurrentQueue<S7AlarmIndicationDatagram> _alarms = new();
        internal ProtocolHandler ProtocolHandler { get; private set; }
        internal CallbackHandler<S7AlarmIndicationDatagram> CallbackHandler { get; private set; }

        internal AlarmSubscription(ProtocolHandler ph, CallbackHandler<S7AlarmIndicationDatagram> cbh)
        {
            ProtocolHandler = ph;
            CallbackHandler = cbh;
        }

        internal bool TryGetDatagram(out S7AlarmIndicationDatagram datagram)
        {
            return _alarms.TryDequeue(out datagram);
        }

        internal void AddDatagram(S7AlarmIndicationDatagram datagram)
        {
            _alarms.Enqueue(datagram);
            CallbackHandler.Event.Set(datagram); // set but ignore the datagram in code 
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _ = ProtocolHandler.RemoveAlarmSubscriptionAsync(this);
                    while (_alarms.TryDequeue(out _))
                    {
                        ; // clear the queue
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
