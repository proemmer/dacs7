using Dacs7.Heper;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Communication
{
    /// <summary>
    /// </summary>
    public abstract class SocketBase
    {
        public delegate void OnConnectionStateChangedHandler(string socketHandle, bool connected);
        public delegate void OnSendFinishedHandler(string socketHandle);
        public delegate void OnDataReceivedHandler(string socketHandle, Memory<byte> aBuffer);
        public delegate void OnSocketShutdownHandler(string socketHandle);

        #region Fields
        private readonly IEventPublisher eventPublisher = new EventPublisher();
        protected ISocketConfiguration _configuration;
        private bool _shutdown;
        protected string _identity;
        #endregion

        #region Properties
        public string CycleId { get; } = Guid.NewGuid().ToString();
        public bool IsConnected { get; protected set; }
        public bool Shutdown => _shutdown;
        public int ReceiveBufferSize { get { return _configuration.ReceiveBufferSize; } }


        public OnConnectionStateChangedHandler OnConnectionStateChanged;
        public OnDataReceivedHandler OnRawDataReceived;
        public OnSendFinishedHandler OnSendFinished;
        public OnSocketShutdownHandler OnSocketShutdown;

        public abstract string Identity { get; }
        #endregion

        public SocketBase(ISocketConfiguration configuration)
        {
            _configuration = configuration;
            if (configuration.Autoconnect)
            {
                CyclicExecutor.Instance.Add(CycleId, CycleId, 5000, () =>
                {
                    CyclicExecutor.Instance.Enabled(CycleId, false);
                    if(!IsConnected && !_shutdown)
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

        protected void HandleSocketDown()
        {
            PublishConnectionStateChanged(false);
            if (_shutdown && _configuration.Autoconnect)
                CyclicExecutor.Instance.Enabled(CycleId, true);
        }

        protected void PublishSocketShutdown(string identity = null)
        {
            OnSocketShutdown?.Invoke(identity ?? Identity);
        }

        protected void PublishSendFinished(string identity = null)
        {
            OnSendFinished?.Invoke(identity ?? Identity);
        }

        protected void PublishConnectionStateChanged(bool state, string identity = null)
        {
            IsConnected = state;
            OnConnectionStateChanged?.Invoke(identity ?? Identity, IsConnected);
        }

        protected void PublishDataReceived(Memory<byte> receivedData, string identity = null)
        {
            OnRawDataReceived?.Invoke(identity ?? Identity, receivedData);
        }
    }
}
