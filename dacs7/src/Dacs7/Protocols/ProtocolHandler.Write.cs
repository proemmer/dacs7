// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
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

    internal sealed class WriteResult
    {
        public Exception Exception { get; set; }

    }


    internal sealed partial class ProtocolHandler
    {
        private readonly ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>> _writeHandler = new ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemWriteResult>>>();

        public Task CancelWriteHandlingAsync()
        {
            try
            {
                foreach (var item in _writeHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }
            }
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    _logger?.LogWarning("Exception while cancelling write handling. Exception was {0} - StackTrace: {1}", ex.Message, ex.StackTrace);
                }
                else
                {
                    _logger?.LogWarning("Exception while canceling read handling. Exception was {0}", ex.Message);
                }
            }
            return Task.CompletedTask;
        }


        public async Task<IEnumerable<ItemResponseRetValue>> WriteAsync(IEnumerable<WriteItem> vars)
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();


            var result = vars.ToDictionary(x => x, x => ItemResponseRetValue.Success);
            foreach (var normalized in CreateWritePackages(_s7Context, vars))
            {
                if (!await WritePackage(result, normalized).ConfigureAwait(false)) return new List<ItemResponseRetValue>();
            }
            return result.Values;
        }


        private async Task<bool> WritePackage(Dictionary<WriteItem, ItemResponseRetValue> result, WritePackage normalized)
        {
            var id = GetNextReferenceId();
            CallbackHandler<IEnumerable<S7DataItemWriteResult>> cbh = null;
            using (var dg = S7WriteJobDatagram.TranslateToMemory(S7WriteJobDatagram.BuildWrite(_s7Context, id, normalized.Items), out var memoryLegth))
            {
                using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLegth), out var sendLength))
                {
                    try
                    {
                        IEnumerable<S7DataItemWriteResult> writeResults = null;
                        try
                        {
                            if (_concurrentJobs == null) return false;
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<IEnumerable<S7DataItemWriteResult>>(id);
                                if (_writeHandler.TryAdd(id, cbh))
                                {
                                    _logger?.LogTrace("Write handler with id {id} was added.", id);
                                    try
                                    {
                                        if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        {
                                            // we return false, because if one send faild we expect also all other ones failed.
                                            _logger?.LogWarning("Could not send write package with reference <{id}>.", id);
                                            return false;
                                        }
                                        writeResults = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                    }
                                    finally
                                    {
                                        _writeHandler.TryRemove(id, out _);
                                        _logger?.LogTrace("Write handler with id {id} was removed.", id);
                                    }
                                }
                                else
                                {
                                    _logger?.LogWarning("Could not add write handler with reference <{id}>.", id);
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (cbh == null) return false;
                        }

                        HandlerErrorResult(id, cbh, writeResults);

                        BildResults(result, normalized, writeResults);
                    }
                    catch (TaskCanceledException)
                    {
                        ThrowHelper.ThrowTimeoutException();
                    }
                }
            }
            return true;
        }

        private void HandlerErrorResult(ushort id, CallbackHandler<IEnumerable<S7DataItemWriteResult>> cbh, IEnumerable<S7DataItemWriteResult> writeResults)
        {
            if (writeResults == null)
            {
                if (_closeCalled)
                {
                    ThrowHelper.ThrowNotConnectedException(cbh.Exception);
                }
                else
                {
                    if (cbh.Exception != null)
                    {
                        ThrowHelper.ThrowException(cbh.Exception);
                    }
                    ThrowHelper.ThrowWriteTimeoutException(id);

                }
            }
        }

        private static void BildResults(Dictionary<WriteItem, ItemResponseRetValue> result, WritePackage normalized, IEnumerable<S7DataItemWriteResult> writeResults)
        {
            var items = normalized.Items.GetEnumerator();
            foreach (var item in writeResults)
            {
                if (items.MoveNext())
                {
                    var current = items.Current;
                    if (current.IsPart)
                    {
                        if (result.TryGetValue(current.Parent, out var retCode) && retCode == ItemResponseRetValue.Success)
                        {
                            result[current.Parent] = (ItemResponseRetValue)item.ReturnCode;
                        }
                    }
                    else
                    {
                        result[current] = (ItemResponseRetValue)item.ReturnCode;
                    }
                }
            }
        }

        private void ReceivedWriteJobAck(Memory<byte> buffer)
        {
            var data = S7WriteJobAckDatagram.TranslateFromMemory(buffer);

            if (_writeHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.Header.Error.ErrorClass != 0)
                {
                    _logger?.LogError("Error while writing data for reference {0}. ErrorClass: {1}  ErrorCode:{2}", data.Header.Header.ProtocolDataUnitReference, data.Header.Error.ErrorClass, data.Header.Error.ErrorCode);
                    cbh.Exception = new Dacs7Exception(data.Header.Error.ErrorClass, data.Header.Error.ErrorCode);
                }
                if (data.Data == null)
                {
                    _logger?.LogWarning("No data from write ack received for reference {0}", data.Header.Header.ProtocolDataUnitReference);
                }

                cbh.Event.Set(data.Data);
            }
            else
            {
                _logger?.LogWarning("No write handler found for received write ack reference {0}", data.Header.Header.ProtocolDataUnitReference);
            }
        }

        private static IEnumerable<WritePackage> CreateWritePackages(SiemensPlcProtocolContext s7Context, IEnumerable<WriteItem> vars)
        {
            var result = new List<WritePackage>();
            foreach (var item in vars.OrderByDescending(x => x.NumberOfItems).ToList())
            {
                var currentPackage = result.FirstOrDefault(package => package.TryAdd(item));
                if (currentPackage == null)
                {
                    if (item.NumberOfItems > s7Context.WriteItemMaxLength)
                    {
                        var bytesToWrite = item.NumberOfItems;
                        ushort processed = 0;
                        while (bytesToWrite > 0)
                        {
                            var slice = Math.Min(s7Context.WriteItemMaxLength, bytesToWrite);
                            var child = WriteItem.CreateChild(item, (ushort)(item.Offset + processed), slice);
                            if (slice < s7Context.WriteItemMaxLength)
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
                                    ThrowHelper.ThrowCouldNotAddPackageException(nameof(WritePackage));
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
                            ThrowHelper.ThrowCouldNotAddPackageException(nameof(WritePackage));
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
