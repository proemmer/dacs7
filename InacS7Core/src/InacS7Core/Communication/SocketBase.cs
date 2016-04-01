using InacS7Core.Arch;
using InacS7Core.Heper;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace InacS7Core.Communication
{
    /// <summary>
    /// Loopback enabling for debugging:
    /// http://teamjohnston.net/blog/2013/07/08/windows-8-apps-streamsocket-connection-to-localhost/
    /// 
    /// 
    /// https://msdn.microsoft.com/library/windows/apps/windows.networking.sockets.aspx
    /// </summary>
    public abstract class SocketBase : IEventPublisher
    {
        public delegate void OnConnectionStateChangedHandler(string socketHandle, bool connected);
        public delegate void OnSendFinishedHandler(string socketHandle);
        public delegate void OnDataReceivedHandler(string socketHandle, IEnumerable<byte> aBuffer);
        public delegate void OnSocketShutdownHandler(string socketHandle);

        #region Fields
        private readonly IEventPublisher eventPublisher = new EventPublisher();
        protected ISocketConfiguration _configuration;
        private string _cycleId = Guid.NewGuid().ToString();
        private bool _shutdown;
        protected string _identity;
        #endregion

        #region Properties
        public string CycleId { get { return _cycleId; } }
        public bool IsConnected { get; protected set; }
        public int ReceiveBufferSize { get { return _configuration.ReceiveBufferSize; } }
        public event OnConnectionStateChangedHandler OnConnectionStateChanged;
        public event OnDataReceivedHandler OnRawDataReceived;
        public event OnSendFinishedHandler OnSendFinished;
        public event OnSocketShutdownHandler OnSocketShutdown;
        public event PublisherEventHandlerDelegate PublisherEvent;

        public abstract string Identity { get; }
        #endregion

        public SocketBase(ISocketConfiguration configuration)
        {
            _configuration = configuration;
            if (configuration.Autoconnect)
            {
                CyclicExecutor.Instance.Add(_cycleId, _cycleId, 5000, () =>
                {
                    CyclicExecutor.Instance.Enabled(_cycleId, false);
                    if(!IsConnected)
                        Open();
                });
            }
        }

        /// <summary>
        /// Initializes the server by preallocating reusable buffers and 
        //  context objects.  These objects do not need to be preallocated 
        /// or reused, but it is done this way to illustrate how the API can 
        /// easily be used to create reusable objects to increase server performance.
        /// </summary>
        public void Init()
        {

        }

        public abstract bool Open();

        public virtual void Close()
        {
            _shutdown = true;
        }

        public abstract Task<SocketError> Send(IEnumerable<byte> data);

        //protected NetworkAdapter GetAdapterByName(string name)
        //{
        //    var host = NetworkInformation.GetHostNames().FirstOrDefault(x => x.IPInformation != null && x.IPInformation.NetworkAdapter.NetworkAdapterId.ToString() == name);
        //    if (host != null)
        //        return host.IPInformation.NetworkAdapter;
        //    throw new ArgumentException(string.Format("Adapter name {0} not found!", name));
        //}

        //protected void SetTcpKeepAlive(Socket targetSocket)
        //{
        //    /* the native structure
        //    struct tcp_keepalive {
        //    ULONG onoff;
        //    ULONG keepalivetime;
        //    ULONG keepaliveinterval;
        //    };
        //    */

        //    if (KeepAliveTime == 0)
        //        return;

        //    // marshal the equivalent of the native structure into a byte array
        //    var dummy = KeepAliveTime != 0 ? 1u : 0;
        //    var size = Marshal.SizeOf(dummy);
        //    var inOptionValues = new byte[size * 3];
        //    BitConverter.GetBytes(dummy).CopyTo(inOptionValues, 0);
        //    BitConverter.GetBytes(KeepAliveTime).CopyTo(inOptionValues, size);
        //    BitConverter.GetBytes(KeepAliveInterval).CopyTo(inOptionValues, size * 2);

        //    // write SIO_VALS to Socket IOControl
        //    targetSocket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        //}

        protected void HandleSocketDown()
        {
            PublishConnectionStateChanged(false);
            if (_shutdown && _configuration.Autoconnect)
                CyclicExecutor.Instance.Enabled(CycleId, true);
        }

        public int GetSubscriberCount()
        {
            return eventPublisher.GetSubscriberCount();
        }

        public bool Subscribe(IEventSubscriber subscriber)
        {
            return eventPublisher.Subscribe(subscriber);
        }

        public bool Unsubscribe(IEventSubscriber subscriber)
        {
            return eventPublisher.Unsubscribe(subscriber);
        }

        private void NotifySubscribers(IEventPublisher source, Event evt)
        {
            eventPublisher.NotifySubscribers(source, evt);
        }

        protected void PublishSocketShutdown(string identity = null)
        {
            if (OnSocketShutdown != null)
                OnSocketShutdown(identity ?? Identity);
            else
                NotifySubscribers(this, new Event(Event.EventCode.Shutdown, identity ?? Identity, null));
        }

        protected void PublishSendFinished(string identity = null)
        {
            if (OnSendFinished != null)
                OnSendFinished(identity ?? Identity);
            else
                NotifySubscribers(this, new Event(Event.EventCode.SendFinished, identity ?? Identity, null));
        }

        protected void PublishConnectionStateChanged(bool state, string identity = null)
        {
            IsConnected = state;
            if (OnConnectionStateChanged != null)
                OnConnectionStateChanged(identity ?? Identity, IsConnected);
            else
                NotifySubscribers(this, new Event(Event.EventCode.ConnectionChanged, identity ?? Identity, IsConnected));
        }

        protected void PublishDataReceived(IEnumerable<byte> receivedData, string identity = null)
        {
            if (OnRawDataReceived != null)
                OnRawDataReceived(identity ?? Identity, receivedData);
            else
                NotifySubscribers(this, new Event(Event.EventCode.DataReceived, identity ?? Identity, receivedData));
        }

        void IEventPublisher.NotifySubscribers(IEventPublisher source, Event evt)
        {
            throw new NotImplementedException();
        }
    }
}
