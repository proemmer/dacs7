// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Communication.Socket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Communication
{

    internal sealed class ServerSocket : SocketBase, IDisposable
    {
        private System.Net.Sockets.Socket _socket;
        private readonly ServerSocketConfiguration _config;
        private CancellationTokenSource _tokenSource;
        private Task _receivingTask;
        private volatile bool _unbinding;

        private readonly List<System.Net.Sockets.Socket> _clients = new();


        public sealed override string Identity
        {
            get
            {

                if (_identity == null)
                {
                    System.Net.Sockets.Socket socket = _socket;
                    if (socket != null)
                    {
                        IPEndPoint epLocal = socket.LocalEndPoint as IPEndPoint;
                        try
                        {
                            IPEndPoint epRemote = socket.RemoteEndPoint as IPEndPoint;
                            _identity = $"{epLocal.Address}:{epLocal.Port}-{(epRemote != null ? epRemote.Address.ToString() : _config.Hostname)}:{(epRemote != null ? epRemote.Port : _config.ServiceName)}";
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                return _identity;
            }
        }

        public ServerSocket(ServerSocketConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory?.CreateLogger<ServerSocket>())
        {
            _config = configuration;
        }



        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        /// </summary>
        public sealed override async Task OpenAsync()
        {
            await base.OpenAsync().ConfigureAwait(false);
            await InternalOpenAsync().ConfigureAwait(false);
        }

        protected sealed override async Task InternalOpenAsync(bool internalCall = false)
        {
            try
            {
                if (_shutdown || IsConnected)
                {
                    return;
                }

                await DisposeSocketAsync().ConfigureAwait(false);
                _identity = null;
                _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = _configuration.ReceiveBufferSize,
                    NoDelay = true
                };
                _logger?.LogDebug("Socket connecting. ({0}:{1})", _config.Hostname, _config.ServiceName);

                try
                {
                    IPEndPoint epEndpoint = new(IPAddress.Parse(_config.Hostname), _config.ServiceName);
                    _socket.Bind(epEndpoint);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // TODO
                    // throw new AddressInUseException(e.Message, e);
                }

                _socket.Listen(512);

                _tokenSource = new CancellationTokenSource();
                _receivingTask = Task.Factory.StartNew(() => RunAcceptLoopAsync(), _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                await PublishConnectionStateChanged(true).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisposeSocketAsync();
                await HandleSocketDown().ConfigureAwait(false);
                if (!internalCall)
                {
                    throw;
                }
            }
        }

        public sealed override async Task<SocketError> SendAsync(Memory<byte> data)
        {
            // Write the locally buffered data to the network.
            try
            {
                if (_socket != null)
                {
                    int result = await _socket.SendAsync(new ArraySegment<byte>(data.ToArray()), SocketFlags.None).ConfigureAwait(false);
                    if (result != data.Length)
                    {
                        return SocketError.Fault;
                    }
                }
                else
                {
                    return SocketError.Fault;
                }
            }
            catch (Exception)
            {
                //TODO
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                //{
                //    throw;
                //}
                return SocketError.Fault;
            }
            return SocketError.Success;
        }

        public sealed override async Task CloseAsync()
        {
            await base.CloseAsync().ConfigureAwait(false);
            await DisposeSocketAsync();
        }

        private async ValueTask DisposeSocketAsync()
        {
            _tokenSource?.Cancel();

            if (_socket != null)
            {
                try
                {
                    _unbinding = true;
                    _socket?.Dispose();
                }
                catch (ObjectDisposedException) { }
            }

            if (_tokenSource != null)
            {
                try
                {
                    _tokenSource.Dispose();
                }
                catch (ObjectDisposedException) { }
            }

            if (_receivingTask != null)
            {
                await _receivingTask.ConfigureAwait(false);
            }

            foreach (System.Net.Sockets.Socket client in _clients)
            {
                client.Close();
                client.Dispose();
            }

            _clients.Clear();
            _unbinding = false;
            _socket = null;
            _tokenSource = null;
            _receivingTask = null;
        }

        protected sealed override Task HandleSocketDown()
        {
            _ = HandleReconnectAsync();
            return PublishConnectionStateChanged(false);
        }


        private async Task RunAcceptLoopAsync()
        {
            try
            {
                while (true)
                {

                    try
                    {
                        if (_unbinding || _socket == null)
                        {
                            break;
                        }

                        System.Net.Sockets.Socket acceptSocket = await _socket.AcceptAsync().ConfigureAwait(false);
                        acceptSocket.NoDelay = true;
                        _clients.Add(acceptSocket);
                        if (OnNewSocketConnected != null)
                        {
                            await OnNewSocketConnected.Invoke(acceptSocket).ConfigureAwait(false);
                        }

                    }
                    catch (SocketException) when (!_unbinding)
                    {
                        _logger?.LogDebug($"ConnectionReset: connectionId: (null)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_unbinding)
                {
                    // Means we must be unbinding. Eat the exception.
                }
                else
                {
                    _logger?.LogCritical(ex, $"Unexpected exception in {nameof(ServerSocket)}.{nameof(RunAcceptLoopAsync)}.");
                    // _listenException = ex;

                    // Request shutdown so we can rethrow this exception
                    // in Stop which should be observable.
                    // _appLifetime.StopApplication();
                }
            }
        }


        public void Dispose()
        {
            DisposeSocketAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
