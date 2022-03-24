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
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal sealed partial class ProtocolHandler
    {
        private static readonly List<S7DataItemSpecification> _defaultReadJobResult = new();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>> _readHandler = new();


        public Task CancelReadHandlingAsync()
        {
            try
            {
                foreach (KeyValuePair<ushort, CallbackHandler<IEnumerable<S7DataItemSpecification>>> item in _readHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }
            }
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    _logger?.LogWarning("Exception while canceling read handling. Exception was {0} - StackTrace: {1}", ex.Message, ex.StackTrace);
                }
                else
                {
                    _logger?.LogWarning("Exception while canceling read handling. Exception was {0}", ex.Message);
                }
            }
            return Task.CompletedTask;
        }


        public async Task<Dictionary<ReadItem, S7DataItemSpecification>> ReadAsync(IEnumerable<ReadItem> vars)
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            Dictionary<ReadItem, S7DataItemSpecification> result = vars.ToDictionary(x => x, x => null as S7DataItemSpecification);
            foreach (ReadPackage normalized in CreateReadPackages(_s7Context, vars))
            {
                if (!await ReadPackage(result, normalized).ConfigureAwait(false))
                {
                    return new Dictionary<ReadItem, S7DataItemSpecification>();
                }
            }
            return result;
        }

        private async Task<bool> ReadPackage(Dictionary<ReadItem, S7DataItemSpecification> result, ReadPackage normalized)
        {
            ushort id = GetNextReferenceId();
            using (System.Buffers.IMemoryOwner<byte> dgmem = S7ReadJobDatagram.TranslateToMemory(S7ReadJobDatagram.BuildRead(_s7Context, id, normalized.Items), out int dgmemLength))
            {
                using (System.Buffers.IMemoryOwner<byte> sendData = _transport.Build(dgmem.Memory.Slice(0, dgmemLength), out int sendLength))
                {
                    try
                    {
                        IEnumerable<S7DataItemSpecification> readResults = null;
                        CallbackHandler<IEnumerable<S7DataItemSpecification>> cbh = null;
                        try
                        {
                            if (_concurrentJobs == null)
                            {
                                return false;
                            }

                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<IEnumerable<S7DataItemSpecification>>(id);
                                if (_readHandler.TryAdd(id, cbh))
                                {
                                    _logger?.LogTrace("Read handler with id {id} was added.", id);
                                    try
                                    {
                                        if (await _transport.Connection.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        {
                                            // we cancel return false, because if on esend faild we expect also all other ones failed.
                                            _logger?.LogWarning("Could not send read package with reference <{id}>.", id);
                                            return false;
                                        }
                                        readResults = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                    }
                                    finally
                                    {
                                        _readHandler.TryRemove(id, out _);
                                        _logger?.LogTrace("Read handler with id {id} was removed.", id);

                                    }
                                }
                                else
                                {
                                    _logger?.LogWarning("Could not add read handler with reference <{id}>.", id);
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            if (cbh == null)
                            {
                                return false;
                            }
                        }

                        HandlerErrorResult(id, readResults, cbh);

                        BildResults(result, normalized, readResults);
                    }
                    catch (TaskCanceledException)
                    {
                        ThrowHelper.ThrowTimeoutException();
                    }
                }
            }
            return true;
        }


        private void HandlerErrorResult(ushort id, IEnumerable<S7DataItemSpecification> readResults, CallbackHandler<IEnumerable<S7DataItemSpecification>> cbh)
        {
            if (readResults == null)
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
                    ThrowHelper.ThrowReadTimeoutException(id);
                }
            }
        }

        private static void BildResults(Dictionary<ReadItem, S7DataItemSpecification> result, ReadPackage normalized, IEnumerable<S7DataItemSpecification> readResults)
        {
            IEnumerator<ReadItem> items = normalized.Items.GetEnumerator();
            foreach (S7DataItemSpecification item in readResults)
            {
                if (items.MoveNext())
                {
                    ReadItem current = items.Current;
                    if (current.IsPart)
                    {
                        if (!result.TryGetValue(current.Parent, out S7DataItemSpecification parent) || parent == null)
                        {
                            parent = new S7DataItemSpecification
                            {
                                TransportSize = item.TransportSize,
                                Length = current.Parent.NumberOfItems,
                                Data = new byte[current.Parent.NumberOfItems]
                            };
                            result[current.Parent] = parent;
                        }

                        parent.ReturnCode = item.ReturnCode;

                        item.Data.CopyTo(parent.Data.Slice(current.Offset - current.Parent.Offset, current.NumberOfItems));
                    }
                    else
                    {
                        result[current] = item;
                    }
                }
            }
        }



        private void ReceivedReadJobAck(Memory<byte> buffer)
        {
            S7ReadJobAckDatagram data = S7ReadJobAckDatagram.TranslateFromMemory(buffer);

            if (_readHandler.TryGetValue(data.Header.Header.ProtocolDataUnitReference, out CallbackHandler<IEnumerable<S7DataItemSpecification>> cbh))
            {
                if (data.Header.Error.ErrorClass != 0)
                {
                    _logger?.LogError("Error while reading data for reference {0}. ErrorClass: {1}  ErrorCode:{2}", data.Header.Header.ProtocolDataUnitReference, data.Header.Error.ErrorClass, data.Header.Error.ErrorCode);
                    cbh.Exception = new Dacs7Exception(data.Header.Error.ErrorClass, data.Header.Error.ErrorCode);
                }
                if (data.Data == null)
                {
                    _logger?.LogWarning("No data from read ack received for reference {0}", data.Header.Header.ProtocolDataUnitReference);
                }

                if (cbh.Event != null)
                {
                    cbh.Event.Set(data.Data);
                }
                else
                {
                    _logger?.LogWarning("No event for read handler found for received read ack reference {0}", data.Header.Header.ProtocolDataUnitReference);
                }
            }
            else
            {
                _logger?.LogWarning("No read handler found for received read ack reference {0}", data.Header.Header.ProtocolDataUnitReference);
            }
        }

        //private void ReceivedReadJob(Memory<byte> buffer)
        //{
        //    var data = S7ReadJobDatagram.TranslateFromMemory(buffer);

        //    if (_readHandler.TryGetValue(data.Header.ProtocolDataUnitReference, out var cbh))
        //    {
        //        cbh.Event.Set(_defaultReadJobResult);
        //    }
        //    else
        //    {
        //        _logger?.LogWarning("No read handler found for received read job reference {0}", data.Header.ProtocolDataUnitReference);
        //    }
        //}

        private static IEnumerable<ReadPackage> CreateReadPackages(SiemensPlcProtocolContext s7Context, IEnumerable<ReadItem> vars)
        {
            List<ReadPackage> result = new();
            foreach (ReadItem item in vars.OrderByDescending(x => x.NumberOfItems).ToList())
            {
                ReadPackage currentPackage = result.FirstOrDefault(package => package.TryAdd(item));
                if (currentPackage == null)
                {
                    if (item.NumberOfItems > s7Context.ReadItemMaxLength)
                    {
                        ushort bytesToRead = item.NumberOfItems;
                        ushort processed = 0;
                        while (bytesToRead > 0)
                        {
                            ushort slice = Math.Min(s7Context.ReadItemMaxLength, bytesToRead);
                            ReadItem child = ReadItem.CreateChild(item, (item.Offset + processed), slice);
                            if (slice < s7Context.ReadItemMaxLength)
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
                                    ThrowHelper.ThrowCouldNotAddPackageException(nameof(ReadPackage));
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
                            ThrowHelper.ThrowCouldNotAddPackageException(nameof(ReadPackage));
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
            foreach (ReadPackage package in result)
            {
                yield return package.Return();
            }
        }

    }
}
