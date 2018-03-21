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
        public delegate void OnDataReceivedHandler(string socketHandle, Memory<byte> aBuffer);
        public delegate void OnSocketShutdownHandler(string socketHandle);

        #region Fields
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
                        OpenAsync();
                });
            }
        }

        public abstract Task OpenAsync();

        public virtual Task CloseAsync()
        {
            _shutdown = true;
            return Task.CompletedTask;
        }

        public abstract Task<SocketError> SendAsync(Memory<byte> data);

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
