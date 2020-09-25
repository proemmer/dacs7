// Copyright (c) Benjamin Proemmer. All rights reserved.
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
        private readonly ConcurrentDictionary<ushort, AlarmSubscription> _subscriptions = new ConcurrentDictionary<ushort, AlarmSubscription>();


        public async Task CancelAlarmHandlingAsync()
        {
            try
            {
                foreach (var item in _alarmHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }

                foreach (var item in _alarmIndicationHandler.ToList())
                {
                    item.Value?.Event?.Set(null);
                }

                if (_alarmUpdateHandler?.Id != 0)
                {
                    _alarmUpdateHandler?.Event?.Set(null);
                    await DisableAlarmUpdatesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    _logger?.LogWarning("Exception while canceling alarm handling. Exception was {0} - StackTrace: {1}", ex.Message, ex.StackTrace);
                }
                else
                {
                    _logger?.LogWarning("Exception while canceling alarm handling. Exception was {0}", ex.Message);
                }
            }
        }

        public async Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync()
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
            {
                ThrowHelper.ThrowNotConnectedException();
            }

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

                            CallbackHandler<S7PendingAlarmAckDatagram> cbh = null;
                            try
                            {
                                using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                                {
                                    cbh = new CallbackHandler<S7PendingAlarmAckDatagram>(id);
                                    if (_alarmHandler.TryAdd(id, cbh))
                                    {

                                        _logger?.LogTrace("Alarmhandler with id {id} was added.", id);
                                        try
                                        {
                                            if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                            {
                                                // we return false, because if one send faild we expect also all other ones failed.
                                                _logger?.LogWarning("Could not send read pending alarm package with reference <{id}>.", id);
                                                return null;
                                            }
                                            alarmResults = await cbh.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                        }
                                        finally
                                        {
                                            _alarmHandler.TryRemove(id, out _);
                                            _logger?.LogTrace("Alarmhandler with id {id} was removed.", id);
                                        }
                                    }
                                    else
                                    {
                                        _logger?.LogWarning("Could not add pending alarm handler with reference <{id}>.", id);
                                    }
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                if (cbh == null)
                                {
                                    return alarms; // client was shut down without any result, so we return an empty list.
                                }
                                // if we have a result we could handle it.
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
                    ThrowHelper.ThrowReadTimeoutException($"Pending alarm read operation timeout for job {id}");
                }
            }
        }

        public AlarmSubscription CreateAlarmSubscription()
        {
            var userId = GetNextReferenceId();
            var waitHandler = new CallbackHandler<S7AlarmIndicationDatagram>(userId);
            var subscription = new AlarmSubscription(this, waitHandler);
            if (_alarmIndicationHandler.TryAdd(waitHandler.Id, waitHandler))
            {
                if (_subscriptions.TryAdd(waitHandler.Id, subscription))
                {
                    return subscription;
                }
            }
            ThrowHelper.ThrowException(new InvalidOperationException());
            return null;
        }


        [Obsolete("ReceiveAlarmUpdatesAsync is deprecated, please use AlarmSubscription class instead.")]
        public async Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(CancellationToken ct)
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            if (!await EnableAlarmUpdatesAsync().ConfigureAwait(false))
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            var userId = GetNextReferenceId();
            try
            {
                var waitHandler = new CallbackHandler<S7AlarmIndicationDatagram>(userId);
                if (_alarmIndicationHandler.TryAdd(waitHandler.Id, waitHandler))
                {
                    if (waitHandler.Event != null)
                    {
                        var result = await waitHandler.Event.WaitAsync(ct).ConfigureAwait(false);
                        if (result != null)
                        {
                            return new AlarmUpdateResult(_alarmUpdateHandler?.Id == 0, result.AlarmMessage.Alarms.ToList(), () => DisableAlarmUpdatesAsync());
                        }
                        else
                        {
                            _logger?.LogDebug("AlarmIndication handler received with null result for handler id {0}.", waitHandler.Id);
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Alarm indication handler with id {0} has currently no event handler.", waitHandler.Id);
                    }
                }
                else
                {
                    _logger?.LogWarning("Could not add alarm indication handler with id {0} to handler list", waitHandler.Id);
                }
            }
            finally
            {
                _alarmIndicationHandler.TryRemove(userId, out _);
            }

            return new AlarmUpdateResult(_alarmUpdateHandler?.Id == 0, () => DisableAlarmUpdatesAsync());
        }



        internal async Task RemoveAlarmSubscriptionAsync(AlarmSubscription subscription)
        {
            var userId = subscription.CallbackHandler.Id;
            if (_subscriptions.TryRemove(userId, out _))
            {
                if (_alarmIndicationHandler.TryRemove(userId, out _))
                {
                    if (!_alarmIndicationHandler.Any())
                    {
                        await DisableAlarmUpdatesAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        internal async Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(AlarmSubscription subscription, CancellationToken ct)
        {
            if (_closeCalled || ConnectionState != ConnectionState.Opened)
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            if (!await EnableAlarmUpdatesAsync().ConfigureAwait(false))
            {
                ThrowHelper.ThrowNotConnectedException();
            }

            if (subscription.CallbackHandler != null)
            {
                if (subscription.CallbackHandler.Event != null)
                {
                    subscription.CallbackHandler.Event.Reset();
                    if (!subscription.TryGetDatagram(out var result))
                    {
                        await subscription.CallbackHandler.Event.WaitAsync(ct).ConfigureAwait(false);

                        if (subscription.TryGetDatagram(out result) && result != null)
                        {
                            return new AlarmUpdateResult(_alarmUpdateHandler?.Id == 0, result.AlarmMessage.Alarms.ToList(), null);
                        }
                        else
                        {
                            _logger?.LogDebug("AlarmIndication handler received with null result for handler id {0}.", subscription.CallbackHandler.Id);
                        }
                    }
                    else if (result != null)
                    {
                        return new AlarmUpdateResult(_alarmUpdateHandler?.Id == 0, result.AlarmMessage.Alarms.ToList(), null);
                    }
                }
                else
                {
                    _logger?.LogWarning("Alarm indication handler with id {0} has currently no event handler.", subscription.CallbackHandler.Id);
                }
            }
            else
            {
                _logger?.LogWarning("Could not add alarm indication handler with id {0} to handler list", subscription.CallbackHandler.Id);
            }

            return new AlarmUpdateResult(_alarmUpdateHandler?.Id == 0, null);
        }


        private async Task<bool> EnableAlarmUpdatesAsync()
        {
            CallbackHandler<S7AlarmUpdateAckDatagram> cbh = null;
            if (_alarmUpdateHandler?.Id == 0)
            {
                var id = GetNextReferenceId();
                using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildAlarmUpdateRequest(_s7Context, id), out var memoryLength))
                {
                    using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                    {
                        if (_concurrentJobs == null)
                        {
                            return false;
                        }

                        try
                        {
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                if (_alarmUpdateHandler?.Id == 0)
                                {
                                    cbh = new CallbackHandler<S7AlarmUpdateAckDatagram>(id);
                                    _alarmUpdateHandler = cbh;
                                    try
                                    {
                                        if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        {
                                            return false;
                                        }

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
                        catch (ObjectDisposedException)
                        {
                            _alarmUpdateHandler = cbh;
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private async Task<bool> DisableAlarmUpdatesAsync()
        {
            if (_alarmUpdateHandler?.Id != 0)
            {
                if (_concurrentJobs == null)
                {
                    return false;
                }

                using (var dg = S7UserDataDatagram.TranslateToMemory(S7UserDataDatagram.BuildAlarmUpdateRequest(_s7Context, _alarmUpdateHandler.Id, false), out var memoryLength))
                {
                    using (var sendData = _transport.Build(dg.Memory.Slice(0, memoryLength), out var sendLength))
                    {
                        if (_concurrentJobs == null)
                        {
                            return false;
                        }

                        try
                        {
                            using (await SemaphoreGuard.Async(_concurrentJobs).ConfigureAwait(false))
                            {
                                if (_alarmUpdateHandler?.Id != 0)
                                {
                                    try
                                    {
                                        if (await _transport.Client.SendAsync(sendData.Memory.Slice(0, sendLength)).ConfigureAwait(false) != SocketError.Success)
                                        {
                                            return false;
                                        }

                                        if (_alarmUpdateHandler?.Event != null)
                                        {
                                            await _alarmUpdateHandler.Event.WaitAsync(_s7Context.Timeout).ConfigureAwait(false);
                                        }
                                        _alarmUpdateHandler = new CallbackHandler<S7AlarmUpdateAckDatagram>();
                                    }
                                    catch (Exception)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            return false;
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
                    _logger?.LogError("Error while reading pending alarms for reference {0}. ParamErrorCode: {1}", data.UserData.Header.ProtocolDataUnitReference, data.UserData.Parameter.ParamErrorCode);
                    cbh.Exception = new Dacs7ParameterException(data.UserData.Parameter.ParamErrorCode);
                    cbh.Event.Set(null);
                }

                if (data.UserData.Data == null)
                {
                    _logger?.LogWarning("No data from pending alarm  ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                cbh.Event.Set(data);
            }
            else
            {
                _logger?.LogWarning("No read handler found for received pending alarm ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

        private void ReceivedS7AlarmUpdateAckDatagram(Memory<byte> buffer)
        {
            var data = S7AlarmUpdateAckDatagram.TranslateFromMemory(buffer);

            if (_alarmUpdateHandler.Id != 0)
            {
                if (data.UserData.Data == null)
                {
                    _logger?.LogWarning("No data from alarm update ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
                }
                _alarmUpdateHandler.Event.Set(data);
            }
            else
            {
                _logger?.LogWarning("No read handler found for received alarm update ack reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }
        }

        private void ReceivedS7AlarmIndicationDatagram(Memory<byte> buffer)
        {
            var data = S7AlarmIndicationDatagram.TranslateFromMemory(buffer);
            if (data.UserData.Data == null)
            {
                _logger?.LogWarning("No data from alarm update ack received for reference {0}", data.UserData.Header.ProtocolDataUnitReference);
            }

            foreach (var subscription in _subscriptions.Values)
            {
                subscription.AddDatagram(data);
            }

            foreach (var handler in _alarmIndicationHandler.Where(x => !_subscriptions.ContainsKey(x.Key)).Select(x => x.Value))
            {
                handler.Event.Set(data);
            }
        }

    }

}
