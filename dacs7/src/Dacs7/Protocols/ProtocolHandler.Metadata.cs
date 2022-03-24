// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>> _blockInfoHandler = new();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksCountAckDatagram>> _blocksCountHandler = new();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PlcBlocksOfTypeAckDatagram>> _blocksOfTypeHandler = new();

        public Task CancelMetaDataHandlingAsync()
        {
            try
            {
                foreach (KeyValuePair<ushort, CallbackHandler<S7PlcBlockInfoAckDatagram>> item in _blockInfoHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }
                foreach (KeyValuePair<ushort, CallbackHandler<S7PlcBlocksCountAckDatagram>> item in _blocksCountHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }
            }
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    _logger?.LogWarning("Exception while canceling meta data handling. Exception was {0} - StackTrace: {1}", ex.Message, ex.StackTrace);
                }
                else
                {
                    _logger?.LogWarning("Exception while canceling meta data handling. Exception was {0}", ex.Message);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<S7PlcBlockInfoAckDatagram> ReadBlockInfoAsync(PlcBlockType type, int blocknumber)
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            ushort id = GetNextReferenceId();
            using (IMemoryOwner<byte> dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlockInfoRequest(_s7Context, id, type, blocknumber), out int memoryLength))
            {
                using (IMemoryOwner<byte> sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out int sendLength))
                {
                    try
                    {
                        CallbackHandler<S7PlcBlockInfoAckDatagram> cbh = null;
                        S7PlcBlockInfoAckDatagram blockinfoResult = null;
                        try
                        {
                            if (_concurrentJobs == null)
                            {
                                return null;
                            }

                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<S7PlcBlockInfoAckDatagram>(id);
                                if (_blockInfoHandler.TryAdd(id, cbh))
                                {
                                    _logger?.LogTrace("Metadata read handler with id {id} was added.", id);

                                    try
                                    {
                                        if (await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        {
                                            // we return false, because if one send faild we expect also all other ones failed.
                                            _logger?.LogWarning("Could not send metadata read package with reference <{id}>.", id);
                                            return null;
                                        }
                                        blockinfoResult = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                    }
                                    finally
                                    {
                                        _blockInfoHandler.TryRemove(id, out _);
                                        _logger?.LogTrace("Metadata read handler with id {id} was removed.", id);
                                    }
                                }
                                else
                                {
                                    _logger?.LogWarning("Could not add metadata read handler with reference <{id}>.", id);
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (cbh == null)
                            {
                                return null;
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
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            ushort id = GetNextReferenceId();
            using (IMemoryOwner<byte> dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlocksCountRequest(_s7Context, id), out int memoryLength))
            {
                using (IMemoryOwner<byte> sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out int sendLength))
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
                                if (await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                {
                                    return null;
                                }

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
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            ushort id = GetNextReferenceId();
            byte sequenceNumber = 0x00;
            List<IPlcBlock> blocks = new();
            IMemoryOwner<byte> memoryOwner = null;
            int currentPosition = 0;
            int totalLength = 0;
            try
            {

                S7PlcBlocksOfTypeAckDatagram blocksOfTypeResults = null;
                do
                {
                    using (IMemoryOwner<byte> dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildBlocksOfTypeRequest(_s7Context, id, type, sequenceNumber), out int memoryLength))
                    {
                        using (IMemoryOwner<byte> sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out int sendLength))
                        {

                            CallbackHandler<S7PlcBlocksOfTypeAckDatagram> cbh;
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<S7PlcBlocksOfTypeAckDatagram>(id);
                                _blocksOfTypeHandler.TryAdd(cbh.Id, cbh);
                                try
                                {
                                    if (await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                    {
                                        return null;
                                    }

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
                                    IMemoryOwner<byte> newMem = MemoryPool<byte>.Shared.Rent(totalLength);
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
                    ThrowHelper.ThrowReadTimeoutException($"Metadata read operation timeout for job {id}");
                }
            }
        }

        private void ReceivedS7PlcBlockInfoAckDatagram(Memory<byte> buffer)
        {
            S7PlcBlockInfoAckDatagram data = S7PlcBlockInfoAckDatagram.TranslateFromMemory(buffer);

            if (_blockInfoHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlockInfoAckDatagram> cbh))
            {
                if (data.UserData.Parameter.ParamErrorCode != (int)ErrorParameter.AtLeatOneOfTheGivenBlocksNotFound)
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
            S7PlcBlocksCountAckDatagram data = S7PlcBlocksCountAckDatagram.TranslateFromMemory(buffer);

            if (_blocksCountHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlocksCountAckDatagram> cbh))
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
            S7PlcBlocksOfTypeAckDatagram data = S7PlcBlocksOfTypeAckDatagram.TranslateFromMemory(buffer);

            if (_blocksOfTypeHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out CallbackHandler<S7PlcBlocksOfTypeAckDatagram> cbh))
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
