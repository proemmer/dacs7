using Dacs7.Communication;
using Dacs7.Helper;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
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
        private ConnectionState _connectionState = ConnectionState.Closed;
        private ClientSocket _socket;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private AsyncAutoResetEvent<bool> _connectEvent = new AsyncAutoResetEvent<bool>();

        private SemaphoreSlim _concurrentJobs;

        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>> _readHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>>();
        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>> _writeHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>>();

        private int _referenceId;
        private readonly object _idLock = new object();
        private int _timeout = 5000;

        public ConnectionState ConnectionState => _connectionState;

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

        public ProtocolHandler(ClientSocketConfiguration config, Rfc1006ProtocolContext rfcContext, SiemensPlcProtocolContext s7Context)
        {
            _context = rfcContext;
            _s7Context = s7Context;

            _socket = new ClientSocket(config)
            {
                OnRawDataReceived = OnRawDataReceived,
                OnConnectionStateChanged = OnConnectionStateChanged
            };
        }


        public async Task OpenAsync()
        {
            await _socket.OpenAsync();
            if (_socket.IsConnected)
            {
                try
                {
                    if (!await _connectEvent.WaitAsync(_timeout))
                    {
                        throw new Dacs7NotConnectedException();
                    }
                }
                catch (TimeoutException)
                {
                    throw new Dacs7NotConnectedException();
                }
            }
            
            if(!_socket.IsConnected)
            {
                throw new Dacs7NotConnectedException();
            }
        }

        public async Task CloseAsync()
        {
            await _socket.CloseAsync();
        }

        public async Task<IEnumerable<S7DataItemSpecification>> ReadAsync(IEnumerable<ReadItemSpecification> vars)
        {
            if (ConnectionState != ConnectionState.Opened)
                throw new Dacs7NotConnectedException();

            
            CallbackHandler<IEnumerable<S7DataItemSpecification>> cbh;
            SocketError errorCode = SocketError.NoData;

            if(_s7Context.OptimizeReadAccess && _concurrentJobs.CurrentCount == 0)
            {
                // TODO: optimization if we have no semaphore we can collect the read data and send them in a single request
            }

            var result = vars.ToDictionary(x => x, x => null as S7DataItemSpecification);
            foreach (var normalized in CreatePackages(_s7Context, vars))
            {
                var id = GetNextReferenceId();
                var sendData = DataTransferDatagram.TranslateToMemory(
                                    DataTransferDatagram.Build(_context,
                                            S7ReadJobDatagram.TranslateToMemory(
                                                S7ReadJobDatagram.BuildRead(_s7Context, id, normalized.Items))).FirstOrDefault());


                try
                {
                    IEnumerable<S7DataItemSpecification> readResults = null;
                    using (await SemaphoreGuard.Async(_concurrentJobs))
                    {
                        cbh = new CallbackHandler<IEnumerable<S7DataItemSpecification>>(id);
                        _readHandler.TryAdd(cbh.Id, cbh);
                        errorCode = await _socket.SendAsync(sendData);

                        if (errorCode != SocketError.Success)
                            return new List<S7DataItemSpecification>();


                        readResults = await cbh.Event.WaitAsync(_timeout);
                    }

                    if (readResults == null)
                    {
                        throw new TimeoutException();
                    }

                    var items = normalized.Items.GetEnumerator();
                    foreach (var item in readResults)
                    {
                        if (items.MoveNext())
                        {
                            if (items.Current.IsPart)
                            {
                                if (result.TryGetValue(items.Current.Parent, out var parent))
                                {
                                    parent = new S7DataItemSpecification
                                    {
                                        TransportSize = item.TransportSize,
                                        Length = items.Current.Parent.Length,
                                        Data = new byte[items.Current.Parent.Length]
                                    };
                                    result[items.Current.Parent] = parent;
                                }

                                parent.ReturnCode = item.ReturnCode;
                                item.Data.CopyTo(parent.Data.Slice(items.Current.Offset - items.Current.Parent.Offset, items.Current.Length));
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

        private IEnumerable<Package> CreatePackages(SiemensPlcProtocolContext s7Context, IEnumerable<ReadItemSpecification> vars)
        {
            var result = new List<Package>();
            foreach (var item in vars.ToList().OrderByDescending(x => x.Length))
            {
                var currentPackage = result.FirstOrDefault(package => package.TryAdd(item));
                if (currentPackage == null)
                {
                    if (item.Length > s7Context.ReadItemMaxLength)
                    {
                        ushort bytesToRead = item.Length;
                        ushort processed = 0;
                        while (bytesToRead > 0)
                        {
                            var slice = Math.Min(_s7Context.ReadItemMaxLength, bytesToRead);
                            var child = ReadItemSpecification.CreateChild(item, (ushort)(item.Offset + processed), slice);
                            if (slice < _s7Context.ReadItemMaxLength)
                            {
                                currentPackage = result.FirstOrDefault(package => package.TryAdd(child));
                            }

                            if (currentPackage == null)
                            {
                                currentPackage = new Package(s7Context.PduSize);
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
                        currentPackage = new Package(s7Context.PduSize);
                        result.Add(currentPackage);
                        if (!currentPackage.TryAdd(item))
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                if (currentPackage != null)
                {
                    if(currentPackage.Full)
                    {
                        yield return currentPackage.Return();
                    }

                    if(currentPackage.Handled)
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

        public async Task<IEnumerable<byte>> WriteAsync(IEnumerable<WriteItemSpecification> vars)
        {
            if (ConnectionState != ConnectionState.Opened)
                throw new Dacs7NotConnectedException();


            var id = GetNextReferenceId();
            CallbackHandler<IEnumerable<S7DataItemWriteResult>> cbh;
            SocketError errorCode = SocketError.NoData;

            if (_s7Context.OptimizeWriteAccess && _concurrentJobs.CurrentCount == 0)
            {
                // TODO: optimization if we have no semaphore we can collect the write data and send them in a single request
            }

            var sendData = DataTransferDatagram.TranslateToMemory(
                                DataTransferDatagram.Build(_context,
                                        S7WriteJobDatagram.TranslateToMemory(
                                            S7WriteJobDatagram.BuildWrite(_s7Context, id, vars))).FirstOrDefault());

            await _concurrentJobs.WaitAsync();
            try
            {
                cbh = new CallbackHandler<IEnumerable<S7DataItemWriteResult>>(id);
                _writeHandler.TryAdd(cbh.Id, cbh);
                errorCode = await _socket.SendAsync(sendData);
            }
            finally
            {
                _concurrentJobs.Release();
            }


            if (errorCode != SocketError.Success)
                return new List<byte>();


            try
            {
                var result = await cbh.Event.WaitAsync(_timeout);
                return result.Select(x => x.ReturnCode);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException();
            }


        }



        private Task<int> OnRawDataReceived(string socketHandle, Memory<byte> buffer)
        {
            if (buffer.Length > Rfc1006ProtocolContext.MinimumBufferSize)
            {
                if (_context.TryDetectDatagramType(buffer, out var type))
                {
                    return Rfc1006DatagramReceived(type, buffer);
                }
                // unknown datagram
            }
            else
            {
                return Task.FromResult(0); // no data processed, buffer is to short
            }
            return Task.FromResult(1); // move forward

        }

        private Task OnConnectionStateChanged(string socketHandle, bool connected)
        {
            if (_connectionState == ConnectionState.Closed && connected)
            {
                return SendConnectionRequest();
            }
            else if (_connectionState == ConnectionState.Opened && !connected)
            {
                return Closed();
            }
            return Task.CompletedTask;
        }

        private async Task<int> Rfc1006DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            var processed = 0;
            if (datagramType == typeof(ConnectionConfirmedDatagram))
            {
                var res = ConnectionConfirmedDatagram.TranslateFromMemory(buffer, out processed);
                await ReceivedConnectionConfirmed();
            }
            else if (datagramType == typeof(DataTransferDatagram))
            {
                var datagram = DataTransferDatagram.TranslateFromMemory(buffer.Slice(processed), _context, out var needMoreData, out processed);
                if (!needMoreData && _s7Context.TryDetectDatagramType(datagram.Payload, out var s7DatagramType))
                {
                    await SiemensPlcDatagramReceived(s7DatagramType, datagram.Payload);
                }
            }

            return processed;
        }

        private Task SiemensPlcDatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            if (datagramType == typeof(S7CommSetupAckDataDatagram))
            {
                return ReceivedCommunicationSetupAck(buffer);
            }
            else if(datagramType == typeof(S7ReadJobAckDatagram))
            {
                return ReceivedReadJobAck(buffer);
            }
            else if(datagramType == typeof(S7WriteJobAckDatagram))
            {
                return ReceivedWriteJobAck(buffer);
            }
            return Task.CompletedTask;
        }



        private async Task SendConnectionRequest()
        {
            var sendData = ConnectionRequestDatagram.TranslateToMemory(ConnectionRequestDatagram.BuildCr(_context));
            var result = await _socket.SendAsync(sendData);
            if (result == System.Net.Sockets.SocketError.Success)
                _connectionState = ConnectionState.PendingOpenRfc1006;
        }

        private async Task ReceivedConnectionConfirmed()
        {
            _connectionState = ConnectionState.Rfc1006Opened;
            await StartCommunicationSetup();
        }

        private async Task StartCommunicationSetup()
        {
            var sendData = DataTransferDatagram
                                    .TranslateToMemory(
                                        DataTransferDatagram
                                        .Build(_context,
                                            S7CommSetupDatagram
                                            .TranslateToMemory(
                                                S7CommSetupDatagram
                                                .Build(_s7Context)))
                                                    .FirstOrDefault());
            var result = await _socket.SendAsync(sendData);
            if (result == System.Net.Sockets.SocketError.Success)
            {
                _connectionState = ConnectionState.PendingOpenPlc;
            }
        }


            

        private Task ReceivedCommunicationSetupAck(Memory<byte> buffer)
        {
            var data = S7CommSetupAckDataDatagram.TranslateFromMemory(buffer);
            _s7Context.MaxParallelJobs = data.Parameter.MaxAmQCalling;
            _s7Context.PduSize = data.Parameter.PduLength;
            _concurrentJobs = new SemaphoreSlim(_s7Context.MaxParallelJobs);
            _connectionState = ConnectionState.Opened;
            _connectEvent.Set(true);

            return Task.CompletedTask;
        }

        private Task ReceivedReadJobAck(Memory<byte> buffer)
        {
            var data = S7ReadJobAckDatagram.TranslateFromMemory(buffer);

            if(_readHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out var cbh))
            {
                cbh.Event.Set(data.Data);
                _readHandler.TryRemove(cbh.Id, out _);
            }

            return Task.CompletedTask;
        }

        private Task ReceivedWriteJobAck(Memory<byte> buffer)
        {
            var data = S7WriteJobAckDatagram.TranslateFromMemory(buffer);

            if (_writeHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out var cbh))
            {
                cbh.Event.Set(data.Data);
                _writeHandler.TryRemove(cbh.Id, out _);
            }

            return Task.CompletedTask;
        }




        private Task Closed()
        {
            _connectionState = ConnectionState.Closed;
            if(_concurrentJobs != null)
            {
                _concurrentJobs.Dispose();
                _concurrentJobs = null;
            }
            return Task.CompletedTask;
        }

    }
}
