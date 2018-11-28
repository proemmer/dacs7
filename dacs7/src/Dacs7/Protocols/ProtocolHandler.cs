using Dacs7.Alarms;
using Dacs7.Communication;
using Dacs7.Exceptions;
using Dacs7.Helper;
using Dacs7.Metadata;
using Dacs7.Protocols.Fdl;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{


    internal partial class ProtocolHandler
    {
        private bool _closeCalled;
        private SocketBase _socket;
        private Rfc1006ProtocolContext _RfcContext;
        private FdlProtocolContext _FdlContext;
        private SiemensPlcProtocolContext _s7Context;
        private AsyncAutoResetEvent<bool> _connectEvent = new AsyncAutoResetEvent<bool>();
        private SemaphoreSlim _concurrentJobs;
        private ILogger _logger;
        
        
        
        


        private int _referenceId;
        private readonly object _idLock = new object();
        private Action<ConnectionState> _connectionStateChanged;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Closed;

        internal UInt16 GetNextReferenceId()
        {
            var id = Interlocked.Increment(ref _referenceId);

            if (id < UInt16.MinValue || id > UInt16.MaxValue)
            {
                lock (_idLock)
                {
                    id = Interlocked.Increment(ref _referenceId);
                    if (id < UInt16.MinValue || id > UInt16.MaxValue)
                    {
                        Interlocked.Exchange(ref _referenceId, 0);
                        id = Interlocked.Increment(ref _referenceId);
                    }
                }
            }
            return Convert.ToUInt16(id);

        }

        public ProtocolHandler( Transport transport,
                                SiemensPlcProtocolContext s7Context, 
                                Action<ConnectionState> connectionStateChanged,
                                ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<ProtocolHandler>();
            _s7Context = s7Context;
            _connectionStateChanged = connectionStateChanged;
            _logger?.LogDebug("S7Protocol-Timeout is {0} ms", _s7Context.Timeout);

            if (transport.ProtocolContext is Rfc1006ProtocolContext rfcContext)
            {
                _logger?.LogDebug("Creating clientSocket for RFC1006 protocol!");
                _RfcContext = rfcContext;
                _socket = new ClientSocket(transport.Configuration as ClientSocketConfiguration, loggerFactory)
                {
                    OnRawDataReceived = OnTcpSocketRawDataReceived,
                    OnConnectionStateChanged = OnTcpSocketConnectionStateChanged
                };
                _logger?.LogDebug("ClientSocket created!");
            }
            else if(transport.ProtocolContext is FdlProtocolContext fdlContext)
            {
                _logger?.LogDebug("Creating S7OnlineClient for FDL protocol!");
                _FdlContext = fdlContext;
                _socket = new S7OnlineClient(transport.Configuration as S7OnlineConfiguration, loggerFactory)
                {
                    OnRawDataReceived = OnS7OnlineRawDataReceived,
                    OnConnectionStateChanged = OnS7OnlineConnectionStateChanged
                };
                _logger?.LogDebug("S7OnlineClient created!");
            }
        }


        public async Task OpenAsync()
        {
            try
            {
                _closeCalled = false;
                await _socket.OpenAsync();
                try
                {
                    if (!await _connectEvent.WaitAsync(_s7Context.Timeout * 10))
                    {
                        await CloseAsync();
                        throw new Dacs7NotConnectedException();
                    }
                }
                catch (TimeoutException)
                {
                    await CloseAsync();
                    throw new Dacs7NotConnectedException();
                }
            }
            catch(Dacs7NotConnectedException)
            {
                await CloseAsync();
                throw;
            }
            catch(Exception ex)
            {
                throw new Dacs7NotConnectedException(ex);
            }
        }

        public async Task CloseAsync()
        {
            _closeCalled = true;
            foreach (var item in _writeHandler)
            {
                item.Value.Event?.Set(null);
            }
            foreach (var item in _readHandler)
            {
                item.Value.Event?.Set(null);
            }
            foreach (var item in _blockInfoHandler)
            {
                item.Value.Event?.Set(null);
            }
            foreach (var item in _alarmHandler)
            {
                item.Value.Event?.Set(null);
            }
            if(_alarmUpdateHandler.Id != 0)
            {
                _alarmUpdateHandler.Event?.Set(null);
                await DisableAlarmUpdatesAsync();
            }
            await _socket.CloseAsync();
            await Task.Delay(1); // This ensures that the user can call connect after reconnect. (Otherwise he has so sleep for a while)
        }



        private Memory<byte> BuildForSelectedContext(Memory<byte> buffer)
        {
            if (_RfcContext != null)
            {
                return BuildForTcp(buffer);
            }
            else if (_FdlContext != null)
            {
                return BuildForS7Online(buffer);
            }
            throw new InvalidOperationException();
        }

        private Task S7DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            if (datagramType == typeof(S7CommSetupAckDataDatagram))
            {
                return ReceivedCommunicationSetupAck(buffer);
            }
            else if (datagramType == typeof(S7CommSetupDatagram))
            {
                return ReceivedCommunicationSetupJob(buffer);
            }
            else if (datagramType == typeof(S7ReadJobAckDatagram))
            {
                return ReceivedReadJobAck(buffer);
            }
            else if (datagramType == typeof(S7ReadJobDatagram))
            {
                return ReceivedReadJob(buffer);
            }
            else if (datagramType == typeof(S7WriteJobAckDatagram))
            {
                return ReceivedWriteJobAck(buffer);
            }
            else if (datagramType == typeof(S7PlcBlockInfoAckDatagram))
            {
                return ReceivedS7PlcBlockInfoAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7PendingAlarmAckDatagram))
            {
                return ReceivedS7PendingAlarmsAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7AlarmUpdateAckDatagram))
            {
                return ReceivedS7AlarmUpdateAckDatagram(buffer);
            }
            else if (datagramType == typeof(S7AlarmIndicationDatagram))
            {
                return ReceivedS7AlarmIndicationDatagram(buffer);
            }
            return Task.CompletedTask;
        }


        private async Task StartS7CommunicationSetup()
        {
            var sendData = BuildForSelectedContext(S7CommSetupDatagram
                                            .TranslateToMemory(
                                                S7CommSetupDatagram
                                                .Build(_s7Context, GetNextReferenceId())));
            var result = await _socket.SendAsync(sendData);
            if (result == SocketError.Success)
            {
                UpdateConnectionState(ConnectionState.PendingOpenPlc);
            }
        }

        private async Task ReceivedCommunicationSetupJob(Memory<byte> buffer)
        {
            var data = S7CommSetupDatagram.TranslateFromMemory(buffer);
            var sendData = BuildForSelectedContext(S7CommSetupAckDataDatagram
                                                    .TranslateToMemory(
                                                        S7CommSetupAckDataDatagram
                                                        .BuildFrom(_s7Context, data)));
            var result = await _socket.SendAsync(sendData);
            if (result == SocketError.Success)
            {
                //UpdateConnectionState(ConnectionState.PendingOpenPlc);
                _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
                _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalling;
                _s7Context.PduSize = data.Parameter.PduLength;
                _concurrentJobs = new SemaphoreSlim(_s7Context.MaxAmQCalling);
                UpdateConnectionState(ConnectionState.Opened);
            }
        }

        private Task TransportOpened()
        {
            UpdateConnectionState(ConnectionState.TransportOpened);
            return StartS7CommunicationSetup();
        }

        private Task ReceivedCommunicationSetupAck(Memory<byte> buffer)
        {
            var data = S7CommSetupAckDataDatagram.TranslateFromMemory(buffer);
            _s7Context.MaxAmQCalling = data.Parameter.MaxAmQCalling;
            _s7Context.MaxAmQCalled = data.Parameter.MaxAmQCalled;
            _s7Context.PduSize = data.Parameter.PduLength;
            _concurrentJobs = new SemaphoreSlim(_s7Context.MaxAmQCalling);
            UpdateConnectionState(ConnectionState.Opened);
            _connectEvent.Set(true);

            return Task.CompletedTask;
        }



        private Task Closed()
        {
            UpdateConnectionState(ConnectionState.Closed);

            if (_concurrentJobs != null)
            {
                _concurrentJobs.Dispose();
                _concurrentJobs = null;
            }
            return Task.CompletedTask;
        }

        private void UpdateConnectionState(ConnectionState state)
        {
            if (ConnectionState != state)
            {
                ConnectionState = state;
                _connectionStateChanged?.Invoke(state);
            }
        }



    }
}
