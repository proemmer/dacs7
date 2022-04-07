// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


using Dacs7.Protocols;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Communication
{
    /// <summary>
    /// </summary>
    internal abstract class SocketBase
    {
        public delegate Task OnConnectionStateChangedHandler(string socketHandle, bool connected);
        public delegate Task<int> OnDataReceivedHandler(string socketHandle, Memory<byte> aBuffer);
        public delegate Task OnSocketShutdownHandler(string socketHandle);
        protected readonly IConfiguration _configuration;

        protected readonly ILogger _logger;
        protected bool _disableReconnect;
        protected bool _shutdown;
        protected string _identity;


        public bool IsConnected { get; protected set; }
        public bool Shutdown => _shutdown;
        public int ReceiveBufferSize => _configuration.ReceiveBufferSize;


        public OnConnectionStateChangedHandler OnConnectionStateChanged;
        public OnDataReceivedHandler OnRawDataReceived;
        public OnNewSocketConnected OnNewSocketConnected;

        public abstract string Identity { get; }



        public SocketBase(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public virtual Task OpenAsync()
        {
            _shutdown = false;
            _disableReconnect = true;
            return Task.CompletedTask;
        }

        public virtual Task CloseAsync()
        {
            _shutdown = _disableReconnect = true;
            return Task.CompletedTask;
        }

        public void EnableAutoReconnectReconnect()
        {
            _disableReconnect = _configuration.AutoconnectTime <= 0; // we have a connection, so enable reconnect if configured
        }

        protected abstract Task InternalOpenAsync(bool internalCall = false);

        public abstract Task<SocketError> SendAsync(Memory<byte> data);

        protected virtual Task HandleSocketDown()
        {
            return PublishConnectionStateChanged(false);
        }

        protected Task PublishConnectionStateChanged(bool state, string identity = null)
        {
            if (IsConnected != state)
            {
                IsConnected = state;
                if (OnConnectionStateChanged != null)
                {
                    return OnConnectionStateChanged?.Invoke(identity ?? Identity, IsConnected);
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process the incomimgn data
        /// </summary>
        /// <param name="receivedData"></param>
        /// <param name="identity"></param>
        /// <returns>the processed number of bytes</returns>
        protected Task<int> ProcessData(Memory<byte> receivedData, string identity = null)
        {
            return OnRawDataReceived?.Invoke(identity ?? Identity, receivedData);
        }

        protected virtual async Task HandleReconnectAsync()
        {
            if (!_disableReconnect && _configuration.AutoconnectTime > 0)
            {
                await Task.Delay(_configuration.AutoconnectTime).ConfigureAwait(false);
                if (!_disableReconnect)
                {
                    await InternalOpenAsync(true).ConfigureAwait(false);
                }
            }
        }
    }
}
