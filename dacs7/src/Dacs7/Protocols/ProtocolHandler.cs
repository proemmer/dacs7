// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Communication;
using Dacs7.Communication.Socket;
using Dacs7.DataProvider;
using Dacs7.Helper;
using Dacs7.Protocols.SiemensPlc;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal delegate Task OnUpdateConnectionState(ConnectionState state);
    internal delegate Task<bool> OnDetectAndReceive(Memory<byte> payload);
    internal delegate ConnectionState OnGetConnectionState();
    internal delegate Task OnNewSocketConnected(Socket socket);

    internal sealed partial class ProtocolHandler : IDisposable
    {
        private bool _disposed;
        private readonly Transport _transport;
        private readonly SiemensPlcProtocolContext _s7Context;
        private readonly AsyncAutoResetEvent<bool> _connectEvent = new AsyncAutoResetEvent<bool>();
        private readonly ILogger _logger;
        private readonly Action<ProtocolHandler, ConnectionState> _connectionStateChanged;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Action<Socket> _newSocketConnected;
        private readonly IPlcDataProvider _provider;
        private volatile bool _closeCalled;
        private SemaphoreSlim _concurrentJobs;
        private readonly SemaphoreSlim _connectSema = new SemaphoreSlim(1);
        private int _referenceId;
        private bool _isServerConnection;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Closed;

        internal ushort GetNextReferenceId()
        {
            var id = unchecked((ushort)Interlocked.Increment(ref _referenceId));
            if (id <= ushort.MinValue)
            {
                return GetNextReferenceId();
            }
            return id;
        }

        public ProtocolHandler(Transport transport,
                                SiemensPlcProtocolContext s7Context,
                                Action<ProtocolHandler, ConnectionState> connectionStateChanged,
                                ILoggerFactory loggerFactory,
                                Action<Socket> newSocketConnected = null,
                                IPlcDataProvider provider = null)
        {
            _logger = loggerFactory?.CreateLogger<ProtocolHandler>();
            _transport = transport;
            _s7Context = s7Context;
            _connectionStateChanged = connectionStateChanged;
            _loggerFactory = loggerFactory;
            _newSocketConnected = newSocketConnected;
            _provider = provider;
            _logger?.LogDebug("S7Protocol-Timeout is {0} ms", _s7Context.Timeout);

            SetupTransport(transport, loggerFactory);

        }


        /// <summary>
        /// Opens the transport cannel and does negotiation.
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync()
        {
            if (_transport.Connection.IsConnected)
            {
                return; // if connection is already open do nothing
            }

            using (await SemaphoreGuard.Async(_connectSema))
            {
                if (_transport.Connection.IsConnected)
                {
                    return;  // if connection is already open do nothing
                }

                try
                {
                    _closeCalled = false;
                    await _transport.Connection.OpenAsync().ConfigureAwait(false);
                    try
                    {
                        if (!_transport.Connection.IsConnected)
                        {
                            ThrowHelper.ThrowNotConnectedException();
                        }

                        if (!_isServerConnection)
                        {
                            if (!await _connectEvent.WaitAsync(_s7Context.Timeout * 2).ConfigureAwait(false) || !_transport.Connection.IsConnected)
                            {
                                ThrowHelper.ThrowNotConnectedException();
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        await InternalCloseAsync().ConfigureAwait(false);
                        ThrowHelper.ThrowNotConnectedException();
                    }

                    
                    // tcp and rfc1006 connection is open, so enable auto reconnect
                    _transport.Connection.EnableAutoReconnectReconnect();
                }
                catch (Dacs7NotConnectedException)
                {
                    await InternalCloseAsync().ConfigureAwait(false);
                    throw;
                }
                catch (Exception ex)
                {
                    ThrowHelper.ThrowNotConnectedException(ex);
                }
            }
        }

        /// <summary>
        /// Close all open wait handlers and the transport channel
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            using (await SemaphoreGuard.Async(_connectSema))
            {
                await InternalCloseAsync().ConfigureAwait(false);
            }
        }

        private async Task InternalCloseAsync()
        {
            if (!_closeCalled)
            {
                _closeCalled = true;
                await CancelPendingEvents().ConfigureAwait(false);
                await _transport.Connection.CloseAsync().ConfigureAwait(false);
                await Task.Delay(1).ConfigureAwait(false); // This ensures that the user can call connect after reconnect. (Otherwise he has to sleep for a while)   
            }
        }

        private async Task CancelPendingEvents()
        {
            await CancelWriteHandlingAsync().ConfigureAwait(false);
            await CancelReadHandlingAsync().ConfigureAwait(false);
            await CancelMetaDataHandlingAsync().ConfigureAwait(false);
            await CancelAlarmHandlingAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_concurrentJobs != null)
                {
                    _logger?.LogDebug("Calling dispose for concurrent jobs.");
                    _concurrentJobs?.Dispose();
                    _concurrentJobs = null;
                }
            }

            _disposed = true;
        }

        private Task<bool> DetectAndReceive(Memory<byte> payload)
        {
            if (_s7Context.TryDetectDatagramType(payload, out var s7DatagramType))
            {
                S7DatagramReceived(s7DatagramType, payload);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private void S7DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            if (datagramType == typeof(S7ReadJobAckDatagram))
            {
                ReceivedReadJobAck(buffer);
            }
            else if (datagramType == typeof(S7WriteJobAckDatagram))
            {
                ReceivedWriteJobAck(buffer);
            }
            else if (datagramType == typeof(S7PlcBlockInfoAckDatagram))
            {
                ReceivedS7PlcBlockInfoAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7PlcBlocksCountAckDatagram))
            {
                ReceivedS7PlcBlocksCountAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7PlcBlocksOfTypeAckDatagram))
            {
                ReceivedS7PlcBlocksOfTypeAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7PendingAlarmAckDatagram))
            {
                ReceivedS7PendingAlarmsAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7AlarmUpdateAckDatagram))
            {
                ReceivedS7AlarmUpdateAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7CommSetupAckDataDatagram))
            {
                ReceivedCommunicationSetupAck(buffer);
            }
            else if (datagramType == typeof(S7CommSetupDatagram))
            {
                _ = ReceivedCommunicationSetupJob(buffer);
            }
            else if (datagramType == typeof(S7ReadJobDatagram))
            {
                _ = ReceivedReadJob(buffer);
            }
            else if (datagramType == typeof(S7WriteJobDatagram))
            {
                _ = ReceivedWriteJob(buffer);
            }
            else if (datagramType == typeof(S7AlarmIndicationDatagram))
            {
                ReceivedS7AlarmIndicationDatagram(buffer);
            }
        }

        private async Task StartS7CommunicationSetup()
        {
            using (var dgmem = S7CommSetupDatagram.TranslateToMemory(S7CommSetupDatagram.Build(_s7Context, GetNextReferenceId()), out var commemLength))
            {
                using (var sendData = _transport.Build(dgmem.Memory.Slice(0, commemLength), out var sendLength))
                {
                    var result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        await UpdateConnectionState(ConnectionState.PendingOpenPlc).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ReceivedCommunicationSetupJob(Memory<byte> buffer)
        {
            var data = S7CommSetupDatagram.TranslateFromMemory(buffer);
            using (var dg = S7CommSetupAckDataDatagram
                                                    .TranslateToMemory(
                                                        S7CommSetupAckDataDatagram
                                                        .BuildFrom(_s7Context, data, GetNextReferenceId()), out var memoryLength))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                {
                    var result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        var oldSemaCount = _s7Context.MaxAmQCalling;
                        _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
                        _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalled;
                        _s7Context.PduSize = data.Parameter.PduLength;
                        UpdateJobsSemaphore(oldSemaCount, _s7Context.MaxAmQCalling);

                        await UpdateConnectionState(ConnectionState.Opened).ConfigureAwait(false);
                    }
                }
            }
        }

        private void ReceivedCommunicationSetupAck(Memory<byte> buffer)
        {
            var data = S7CommSetupAckDataDatagram.TranslateFromMemory(buffer);

            var oldSemaCount = _s7Context.MaxAmQCalling;
            _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
            _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalled;
            _s7Context.PduSize = data.Parameter.PduLength;

            UpdateJobsSemaphore(oldSemaCount, _s7Context.MaxAmQCalling);

            _ = UpdateConnectionState(ConnectionState.Opened);
            _connectEvent.Set(true);
        }

        private void UpdateJobsSemaphore(ushort oldSemaCount, ushort newSemaCount)
        {
            var oldSema = _concurrentJobs;
            if (oldSema == null || oldSemaCount != newSemaCount)
            {
                _concurrentJobs = new SemaphoreSlim(newSemaCount);
                oldSema?.Dispose();
            }
        }

        private async Task UpdateConnectionState(ConnectionState state)
        {
            if (ConnectionState != state)
            {
                if((state == ConnectionState.TransportOpened && ConnectionState != ConnectionState.PendingOpenTransport) ||
                    state == ConnectionState.PendingOpenPlc && ConnectionState != ConnectionState.TransportOpened)
                {
                    // ignore state change
                    return;
                }

                ConnectionState = state;
                _connectionStateChanged?.Invoke(this, state);


                if (state == ConnectionState.TransportOpened)
                {
                    // start comm setup only if state is in pending open transport
                    await StartS7CommunicationSetup().ConfigureAwait(false);
                    // after this ack received we should be in state PendingOpenPlc
                }
                else if (state == ConnectionState.Closed)
                {
                    await CancelPendingEvents().ConfigureAwait(false);
                }
                
            }
        }

        private Task OnNewSocketConnected(Socket clientSocket)
        {
            _newSocketConnected?.Invoke(clientSocket);
            return Task.CompletedTask;
        }

        private void SetupTransport(Transport transport, ILoggerFactory loggerFactory)
        {
            transport.OnUpdateConnectionState = UpdateConnectionState;
            transport.OnDetectAndReceive = DetectAndReceive;
            transport.OnGetConnectionState = () => ConnectionState;

            if (transport.Configuration is ClientSocketConfiguration)
            {
                transport.OnNewSocketConnected = null;
                transport.ConfigureClient(loggerFactory);
                _isServerConnection = false;
            }
            else if(transport.Configuration is ServerSocketConfiguration)
            {
                transport.OnNewSocketConnected = OnNewSocketConnected;
                transport.ConfigureServer(loggerFactory);
                _isServerConnection = true;
            }
        }

    }
}
