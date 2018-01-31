using Dacs7.Helper;
using Dacs7.Protocols.S7;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dacs7.Alarms
{
    public static class PlcAlarmExtensions
    {
        private static Type AlarmCallbackType = typeof(S7UserDataAckAlarmUpdateProtocolPolicy);

        /// <summary>
        /// Read the current pending alarms from the PLC.
        /// </summary>
        /// <returns>returns a list of all pending alarms</returns>
        public static IEnumerable<IPlcAlarm> ReadPendingAlarms(this Dacs7Client client)
        {
            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();


            var id = client.GetNextReferenceId();
            var policy = new S7UserDataProtocolPolicy();
            var alarms = new List<IPlcAlarm>();
            var lastUnit = false;
            var sequenceNumber = (byte)0x00;
            client.Logger?.LogDebug($"ReadBlockInfo: ProtocolDataUnitReference is {id}");

            do
            {
                var reqMsg = S7MessageCreator.CreatePendingAlarmRequest(id, sequenceNumber);

                if (client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
                {
                    cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                    cbh.ResponseMessage.EnsureValidReturnCode(0xff);

                    var numberOfAlarms = cbh.ResponseMessage.GetAttribute("NumberOfAlarms", 0);
                    var result = new List<IPlcAlarm>();
                    for (var i = 0; i < numberOfAlarms; i++)
                    {
                        try
                        {
                            var subItemName = string.Format("Alarm[{0}].", i) + "{0}";
                            var isGoing = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "EventState"), (byte)0x00) == 0x00;
                            result.Add(new PlcAlarm
                            {
                                Id = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "Id"), (UInt16)0),
                                AlarmMessageType = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "AlarmMessageType"), AlarmMessageType.Unknown),
                                MsgNumber = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "MsgNumber"), (UInt32)0),
                                EventState = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "EventState"), (byte)0x00),
                                State = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "State"), (byte)0x00),
                                AckStateGoing = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "AckStateGoing"), (byte)0x00),
                                AckStateComing = cbh.ResponseMessage.GetAttribute(string.Format(subItemName, "AckStateComing"), (byte)0x00),
                                Timestamp = PlcAlarm.ExtractTimestamp(cbh.ResponseMessage, i, isGoing ? 1 : 0),
                                AssociatedValue = PlcAlarm.ExtractAssociatedValue(cbh.ResponseMessage, i)
                            });
                        }
                        catch (Exception ex)
                        {
                            client.Logger?.LogError($"ReadPendingAlarms exception working on alarm {i}. Exception was: {ex.Message}");
                            throw;
                        }
                    }

                    lastUnit = cbh.ResponseMessage.GetAttribute("LastDataUnit", true);
                    sequenceNumber = cbh.ResponseMessage.GetAttribute("SequenceNumber", (byte)0x00);

                    return result;

                }) is IEnumerable<IPlcAlarm> alarmPart)
                    alarms.AddRange(alarmPart);
            } while (!lastUnit);
            return alarms;
        }

        /// <summary>
        /// Read the current pending alarms asynchronous from the PLC.
        /// </summary>
        /// <returns>returns a list of all pending alarms</returns>
        public static Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync(this Dacs7Client client)
        {
            return Task.Factory.StartNew(() => ReadPendingAlarms(client), client.TaskCreationOptions);
        }

        /// <summary>
        /// Register a alarm changed callback. After this you will be notified if a Alarm is coming or going.
        /// </summary>
        /// <param name="onAlarmUpdate">Callback to alarm data change</param>
        /// <param name="onErrorOccured">Callback to error in routine</param>
        /// <returns></returns>
        public static ushort RegisterAlarmUpdateCallback(this Dacs7Client client, Action<IPlcAlarm> onAlarmUpdate, Action<Exception> onErrorOccured = null)
        {
            if (client.HasCallbackId(AlarmCallbackType))
                throw new Exception("There is already an update callback registered. Only one alarm update callback is allowed!");

            if (!client.IsConnected)
                throw new Dacs7NotConnectedException();

            var id = client.GetNextReferenceId();
            var reqMsg = S7MessageCreator.CreateAlarmCallbackRequest(id);
            var policy = new S7UserDataProtocolPolicy();
            client.Logger?.LogDebug($"RegisterAlarmUpdateCallback: ProtocolDataUnitReference is {id}");
            return (ushort)client.PerformDataExchange(id, reqMsg, policy, (cbh) =>
            {
                cbh.ResponseMessage.EnsureValidParameterErrorCode(0);
                cbh.ResponseMessage.EnsureValidReturnCode(0xff);

                var callbackId = client.GetNextReferenceId();
                var cbhOnUpdate = client.GetCallbackHandler(callbackId, true);
                if(!client.TrySetCallbackId(AlarmCallbackType, callbackId))
                {
                    throw new Exception("There is already an update callback registered. Only one alarm update callback is allowed!");
                }

                cbhOnUpdate.OnCallbackAction = (msg) =>
                {
                    if (msg != null)
                    {
                        try
                        {
                            cbh.ResponseMessage.EnsureValidReturnCode(0xff);
                            var dataLength = msg.GetAttribute("UserDataLength", (UInt16)0);
                            if (dataLength > 0)
                            {
                                var numberOfAlarms = msg.GetAttribute("NumberOfAlarms", (byte)0);
                                for (var i = 0; i < numberOfAlarms; i++)
                                {
                                    var subItemName = string.Format("Alarm[{0}].", i) + "{0}";
                                    var isComing = msg.GetAttribute(string.Format(subItemName, "IsComing"), false);
                                    onAlarmUpdate(new PlcAlarm
                                    {
                                        Id = msg.GetAttribute(string.Format(subItemName, "Id"), (UInt16)0),
                                        AlarmMessageType = msg.GetAttribute(string.Format(subItemName, "AlarmMessageType"), AlarmMessageType.Unknown),
                                        MsgNumber = msg.GetAttribute(string.Format(subItemName, "MsgNumber"), (UInt32)0),
                                        EventState = msg.GetAttribute(string.Format(subItemName, "EventState"), (byte)0x00),
                                        State = msg.GetAttribute(string.Format(subItemName, "State"), (byte)0x00),
                                        AckStateGoing = msg.GetAttribute(string.Format(subItemName, "AckStateGoing"), (byte)0x00),
                                        AckStateComing = msg.GetAttribute(string.Format(subItemName, "AckStateComing"), (byte)0x00),
                                        AlarmSource = msg.GetAttribute(string.Format(subItemName, "AlarmSource"), (byte)0),
                                        Timestamp = PlcAlarm.ExtractTimestamp(msg, i),
                                        AssociatedValue = PlcAlarm.ExtractAssociatedValue(msg, i, 0),
                                        IsAck = msg.GetAttribute(string.Format(subItemName, "IsAck"), false),
                                    });
                                }
                                return;
                            }
                            throw new InvalidDataException("SSL Data are empty!");
                        }
                        catch (Exception ex)
                        {
                            client.Logger?.LogError($"Exception in RegisterAlarmUpdateCallback after callback occured with a message. Error was {ex.Message}");
                            onErrorOccured?.Invoke(ex);
                        }
                    }
                    else if (cbhOnUpdate.OccuredException != null)
                    {
                        client.Logger?.LogError($"Exception in RegisterAlarmUpdateCallback after callback occured without a message. Error was {cbhOnUpdate.OccuredException.Message}");
                        onErrorOccured?.Invoke(cbhOnUpdate.OccuredException);
                    }
                };
                return callbackId;
            });
        }

        /// <summary>
        /// Remove the callback for alarms, so you will not get alarms any more.
        /// </summary>
        /// <param name="id">registration id created by register method</param>
        public static void UnregisterAlarmUpdate(this Dacs7Client client, ushort id)
        {
            client.UnregisterCallbackId(AlarmCallbackType, id);
        }

    }
}
