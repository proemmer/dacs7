// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Communication
{

    internal sealed class ClientSocket : SocketBase, IDisposable
    {
        private System.Net.Sockets.Socket _socket;
        private readonly ClientSocketConfiguration _config;
        private CancellationTokenSource _tokenSource;
        private Task _receivingTask;


        public sealed override string Identity
        {
            get
            {

                if (_identity == null)
                {
                    var socket = _socket;
                    if (socket != null)
                    {
                        var epLocal = socket.LocalEndPoint as IPEndPoint;
                        try
                        {
                            var epRemote = socket.RemoteEndPoint as IPEndPoint;
                            _identity = $"{epLocal.Address}:{epLocal.Port}-{(epRemote != null ? epRemote.Address.ToString() : _config.Hostname)}:{(epRemote != null ? epRemote.Port : _config.ServiceName)}";
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        };
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                return _identity;
            }
        }

        public ClientSocket(ClientSocketConfiguration configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory?.CreateLogger<ClientSocket>()) => _config = configuration;


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
                if (_shutdown || IsConnected) return;
                await DisposeSocketAsync().ConfigureAwait(false);
                _identity = null;
                _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = _configuration.ReceiveBufferSize,
                    NoDelay = true
                };
                _logger?.LogDebug("Socket connecting. ({0}:{1})", _config.Hostname, _config.ServiceName);
                await _socket.ConnectAsync(_config.Hostname, _config.ServiceName).ConfigureAwait(false);
                EnsureConnected();
                _logger?.LogDebug("Socket connected. ({0}:{1})", _config.Hostname, _config.ServiceName);
                if (_config.KeepAlive)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);

                if (internalCall) EnableAutoReconnectReconnect();

                _tokenSource = new CancellationTokenSource();
                _receivingTask = Task.Factory.StartNew(() => StartReceive(), _tokenSource.Token,TaskCreationOptions.LongRunning, TaskScheduler.Default);
                await PublishConnectionStateChanged(true).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisposeSocketAsync();
                await HandleSocketDown().ConfigureAwait(false);
                if (!internalCall) throw;
            }
        }

        public sealed override async Task<SocketError> SendAsync(Memory<byte> data)
        {
            // Write the locally buffered data to the network.
            try
            {
                if (_socket != null)
                {
                    var result = await _socket.SendAsync(new ArraySegment<byte>(data.ToArray()), SocketFlags.None).ConfigureAwait(false);
                    if(result != data.Length)
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

            _socket = null;
            _tokenSource = null;
            _receivingTask = null;
        }

        private async Task StartReceive()
        {
            var connectionInfo = _socket.RemoteEndPoint.ToString();
            _logger?.LogDebug("Socket connection receive loop started. ({0})", connectionInfo);
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(_socket.ReceiveBufferSize);
            var receiveOffset = 0;
            var bufferOffset = 0;
            var span = new Memory<byte>(receiveBuffer);
            try
            {
                while (_socket != null && _tokenSource != null && !_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var maximumReceiveDataSize = _socket.ReceiveBufferSize - receiveOffset;
                        var buffer = new ArraySegment<byte>(receiveBuffer, receiveOffset, maximumReceiveDataSize);
                        var received = await _socket.ReceiveAsync(buffer, SocketFlags.Partial).ConfigureAwait(false);

                        if (received == 0) return;

                        var toProcess = received + (receiveOffset - bufferOffset);
                        var processed = 0;
                        do
                        {
                            var off = bufferOffset + processed;
                            var length = toProcess - processed;
                            var slice = span.Slice(off, length);
                            var proc = await ProcessData(slice).ConfigureAwait(false);
                            if (proc == 0)
                            {
                                if (length > 0)
                                {
                                    receiveOffset += received;
                                    bufferOffset = receiveOffset - (toProcess - processed);
                                }
                                else
                                {
                                    receiveOffset = 0;
                                    bufferOffset = 0;
                                }
                                break;
                            }
                            processed += proc;
                        } while (processed < toProcess);
                    }
                    catch (Exception ex)
                    {
                        if (_socket != null && !_shutdown)
                        {
                            if (_logger?.IsEnabled(LogLevel.Debug) == true)
                            {
                                _logger?.LogError("Socket exception ({0}): {1} - Stacktrace {2}", connectionInfo, ex.Message, ex.StackTrace);
                            }
                            else
                            {
                                _logger?.LogError("Socket exception ({0}): {1}", connectionInfo, ex.Message);
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                _ = HandleSocketDown();
                _logger?.LogDebug("Socket connection receive loop ended. ({0})", connectionInfo);
            }

        }

        protected sealed override Task HandleSocketDown()
        {
            _ = HandleReconnectAsync();
            return PublishConnectionStateChanged(false);
        }

        private void EnsureConnected()
        {
            var blocking = true;

            try
            {
                blocking = _socket.Blocking;
                _socket.Blocking = false;
                _socket.Send(Array.Empty<byte>(), 0, 0);
            }
            catch (SocketException se)
            {
                // 10035 == WSAEWOULDBLOCK
                if (!se.SocketErrorCode.Equals(10035))
                {
                    throw;   //Throw the Exception for handling in OnConnectedToServer
                }
            }

            //restore blocking mode
            _socket.Blocking = blocking;
        }

        public void Dispose() => DisposeSocketAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
