using Dacs7.Communication;
using Dacs7.Exceptions;
using Dacs7.Helper;
using Dacs7.Protocols.Fdl;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
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

        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>> _readHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>>();
        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>> _writeHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>>();

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
            await _socket.CloseAsync();
            await Task.Delay(1); // This ensures that the user can call connect after reconnect. (Otherwise he has so sleep for a while)
        }


        private Memory<byte> BuildForSelectedContext(Memory<byte> buffer)
        {
            if(_RfcContext != null)
            {
                return BuildForTcp(buffer);
            }
            else if(_FdlContext != null)
            {
                return BuildForS7Online(buffer);
            }
            throw new InvalidOperationException();
        }


        public async Task<IEnumerable<S7DataItemSpecification>> ReadAsync(IEnumerable<ReadItem> vars)
        {
            if (ConnectionState != ConnectionState.Opened)
                throw new Dacs7NotConnectedException();

            var result = vars.ToDictionary(x => x, x => null as S7DataItemSpecification);
            foreach (var normalized in CreateReadPackages(_s7Context, vars))
            {
                var id = GetNextReferenceId();
                var sendData = BuildForSelectedContext(S7ReadJobDatagram.TranslateToMemory( S7ReadJobDatagram.BuildRead(_s7Context, id, normalized.Items)));


                try
                {
                    IEnumerable<S7DataItemSpecification> readResults = null;
                    using (await SemaphoreGuard.Async(_concurrentJobs))
                    {
                        var cbh = new CallbackHandler<IEnumerable<S7DataItemSpecification>>(id);
                        _readHandler.TryAdd(cbh.Id, cbh);
                        try
                        {
                            if (await _socket.SendAsync(sendData) != SocketError.Success)
                                return new List<S7DataItemSpecification>();
                            readResults = await cbh.Event.WaitAsync(_s7Context.Timeout);
                        }
                        finally
                        {
                            _readHandler.TryRemove(cbh.Id, out _);
                        }
                    }

                    if (readResults == null)
                    {
                        if (_closeCalled)
                        {
                            throw new Dacs7NotConnectedException();
                        }
                        else
                        {
                            throw new Dacs7TimeoutException();
                        }
                    }

                    var items = normalized.Items.GetEnumerator();
                    foreach (var item in readResults)
                    {
                        if (items.MoveNext())
                        {
                            if (items.Current.IsPart)
                            {
                                if (!result.TryGetValue(items.Current.Parent, out var parent) || parent == null)
                                {
                                    parent = new S7DataItemSpecification
                                    {
                                        TransportSize = item.TransportSize,
                                        Length = items.Current.Parent.NumberOfItems,
                                        Data = new byte[items.Current.Parent.NumberOfItems]
                                    };
                                    result[items.Current.Parent] = parent;
                                }

                                parent.ReturnCode = item.ReturnCode;
                                item.Data.CopyTo(parent.Data.Slice(items.Current.Offset - items.Current.Parent.Offset, items.Current.NumberOfItems));
                            }
                            else
                            {
                                result[items.Current] = item;
                            }

                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException();
                }
            }
            return result.Values;

        }


        public async Task<IEnumerable<ItemResponseRetValue>> WriteAsync(IEnumerable<WriteItem> vars)
        {
            if (ConnectionState != ConnectionState.Opened)
                throw new Dacs7NotConnectedException();

            
            var result = vars.ToDictionary(x => x, x => ItemResponseRetValue.Success);
            foreach (var normalized in CreateWritePackages(_s7Context, vars))
            {
                var id = GetNextReferenceId();
                CallbackHandler<IEnumerable<S7DataItemWriteResult>> cbh;
                var sendData = BuildForSelectedContext(S7WriteJobDatagram.TranslateToMemory(S7WriteJobDatagram.BuildWrite(_s7Context, id, normalized.Items)));
                try
                {
                    IEnumerable<S7DataItemWriteResult> writeResults = null;
                    using (await SemaphoreGuard.Async(_concurrentJobs))
                    {
                        cbh = new CallbackHandler<IEnumerable<S7DataItemWriteResult>>(id);
                        _writeHandler.TryAdd(cbh.Id, cbh);
                        try
                        {
                            if (await _socket.SendAsync(sendData) != SocketError.Success)
                                return new List<ItemResponseRetValue>();
                            writeResults = await cbh.Event.WaitAsync(_s7Context.Timeout);
                        }
                        finally
                        {
                            _writeHandler.TryRemove(cbh.Id, out _);
                        }
                    }

                    if (writeResults == null)
                    {
                        if (_closeCalled)
                        {
                            throw new Dacs7NotConnectedException();
                        }
                        else
                        {
                            throw new Dacs7TimeoutException();
                        }
                    }

                    var items = normalized.Items.GetEnumerator();
                    foreach (var item in writeResults)
                    {
                        if (items.MoveNext())
                        {
                            if (items.Current.IsPart)
                            {
                                if (result.TryGetValue(items.Current.Parent, out var retCode) && retCode == ItemResponseRetValue.Success)
                                {
                                    result[items.Current.Parent] = (ItemResponseRetValue)item.ReturnCode;
                                }
                            }
                            else
                            {
                                result[items.Current] = (ItemResponseRetValue)item.ReturnCode;
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException();
                }
            }
            return result.Values;
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
            else if(datagramType == typeof(S7ReadJobAckDatagram))
            {
                return ReceivedReadJobAck(buffer);
            }
            else if (datagramType == typeof(S7ReadJobDatagram))
            {
                return ReceivedReadJob(buffer);
            }
            else if(datagramType == typeof(S7WriteJobAckDatagram))
            {
                return ReceivedWriteJobAck(buffer);
            }
            return Task.CompletedTask;
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

        private Task ReceivedReadJobAck(Memory<byte> buffer)
        {
            var data = S7ReadJobAckDatagram.TranslateFromMemory(buffer);

            if(_readHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out var cbh))
            {
                cbh.Event.Set(data.Data);
            }

            return Task.CompletedTask;
        }

        private Task ReceivedReadJob(Memory<byte> buffer)
        {
            var data = S7ReadJobDatagram.TranslateFromMemory(buffer);

            if (_readHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out var cbh))
            {
                cbh.Event.Set(null);
            }

            return Task.CompletedTask;
        }

        private Task ReceivedWriteJobAck(Memory<byte> buffer)
        {
            var data = S7WriteJobAckDatagram.TranslateFromMemory(buffer);

            if (_writeHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out var cbh))
            {
                cbh.Event.Set(data.Data);
            }

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

        private IEnumerable<ReadPackage> CreateReadPackages(SiemensPlcProtocolContext s7Context, IEnumerable<ReadItem> vars)
        {
            var result = new List<ReadPackage>();
            foreach (var item in vars.ToList().OrderByDescending(x => x.NumberOfItems))
            {
                var currentPackage = result.FirstOrDefault(package => package.TryAdd(item));
                if (currentPackage == null)
                {
                    if (item.NumberOfItems > s7Context.ReadItemMaxLength)
                    {
                        ushort bytesToRead = item.NumberOfItems;
                        ushort processed = 0;
                        while (bytesToRead > 0)
                        {
                            var slice = Math.Min(_s7Context.ReadItemMaxLength, bytesToRead);
                            var child = ReadItem.CreateChild(item, (item.Offset + processed), slice);
                            if (slice < _s7Context.ReadItemMaxLength)
                            {
                                currentPackage = result.FirstOrDefault(package => package.TryAdd(child));
                            }

                            if (currentPackage == null)
                            {
                                currentPackage = new ReadPackage(s7Context.PduSize);
                                if (currentPackage.TryAdd(child))
                                {
                                    if (currentPackage.Full)
                                    {
                                        yield return currentPackage.Return();
                                        if (currentPackage.Handled)
                                        {
                                            currentPackage = null;
                                        }
                                    }
                                    else
                                    {
                                        result.Add(currentPackage);
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                            processed += slice;
                            bytesToRead -= slice;
                        }
                    }
                    else
                    {
                        currentPackage = new ReadPackage(s7Context.PduSize);
                        result.Add(currentPackage);
                        if (!currentPackage.TryAdd(item))
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                if (currentPackage != null)
                {
                    if (currentPackage.Full)
                    {
                        yield return currentPackage.Return();
                    }

                    if (currentPackage.Handled)
                    {
                        result.Remove(currentPackage);
                    }
                }
            }
            foreach (var package in result)
            {
                yield return package.Return();
            }
        }


        private IEnumerable<WritePackage> CreateWritePackages(SiemensPlcProtocolContext s7Context, IEnumerable<WriteItem> vars)
        {
            var result = new List<WritePackage>();
            foreach (var item in vars.ToList().OrderByDescending(x => x.NumberOfItems))
            {
                var currentPackage = result.FirstOrDefault(package => package.TryAdd(item));
                if (currentPackage == null)
                {
                    if (item.NumberOfItems > s7Context.WriteItemMaxLength)
                    {
                        ushort bytesToWrite = item.NumberOfItems;
                        ushort processed = 0;
                        while (bytesToWrite > 0)
                        {
                            var slice = Math.Min(_s7Context.WriteItemMaxLength, bytesToWrite);
                            var child = WriteItem.CreateChild(item, (ushort)(item.Offset + processed), slice);
                            if (slice < _s7Context.WriteItemMaxLength)
                            {
                                currentPackage = result.FirstOrDefault(package => package.TryAdd(child));
                            }

                            if (currentPackage == null)
                            {
                                currentPackage = new WritePackage(s7Context.PduSize);
                                if (currentPackage.TryAdd(child))
                                {
                                    if (currentPackage.Full)
                                    {
                                        yield return currentPackage.Return();
                                        if (currentPackage.Handled)
                                        {
                                            currentPackage = null;
                                        }
                                    }
                                    else
                                    {
                                        result.Add(currentPackage);
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                            processed += slice;
                            bytesToWrite -= slice;
                        }
                    }
                    else
                    {
                        currentPackage = new WritePackage(s7Context.PduSize);
                        result.Add(currentPackage);
                        if (!currentPackage.TryAdd(item))
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                if (currentPackage != null)
                {
                    if (currentPackage.Full)
                    {
                        yield return currentPackage.Return();
                    }

                    if (currentPackage.Handled)
                    {
                        result.Remove(currentPackage);
                    }
                }
            }
            foreach (var package in result)
            {
                yield return package.Return();
            }
        }

    }
}
