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
        private readonly object _clientsLock = new();


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
                IPAddress bindAddress = await ResolveBindAddressAsync(_config.Hostname).ConfigureAwait(false);
                _socket = new System.Net.Sockets.Socket(bindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = _configuration.ReceiveBufferSize,
                    NoDelay = true
                };

                if (bindAddress.Equals(IPAddress.IPv6Any) && !TryEnableDualMode(_socket))
                {
                    // accept IPv4 clients too, otherwise listening on all interfaces would exclude them
                    _socket.Dispose();
                    bindAddress = IPAddress.Any;
                    _socket = new System.Net.Sockets.Socket(bindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveBufferSize = _configuration.ReceiveBufferSize,
                        NoDelay = true
                    };
                }

                _logger?.LogDebug("Socket binding. ({0}:{1})", bindAddress, _config.ServiceName);

                try
                {
                    IPEndPoint epEndpoint = new(bindAddress, _config.ServiceName);
                    _socket.Bind(epEndpoint);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // Never continue here: listening on an unbound socket silently binds a random port
                    // on every interface on unix, so the server would report a successful start while
                    // no client can reach it on the configured endpoint.
                    ThrowHelper.ThrowAddressAlreadyInUseException(bindAddress.ToString(), _config.ServiceName, e);
                }

                _socket.Listen(512);
                _logger?.LogDebug("Socket listening. ({0})", _socket.LocalEndPoint);

                _tokenSource = new CancellationTokenSource();
                // Unwrap, so awaiting _receivingTask waits for the accept loop itself and not only for its start.
                _receivingTask = Task.Factory.StartNew(() => RunAcceptLoopAsync(), _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
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
                try
                {
                    await _receivingTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { } // accept loop was cancelled before it started
            }

            foreach (System.Net.Sockets.Socket client in TakeAllClients())
            {
                client.Close();
                client.Dispose();
            }

            _unbinding = false;
            _socket = null;
            _tokenSource = null;
            _receivingTask = null;

            // The accept loop has no equivalent to the receive loop of a client socket, so the state has to be
            // published here. Without it the socket stays "connected" after a close and starting the server
            // again would silently do nothing.
            await PublishConnectionStateChanged(false).ConfigureAwait(false);
        }

        protected sealed override Task HandleSocketDown()
        {
            _ = HandleReconnectAsync();
            return PublishConnectionStateChanged(false);
        }


        /// <summary>
        /// Resolves the configured hostname to the address to bind to.
        /// Accepts an IPv4 or IPv6 address, a hostname, and the wildcards "*" and "any"
        /// which bind every interface of the machine.
        /// </summary>
        private async Task<IPAddress> ResolveBindAddressAsync(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return IPAddress.Loopback;
            }

            string value = hostname.Trim();

            if (value == "*" || value.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return System.Net.Sockets.Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }

            if (IPAddress.TryParse(value, out IPAddress parsed))
            {
                return parsed;
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(value).ConfigureAwait(false);
            IPAddress resolved = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                                 ?? Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetworkV6);

            if (resolved == null)
            {
                ThrowHelper.ThrowCouldNotResolveHostname(value);
            }

            _logger?.LogDebug("Hostname {0} resolved to {1}.", value, resolved);
            return resolved;
        }

        private bool TryEnableDualMode(System.Net.Sockets.Socket socket)
        {
            try
            {
                socket.DualMode = true;
                return true;
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is SocketException || ex is PlatformNotSupportedException)
            {
                _logger?.LogDebug(ex, "Dual mode is not supported on this platform, listening on IPv4 only.");
                return false;
            }
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
                        try
                        {
                            acceptSocket.NoDelay = true;
                            if (_config.KeepAlive)
                            {
                                acceptSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                            }

                            if (!TryAddClient(acceptSocket))
                            {
                                _logger?.LogWarning("The maximum of {0} connections is reached, a further connection was rejected.", _config.MaxConnections);
                                acceptSocket.Dispose();
                                continue;
                            }

                            if (OnNewSocketConnected != null)
                            {
                                await OnNewSocketConnected.Invoke(acceptSocket).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex) when (!_unbinding)
                        {
                            // a failing connection must not stop the server from accepting further connections
                            _logger?.LogWarning(ex, "Could not set up accepted connection, the connection will be closed.");
                            RemoveClient(acceptSocket);
                            acceptSocket.Dispose();
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


        /// <summary>
        /// Adds the accepted socket to the connected clients, if the configured maximum is not reached.
        /// </summary>
        private bool TryAddClient(System.Net.Sockets.Socket client)
        {
            lock (_clientsLock)
            {
                _clients.RemoveAll(IsDisposed); // sockets of disconnected clients are already closed
                if (_config.MaxConnections > 0 && _clients.Count >= _config.MaxConnections)
                {
                    return false;
                }

                _clients.Add(client);
                return true;
            }
        }

        private void RemoveClient(System.Net.Sockets.Socket client)
        {
            lock (_clientsLock)
            {
                _clients.Remove(client);
            }
        }

        private List<System.Net.Sockets.Socket> TakeAllClients()
        {
            lock (_clientsLock)
            {
                List<System.Net.Sockets.Socket> clients = new(_clients);
                _clients.Clear();
                return clients;
            }
        }

        private static bool IsDisposed(System.Net.Sockets.Socket socket)
        {
            try
            {
                _ = socket.Available;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            DisposeSocketAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
