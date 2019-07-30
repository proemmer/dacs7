﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Alarms;
using Dacs7.Helper;
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
    internal sealed partial class ProtocolHandler
    {
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7PendingAlarmAckDatagram>> _alarmHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7PendingAlarmAckDatagram>>();
        private CallbackHandler<S7AlarmUpdateAckDatagram> _alarmUpdateHandler = new CallbackHandler<S7AlarmUpdateAckDatagram>();
        private readonly ConcurrentDictionary<ushort, CallbackHandler<S7AlarmIndicationDatagram>> _alarmIndicationHandler = new ConcurrentDictionary<ushort, CallbackHandler<S7AlarmIndicationDatagram>>();


        public async Task CancelAlarmHandlingAsync()
        {
            try
            {
                foreach (var item in _alarmHandler.ToList())
                {
                    item.Value.Event?.Set(null);
                }

                foreach (var item in _alarmIndicationHandler.ToList())
                {
                    item.Value.Event?.Set(null);
                }

                if (_alarmUpdateHandler.Id != 0)
                {
                    _alarmUpdateHandler.Event?.Set(null);
                    await DisableAlarmUpdatesAsync().ConfigureAwait(false);
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning("Exception while canceling alarm handling. Exception was {0}", ex.Message);
            }
        }

        public async Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync()
        {
            if (ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();

            var id = GetNextReferenceId();
            var sequenceNumber = (byte)0x00;
            var alarms = new List<IPlcAlarm>();
            IMemoryOwner<byte> memoryOwner = null;
            var currentPosition = 0;
            var totalLength = 0;
            try
            {

                S7PendingAlarmAckDatagram alarmResults = null;
                do
                {
                    using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildPendingAlarmRequest(_s7Context, id, sequenceNumber), out var memoryLength))
                    {
                        using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                        {

                            CallbackHandler<S7PendingAlarmAckDatagram> cbh;
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                cbh = new CallbackHandler<S7PendingAlarmAckDatagram>(id);
                                _alarmHandler.TryAdd(cbh.Id, cbh);
                                try
                                {
                                    if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        return null;

                                    alarmResults = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);

                                }
                                finally
                                {
                                    _alarmHandler.TryRemove(cbh.Id, out _);
                                }
                            }

                            HandleErrorResult(id, cbh, alarmResults);

                            if (alarmResults.UserData.Data.UserDataLength > 4)
                            {
                                if (memoryOwner == null)
                                {
                                    totalLength = BinaryPrimitives.ReadUInt16BigEndian(alarmResults.UserData.Data.Data.Span.Slice(4, 2)) + 6; // 6 is the header
                                    memoryOwner = MemoryPool<byte>.Shared.Rent(totalLength);
                                }

                                alarmResults.UserData.Data.Data.CopyTo(memoryOwner.Memory.Slice(currentPosition, alarmResults.UserData.Data.Data.Length));
                                currentPosition += alarmResults.UserData.Data.Data.Length;
                                sequenceNumber = alarmResults.UserData.Parameter.SequenceNumber;
                            }
                            else
                            {
                                totalLength = 0;
                            }

                        }
                    }
                } while (alarmResults.UserData.Parameter.LastDataUnit == 0x01);

                if (memoryOwner != null)
                {
                    alarms = S7PendingAlarmAckDatagram.TranslateFromSslData(memoryOwner.Memory, totalLength);
                }

            }
            finally
            {
                memoryOwner?.Dispose();
            }


            return alarms;
        }

        private void HandleErrorResult(ushort id, CallbackHandler<S7PendingAlarmAckDatagram> cbh, S7PendingAlarmAckDatagram alarmResults)
        {
            if (alarmResults == null)
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

        public async Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(CancellationToken ct)
        {
            if (ConnectionState != ConnectionState.Opened)
                ThrowHelper.ThrowNotConnectedException();

            if (!await EnableAlarmUpdatesAsync().ConfigureAwait(false))
                ThrowHelper.ThrowNotConnectedException();

            var userId = GetNextReferenceId();
            try
            {
                var waitHandler = new CallbackHandler<S7AlarmIndicationDatagram>(userId);
                if (_alarmIndicationHandler.TryAdd(waitHandler.Id, waitHandler))
                {
                    var result = await waitHandler.Event.WaitAsync(ct).ConfigureAwait(false);
                    if (result != null)
                    {
                        return new AlarmUpdateResult(_alarmUpdateHandler.Id == 0, result.AlarmMessage.Alarms.ToList(), () => DisableAlarmUpdatesAsync());
                    }
                }
            }
            finally
            {
                _alarmIndicationHandler.TryRemove(userId, out _);
            }

            return new AlarmUpdateResult(_alarmUpdateHandler.Id == 0, () => DisableAlarmUpdatesAsync());
        }


        private async Task<bool> EnableAlarmUpdatesAsync()
        {
            CallbackHandler<S7AlarmUpdateAckDatagram> cbh;
            if (_alarmUpdateHandler.Id == 0)
            {
                var id = GetNextReferenceId();
                using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildAlarmUpdateRequest(_s7Context, id), out var memoryLength))
                {
                    using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                    {
                        using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                        {
                            if (_alarmUpdateHandler.Id == 0)
                            {
                                cbh = new CallbackHandler<S7AlarmUpdateAckDatagram>(id);
                                _alarmUpdateHandler = cbh;
                                try
                                {
                                    if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        return false;

                                    await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                }
                                catch (Exception)
                                {
                                    _alarmUpdateHandler = new CallbackHandler<S7AlarmUpdateAckDatagram>();
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private async Task<bool> DisableAlarmUpdatesAsync()
        {
            if (_alarmUpdateHandler.Id != 0)
            {
                using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildAlarmUpdateRequest(_s7Context, _alarmUpdateHandler.Id, false), out var memoryLength))
                {
                    using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                    {
                        using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                        {
                            if (_alarmUpdateHandler.Id != 0)
                            {

                                try
                                {
                                    if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        return false;

                                    await _alarmUpdateHandler.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                    _alarmUpdateHandler = new CallbackHandler<S7AlarmUpdateAckDatagram>();
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void ReceivedS7PendingAlarmsAckDatagram(Memory<byte> buffer)
        {
            var data = S7PendingAlarmAckDatagram.TranslateFromMemory(buffer);

            if (_alarmHandler.TryGetValue(data.UserData.Header.ProtocolDataUnitReference, out var cbh))
            {
                if (data.UserData.Parameter.ParamErrorCode != 0)
                {
                    _logger.LogError("Error while reading pending alarms for reference {0}. ParamErrorCode: {1}", data.UserData.Header.ProtocolDataUnitReference, data.UserData.Parameter.ParamErrorCode);
                    cbh.Exception = new Dacs7ParameterException(data.UserData.Parameter.ParamErrorCode);
                    cbh.Event.Set(null);
                }

                if (data.UserData.Data == null)
                {
                    _logger.LogWarning("No data from pending alarm  ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger.LogWarning("No read handler found for received pending alarm ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

        private void ReceivedS7AlarmUpdateAckDatagram(Memory<byte> buffer)
        {
            var data = S7AlarmUpdateAckDatagram.TranslateFromMemory(buffer);

            if (_alarmUpdateHandler.Id != 0)
            {
                if (data.UserData.Data == null)
                {
                    _logger.LogWarning("No data from alarm update ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                _alarmUpdateHandler.Event.Set(data);
            }
            else
            {
                _logger.LogWarning("No read handler found for received alarm update ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

        private void ReceivedS7AlarmIndicationDatagram(Memory<byte> buffer)
        {
            var data = S7AlarmIndicationDatagram.TranslateFromMemory(buffer);
            if (data.UserData.Data == null)
            {
                _logger.LogWarning("No data from alarm update ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }

            foreach (var handler in _alarmIndicationHandler.Values)
            {
                handler.Event.Set(data);
            }
        }

    }
}
