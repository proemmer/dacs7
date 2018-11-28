using Dacs7.Helper;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal partial class ProtocolHandler
    {
        private ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>> _blockInfoHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>>();

        public async Task<S7PlcBlockInfoAckDatagram> ReadBlockInfoAsync(PlcBlockType type, int blocknumber)
        {
            if (ConnectionState != ConnectionState.Opened)
                throw new Dacs7NotConnectedException();

            var id = GetNextReferenceId();
            var sendData = BuildForSelectedContext(S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlockInfoRequest(_s7Context, id, type, blocknumber)));


            try
            {
                S7PlcBlockInfoAckDatagram blockinfoResult = null;
                using (await SemaphoreGuard.Async(_concurrentJobs))
                {
                    var cbh = new CallbackHandler<S7PlcBlockInfoAckDatagram>(id);
                    _blockInfoHandler.TryAdd(cbh.Id, cbh);
                    try
                    {
                        if (await _socket.SendAsync(sendData) != SocketError.Success)
                            return null;
                        blockinfoResult = await cbh.Event.WaitAsync(_s7Context.Timeout);
                    }
                    finally
                    {
                        _blockInfoHandler.TryRemove(cbh.Id, out _);
                    }
                }

                if (blockinfoResult == null)
                {
                    if (_closeCalled)
                    {
                        throw new Dacs7NotConnectedException();
                    }
                    else
                    {
                        throw new Dacs7ReadTimeoutException(id);
                    }
                }

                return blockinfoResult;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException();
            }
        }



        private Task ReceivedS7PlcBlockInfoAckDatagram(Memory<byte> buffer)
        {
            var data = S7PlcBlockInfoAckDatagram.TranslateFromMemory(buffer);

            if (_blockInfoHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.UserData.Data == null)
                {
                    _logger.LogWarning("No data from blockinfo ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger.LogWarning("No block info handler found for received read ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }

            return Task.CompletedTask;
        }

    }
}
