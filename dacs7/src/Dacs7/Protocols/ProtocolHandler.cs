using Dacs7.Communication;
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
    internal struct CallbackHandler<T>
    {
        public ushort Id { get; }
        public AsyncAutoResetEvent<T> Event { get; }

        public CallbackHandler(ushort id)
        {
            Id = id;
            Event = new AsyncAutoResetEvent<T>();
        }

    }

    public enum ConnectionState
    {
        Closed,
        PendingOpenRfc1006,
        Rfc1006Opened,
        PendingOpenPlc,
        Opened
    }


    public class ProtocolHandler
    {
        private ConnectionState _connectionState = ConnectionState.Closed;
        private ClientSocket _socket;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private AsyncAutoResetEvent<bool> _connectEvent = new AsyncAutoResetEvent<bool>();

        private SemaphoreSlim _concurrentJobs;

        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>> _readHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>>();
        private ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7ItemDataWriteResult>>> _writeHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7ItemDataWriteResult>>>();

        private int _referenceId;
        private readonly object _idLock = new object();
        private int _timeout = 5000;

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

        public async Task<IEnumerable<object>> ReadAsync(IEnumerable<ReadItemSpecification> vars)
        {
            var id = GetNextReferenceId();
            CallbackHandler<IEnumerable<S7DataItemSpecification>> cbh;
            SocketError errorCode = SocketError.NoData;


            if(_s7Context.OptimizeReadAccess && _concurrentJobs.CurrentCount == 0)
            {
                // TODO: optimization if we have no semaphore we can collect the read data and send them in a single request
            }


            var sendData = DataTransferDatagram.TranslateToMemory(
                                DataTransferDatagram.Build(_context,
                                        S7ReadJobDatagram.TranslateToMemory(
                                            S7ReadJobDatagram.BuildRead(_s7Context, id, vars))).FirstOrDefault());
            

            await _concurrentJobs.WaitAsync();
            try
            {
                cbh = new CallbackHandler<IEnumerable<S7DataItemSpecification>>(id);
                _readHandler.TryAdd(cbh.Id, cbh);
                errorCode = await _socket.SendAsync(sendData);
            }
            finally
            {
                _concurrentJobs.Release();
            }


            if (errorCode != SocketError.Success)
                return new List<object>();

            try
            {
                var result = await cbh.Event.WaitAsync(_timeout);
                return result.Select(x => x.Data.ToArray());
            }
            catch(TaskCanceledException)
            {
                throw new TimeoutException();
            }


        }

        public async Task<IEnumerable<byte>> WriteAsync(IEnumerable<WriteItemSpecification> vars)
        {
            var id = GetNextReferenceId();
            CallbackHandler<IEnumerable<S7ItemDataWriteResult>> cbh;
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
                cbh = new CallbackHandler<IEnumerable<S7ItemDataWriteResult>>(id);
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
            if (buffer.Length > _context.MinimumBufferSize)
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
            _connectionState = ConnectionState.Opened;
            _concurrentJobs = new SemaphoreSlim(_s7Context.MaxParallelJobs);
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
