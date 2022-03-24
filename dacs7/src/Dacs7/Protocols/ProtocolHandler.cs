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
using System.Collections.Generic;
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
        private readonly AsyncAutoResetEvent<bool> _connectEvent = new();
        private readonly ILogger _logger;
        private readonly Action<ProtocolHandler, ConnectionState> _connectionStateChanged;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Action<Socket> _newSocketConnected;
        private readonly IPlcDataProvider _provider;
        private volatile bool _closeCalled;
        private SemaphoreSlim _concurrentJobs;
        private readonly SemaphoreSlim _connectSema = new(1);
        private int _referenceId;
        private bool _isServerConnection;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Closed;

        internal ushort GetNextReferenceId()
        {
            ushort id = unchecked((ushort)Interlocked.Increment(ref _referenceId));
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

            using (await SemaphoreGuard.Async(_connectSema).ConfigureAwait(false))
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
            using (await SemaphoreGuard.Async(_connectSema).ConfigureAwait(false))
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

                if (_connectSema != null)
                {
                    _logger?.LogDebug("Calling dispose for connec semaphore.");
                    _connectSema?.Dispose();
                }
            }

            _disposed = true;
        }

        private Task<bool> DetectAndReceive(Memory<byte> payload)
        {
            if (SiemensPlcProtocolContext.TryDetectDatagramType(payload, out Type s7DatagramType))
            {
                return S7DatagramReceived(s7DatagramType, payload);
            }
            return Task.FromResult(false);
        }

        private async Task<bool> S7DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            if (datagramType == typeof(S7ReadJobAckDatagram))
            {
                ReceivedReadJobAck(buffer);
            }
            else if (datagramType == typeof(S7WriteJobAckDatagram))
            {
                ReceivedWriteJobAck(buffer);
            }
            else if (datagramType == typeof(S7AckDataDatagram))
            {
                ReceivedAck(buffer);
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
            else if (datagramType == typeof(S7AlarmIndicationDatagram))
            {
                ReceivedS7AlarmIndicationDatagram(buffer);
            }
            else if (datagramType == typeof(S7CommSetupDatagram))
            {
                await ReceivedCommunicationSetupJob(buffer).ConfigureAwait(false);
            }
            else if (datagramType == typeof(S7ReadJobDatagram))
            {
                await ReceivedReadJob(buffer).ConfigureAwait(false);
            }
            else if (datagramType == typeof(S7WriteJobDatagram))
            {
                await ReceivedWriteJob(buffer).ConfigureAwait(false);
            }
            else
            {
                return false;
            }
            return true;
        }


        private void ReceivedAck(Memory<byte> buffer)
        {
            S7AckDataDatagram data = S7AckDataDatagram.TranslateFromMemory(buffer);
            if (_readHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<IEnumerable<S7DataItemSpecification>> cbhr))
            {
                ReceivedAckDatagram(data, cbhr, "reading");
            }
            else if (_writeHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<IEnumerable<S7DataItemWriteResult>> cbhw))
            {
                ReceivedAckDatagram(data, cbhw, "writing");
            }
            else if (_blockInfoHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlockInfoAckDatagram> cbhbi))
            {
                ReceivedAckDatagram(data, cbhbi, "determine blockinfo");
            }
            else if (_blocksCountHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlocksCountAckDatagram> cbhbc))
            {
                ReceivedAckDatagram(data, cbhbc, "determine blocks count");
            }
            else if (_blocksOfTypeHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlocksOfTypeAckDatagram> cbhbot))
            {
                ReceivedAckDatagram(data, cbhbot, "determine blocks of type");
            }
            else if (_alarmHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<S7PendingAlarmAckDatagram> cbha))
            {
                ReceivedAckDatagram(data, cbha, "handling alarm");
            }
            else if (_alarmIndicationHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out CallbackHandler<S7AlarmIndicationDatagram> cbhai))
            {
                ReceivedAckDatagram(data, cbha, "handling alarmindication");
            }
        }


        private void ReceivedAckDatagram<T>(S7AckDataDatagram data, CallbackHandler<T> cbh, string action)
        {
            if (data.Error.ErrorClass != 0)
            {
                _logger?.LogError("Error while {action} data for reference {reference}. ErrorClass: {eclass}  ErrorCode:{ecode}", action, data.Header.ProtocolDataUnitReference, data.Error.ErrorClass, data.Error.ErrorCode);
                cbh.Exception = new Dacs7Exception(data.Error.ErrorClass, data.Error.ErrorCode);
            }

            if (cbh.Event != null)
            {
                cbh.Event.Set(default);
            }
            else
            {
                _logger?.LogWarning("No event for read handler found for received read ack reference {reference}", data.Header.ProtocolDataUnitReference);
            }
        }

        private async Task StartS7CommunicationSetup()
        {
            using (System.Buffers.IMemoryOwner<byte> dgmem = S7CommSetupDatagram.TranslateToMemory(S7CommSetupDatagram.Build(_s7Context, GetNextReferenceId()), out int commemLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dgmem.Memory.Slice(0, commemLength), out int sendLength))
                {
                    SocketError result = await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        await UpdateConnectionState(ConnectionState.PendingOpenPlc).ConfigureAwait(false);
                    }
                }
            }
        }

        private void ReceivedCommunicationSetupAck(Memory<byte> buffer)
        {
            S7CommSetupAckDataDatagram data = S7CommSetupAckDataDatagram.TranslateFromMemory(buffer);

            ushort oldSemaCount = _s7Context.MaxAmQCalling;
            _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
            _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalled;
            _s7Context.PduSize = data.Parameter.PduLength;

            UpdateJobsSemaphore(oldSemaCount, _s7Context.MaxAmQCalling);

            _ = UpdateConnectionState(ConnectionState.Opened);
            _connectEvent.Set(true);
        }

        private void UpdateJobsSemaphore(ushort oldSemaCount, ushort newSemaCount)
        {
            SemaphoreSlim oldSema = _concurrentJobs;
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
                if ((state == ConnectionState.TransportOpened && ConnectionState != ConnectionState.PendingOpenTransport) ||
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
            else if (transport.Configuration is ServerSocketConfiguration)
            {
                transport.OnNewSocketConnected = OnNewSocketConnected;
                transport.ConfigureServer(loggerFactory);
                _isServerConnection = true;
            }
        }

    }
}
