// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Communication;
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

    internal sealed partial class ProtocolHandler : IDisposable
    {
        private bool _disposed;
        private readonly Transport _transport;
        private readonly SiemensPlcProtocolContext _s7Context;
        private readonly AsyncAutoResetEvent<bool> _connectEvent = new AsyncAutoResetEvent<bool>();
        private readonly ILogger _logger;
        private readonly object _idLock = new object();
        private readonly Action<ConnectionState> _connectionStateChanged;

        private bool _closeCalled;
        private SemaphoreSlim _concurrentJobs;
        private int _referenceId;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Closed;

        internal ushort GetNextReferenceId()
        {
            var id = Interlocked.Increment(ref _referenceId);

            if (id < ushort.MinValue || id > ushort.MaxValue)
            {
                lock (_idLock)
                {
                    id = Interlocked.Increment(ref _referenceId);
                    if (id < ushort.MinValue || id > ushort.MaxValue)
                    {
                        Interlocked.Exchange(ref _referenceId, 0);
                        id = Interlocked.Increment(ref _referenceId);
                    }
                }
            }
            return Convert.ToUInt16(id);

        }

        public ProtocolHandler(Transport transport,
                                SiemensPlcProtocolContext s7Context,
                                Action<ConnectionState> connectionStateChanged,
                                ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<ProtocolHandler>();
            _transport = transport;
            _s7Context = s7Context;
            _connectionStateChanged = connectionStateChanged;
            _logger?.LogDebug("S7Protocol-Timeout is {0} ms", _s7Context.Timeout);

            SetupTransport(transport, loggerFactory);

        }


        /// <summary>
        /// Opens the transport cannel and does negotiation.
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync()
        {
            try
            {
                _closeCalled = false;
                await _transport.Client.OpenAsync().ConfigureAwait(false);
                try
                {
                    if (!await _connectEvent.WaitAsync(_s7Context.Timeout * 10).ConfigureAwait(false))
                    {
                        await CloseAsync().ConfigureAwait(false);
                        ThrowHelper.ThrowNotConnectedException();
                    }
                }
                catch (TimeoutException)
                {
                    await CloseAsync().ConfigureAwait(false);
                    ThrowHelper.ThrowNotConnectedException();
                }
            }
            catch (Dacs7NotConnectedException)
            {
                await CloseAsync().ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                ThrowHelper.ThrowNotConnectedException(ex);
            }
        }

        /// <summary>
        /// Close all open wait handlers and the transport channel
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            _closeCalled = true;
            await CanclePendingEvents().ConfigureAwait(false);
            await _transport.Client.CloseAsync().ConfigureAwait(false);
            await Task.Delay(1).ConfigureAwait(false); // This ensures that the user can call connect after reconnect. (Otherwise he has to sleep for a while)
        }

        private async Task CanclePendingEvents()
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
                return;

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
                ReceivedReadJob(buffer);
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
                    var result = await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
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
                                                        .BuildFrom(_s7Context, data), out var memoryLength))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                {
                    var result = await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false);
                    if (result == SocketError.Success)
                    {
                        //UpdateConnectionState(ConnectionState.PendingOpenPlc);
                        _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
                        _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalling;
                        _s7Context.PduSize = data.Parameter.PduLength;

                        if (_concurrentJobs != null) _concurrentJobs.Dispose();
                        _concurrentJobs = new SemaphoreSlim(_s7Context.MaxAmQCalling);

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


            var oldSema = _concurrentJobs;
            if (oldSema == null || oldSemaCount != _s7Context.MaxAmQCalling)
            {
                _concurrentJobs = new SemaphoreSlim(_s7Context.MaxAmQCalling);
                oldSema?.Dispose();
            }

            _ = UpdateConnectionState(ConnectionState.Opened);
            _connectEvent.Set(true);
        }

        private async Task UpdateConnectionState(ConnectionState state)
        {
            if (ConnectionState != state)
            {

                if (state == ConnectionState.TransportOpened)
                {
                    await StartS7CommunicationSetup().ConfigureAwait(false);
                }
                else if (state == ConnectionState.Closed)
                {

                    await CanclePendingEvents().ConfigureAwait(false);

                    if (_concurrentJobs != null)
                    {
                        _concurrentJobs?.Dispose();
                        _concurrentJobs = null;
                    }
                }

                ConnectionState = state;
                _connectionStateChanged?.Invoke(state);
            }
        }

        private void SetupTransport(Transport transport, ILoggerFactory loggerFactory)
        {
            transport.OnUpdateConnectionState = UpdateConnectionState;
            transport.OnDetectAndReceive = DetectAndReceive;
            transport.OnGetConnectionState = () => ConnectionState;
            transport.ConfigureClient(loggerFactory);
        }

    }
}
