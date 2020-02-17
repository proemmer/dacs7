// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler 
    {
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>> _blockInfoHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>>();


        public Task CancelMetaDataHandlingAsync()
        {
            try
            {
                foreach (var item in _blockInfoHandler.ToList())
                {
                    item.Value.Event?.Set(null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Exception while canceling meta data handling. Exception was {0}", ex.Message);
            }
            return Task.CompletedTask; 
        }

        public async Task<S7PlcBlockInfoAckDatagram> ReadBlockInfoAsync(PlcBlockType type, int blocknumber)
        {
            if (ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();

            var id = GetNextReferenceId();
            using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlockInfoRequest(_s7Context, id, type, blocknumber), out var memoryLength))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                {
                    try
                    {
                        CallbackHandler<S7PlcBlockInfoAckDatagram> cbh;
                        S7PlcBlockInfoAckDatagram blockinfoResult = null;
                        using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                        {
                            cbh = new CallbackHandler<S7PlcBlockInfoAckDatagram>(id);
                            _blockInfoHandler.TryAdd(cbh.Id, cbh);
                            try
                            {
                                if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                    return null;
                                blockinfoResult = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                            }
                            finally
                            {
                                _blockInfoHandler.TryRemove(cbh.Id, out _);
                            }
                        }

                        HandlerErrorResult(id, cbh, blockinfoResult);

                        return blockinfoResult;
                    }
                    catch (TaskCanceledException)
                    {
                        ThrowHelper.ThrowTimeoutException();
                    }
                }
            }

            return null;
        }

        private void HandlerErrorResult(ushort id, CallbackHandler<S7PlcBlockInfoAckDatagram> cbh, S7PlcBlockInfoAckDatagram blockinfoResult)
        {
            if (blockinfoResult == null)
            {
                if (_closeCalled)
                {
                    ThrowHelper.ThrowNotConnectedException();
                }
                else
                {
                    if (cbh.Exception != null)
                    {
                        ThrowHelper.ThrowException(cbh.Exception);
                    }
                    ThrowHelper.ThrowReadTimeoutException(id);
                }
            }
        }

        private void ReceivedS7PlcBlockInfoAckDatagram(Memory<byte> buffer)
        {
            var data = S7PlcBlockInfoAckDatagram.TranslateFromMemory(buffer);

            if (_blockInfoHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.UserData.Parameter.ParamErrorCode != 0)
                {
                    _logger?.LogError("Error while reading blockdata for reference {0}. ParamErrorCode: {1}", data.UserData.Header.ProtocolDataUnitReference, data.UserData.Parameter.ParamErrorCode);
                    cbh.Exception = new Dacs7ParameterException(data.UserData.Parameter.ParamErrorCode);
                    cbh.Event.Set(null);
                }
                if (data.UserData.Data == null)
                {
                    _logger?.LogWarning("No data from blockinfo ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger?.LogWarning("No block info handler found for received read ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }
    }
}
