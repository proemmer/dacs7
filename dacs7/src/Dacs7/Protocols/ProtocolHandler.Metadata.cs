// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler 
    {
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>> _blockInfoHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>>();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksCountAckDatagram>> _blocksCountHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksCountAckDatagram>>();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksOfTypeAckDatagram>> _blocksOfTypeHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksOfTypeAckDatagram>>();

        public Task CancelMetaDataHandlingAsync()
        {
            try
            {
                foreach (var item in _blockInfoHandler.ToList())
                {
                    item.Value.Event?.Set(null);
                }
                foreach (var item in _blocksCountHandler.ToList())
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

        public async Task<S7PlcBlocksCountAckDatagram> ReadBocksCountInfoAsync()
        {
            if (ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();

            var id = GetNextReferenceId();
            using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlocksCountRequest(_s7Context, id), out var memoryLength))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                {
                    try
                    {
                        CallbackHandler<S7PlcBlocksCountAckDatagram> cbh;
                        S7PlcBlocksCountAckDatagram blockinfoResult = null;
                        using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                        {
                            cbh = new CallbackHandler<S7PlcBlocksCountAckDatagram>(id);
                            _blocksCountHandler.TryAdd(cbh.Id, cbh);
                            try
                            {
                                if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                    return null;
                                blockinfoResult = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                            }
                            finally
                            {
                                _blocksCountHandler.TryRemove(cbh.Id, out _);
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

        public async Task<IEnumerable<IPlcBlock>> ReadBlocksOfTypesAsync(PlcBlockType type)
        {
            if (ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();

            var id = GetNextReferenceId();
            var sequenceNumber = (byte)0x00;
            var blocks = new List<IPlcBlock>();
            IMemoryOwner<byte> memoryOwner = null;
            var currentPosition = 0;
            var totalLength = 0;
            try
            {

                S7PlcBlocksOfTypeAckDatagram blocksOfTypeResults = null;
                do
                {
                    using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlocksOfTypeRequest(_s7Context, id, type, sequenceNumber), out var memoryLength))
                    {
                        using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                        {

                            CallbackHandler<S7PlcBlocksOfTypeAckDatagram> cbh;
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<S7PlcBlocksOfTypeAckDatagram>(id);
                                _blocksOfTypeHandler.TryAdd(cbh.Id, cbh);
                                try
                                {
                                    if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        return null;

                                    blocksOfTypeResults = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);

                                }
                                finally
                                {
                                    _blocksOfTypeHandler.TryRemove(cbh.Id, out _);
                                }
                            }

                            HandlerErrorResult(id, cbh, blocksOfTypeResults);

                            if (blocksOfTypeResults.UserData.Data.UserDataLength > 0)
                            {
                                totalLength += blocksOfTypeResults.UserData.Data.UserDataLength; // 6 is the header
                                if (memoryOwner == null)
                                {
                                    memoryOwner = MemoryPool<byte>.Shared.Rent(totalLength);
                                }
                                else
                                {
                                    var newMem = MemoryPool<byte>.Shared.Rent(totalLength);
                                    memoryOwner.Memory.CopyTo(newMem.Memory);
                                    memoryOwner?.Dispose();
                                    memoryOwner = newMem;
                                }

                                blocksOfTypeResults.UserData.Data.Data.CopyTo(memoryOwner.Memory.Slice(currentPosition, blocksOfTypeResults.UserData.Data.UserDataLength));
                                currentPosition += blocksOfTypeResults.UserData.Data.UserDataLength;
                                sequenceNumber = blocksOfTypeResults.UserData.Parameter.SequenceNumber;
                            }
                            else
                            {
                                totalLength = 0;
                            }

                        }
                    }
                } while (blocksOfTypeResults.UserData.Parameter.LastDataUnit == 0x01);

                if (memoryOwner != null)
                {
                    blocks = S7PlcBlocksOfTypeAckDatagram.TranslateFromSslData(memoryOwner.Memory, totalLength);
                }

            }
            finally
            {
                memoryOwner?.Dispose();
            }


            return blocks;
        }




        private void HandlerErrorResult<AckType>(ushort id, CallbackHandler<AckType> cbh, object datagram)
        {
            if (datagram == null)
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

        private void ReceivedS7PlcBlocksCountAckDatagram(Memory<byte> buffer)
        {
            var data = S7PlcBlocksCountAckDatagram.TranslateFromMemory(buffer);

            if (_blocksCountHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.UserData.Parameter.ParamErrorCode != 0)
                {
                    _logger?.LogError("Error while reading blocks count for reference {0}. ParamErrorCode: {1}", data.UserData.Header.ProtocolDataUnitReference, data.UserData.Parameter.ParamErrorCode);
                    cbh.Exception = new Dacs7ParameterException(data.UserData.Parameter.ParamErrorCode);
                    cbh.Event.Set(null);
                }
                if (data.UserData.Data == null)
                {
                    _logger?.LogWarning("No data from blocks count ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger?.LogWarning("No blocks data handler found for received read ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

        private void ReceivedS7PlcBlocksOfTypeAckDatagram(Memory<byte> buffer)
        {
            var data = S7PlcBlocksOfTypeAckDatagram.TranslateFromMemory(buffer);

            if (_blocksOfTypeHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.UserData.Parameter.ParamErrorCode != 0)
                {
                    _logger?.LogError("Error while reading blocks count for reference {0}. ParamErrorCode: {1}", data.UserData.Header.ProtocolDataUnitReference, data.UserData.Parameter.ParamErrorCode);
                    cbh.Exception = new Dacs7ParameterException(data.UserData.Parameter.ParamErrorCode);
                    cbh.Event.Set(null);
                }
                if (data.UserData.Data == null)
                {
                    _logger?.LogWarning("No data from blocks count ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger?.LogWarning("No blocks data handler found for received read ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

    }
}
