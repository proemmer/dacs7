using System;
using System.Collections.Generic;
using System.Linq;
using Dacs7.Helper;
using Dacs7.Domain;

namespace Dacs7.Protocols.S7
{
    public class S7UserDataAckAlarmUpdateProtocolPolicy : S7UserDataProtocolPolicy
    {
        private const int MinimumUserDataSize = 24;


        public S7UserDataAckAlarmUpdateProtocolPolicy()
        {
            //0x84 ->  Response and Type 8
            AddMarker(new byte[] { (byte)UserDataParamTypeType.Request, 0x04 }, MinimumSize + 4, false);
        }


        public override int GetMinimumCountDataBytes()
        {
            return MinimumUserDataSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            base.SetupMessageAttributes(message);
            var subfunction = message.GetAttribute("SubFunction", (byte)0);  // 17 sq  12 ack
            var length = message.GetAttribute("UserDataLength", (UInt16)0);
            var sslData = message.GetAttribute("SSLData", new Byte[0]);
            if (length > 6)
            {
                var offset = 0;
                var timestamp = sslData.ToDateTime(offset);
                var functionIdentifier = sslData[offset + 8];
                var numberOfMsgObjects = sslData[offset + 9];

                offset = 10;
                message.SetAttribute("NumberOfAlarms", numberOfMsgObjects);
                var syntaxId = 0x00;
                for (int i = 0; i < numberOfMsgObjects; i++)
                {
                    var varspec = sslData[offset];
                    var specLenght = sslData[offset + 1];

                    if (specLenght > 0)
                    {
                        if (numberOfMsgObjects < 3 || i == 0)
                        {
                            syntaxId = sslData[offset + 2];
                        }
                        else
                        {
                            // all following messages are incorrect, because they have no syntaxID
                            offset -= 2;
                        }

                        var numberOfAssociatedValues = sslData[offset + 3];

                        var subItemName = string.Format("Alarm[{0}].", i) + "{0}";
                        var subItemExtended = string.Format("Alarm[{0}].ExtendedData[{1}].", i, 0) + "{0}";

                        message.SetAttribute(string.Format(subItemExtended, "Timestamp"), timestamp);
                        message.SetAttribute(string.Format(subItemName, "Id"), sslData.GetSwap<UInt16>(offset + 6));
                        message.SetAttribute(string.Format(subItemName, "MsgNumber"), new byte[] { 0x60, 0x00, sslData[offset + 6], sslData[offset + 7] }.GetSwap<UInt32>());


                        switch (subfunction)
                        {
                            case 17: // ALARM_SQ
                            case 18: // ALARM_S
                                {
                                    message.SetAttribute(string.Format(subItemName, "IsAck"), false);
                                    message.SetAttribute(string.Format(subItemName, "AlarmMessageType"), (AlarmMessageType)subfunction);
                                    message.SetAttribute(string.Format(subItemName, "EventState"), sslData[offset + 8]); // 0x00 == going   0x01  == coming
                                    message.SetAttribute(string.Format(subItemName, "State"), sslData[offset + 9]); // isAck
                                    message.SetAttribute(string.Format(subItemName, "AckStateGoing"), sslData[offset + 10]);
                                    message.SetAttribute(string.Format(subItemName, "AckStateComing"), sslData[offset + 11]); // 0x00 == no ack  0x01  == ack


                                    message.SetAttribute(string.Format(subItemExtended, "NumberOfAssociatedValues"), numberOfAssociatedValues);
                                    var associatedValueValuesLength = 0;
                                    var subOffset = offset + 12;
                                    for (int j = 0; j < numberOfAssociatedValues; j++)
                                    {
                                        var subItemExtendedAssotated = string.Format(subItemExtended, string.Format("AssociatedValue[{0}].", j)) + "{0}";
                                        message.SetAttribute(string.Format(subItemExtendedAssotated, "AssociatedValueSuccessCode"), sslData[subOffset]);
                                        var transportSize = sslData[subOffset + 1];
                                        var subItemLength = sslData.GetSwap<UInt16>(subOffset + 2);
                                        var lengthInByte = transportSize <= (int)DataTransportSize.Int ? subItemLength / 8 : subItemLength;

                                        message.SetAttribute(string.Format(subItemExtendedAssotated, "TransportSize"), transportSize);
                                        message.SetAttribute(string.Format(subItemExtendedAssotated, "AssociatedValueLength"), lengthInByte);
                                        message.SetAttribute(string.Format(subItemExtendedAssotated, "AssociatedValue"), sslData.SubArray(subOffset + 4, lengthInByte));

                                        subOffset += 4 + lengthInByte;
                                        associatedValueValuesLength += 4 + lengthInByte;
                                    }


                                    offset += 2 + specLenght + associatedValueValuesLength;

                                }
                                break;
                            case 12: // ALARM ack
                                {
                                    message.SetAttribute(string.Format(subItemName, "IsAck"), true);
                                    message.SetAttribute(string.Format(subItemName, "AlarmMessageType"), (AlarmMessageType)subfunction);
                                    message.SetAttribute(string.Format(subItemName, "AckStateGoing"), sslData[offset + 8]);
                                    message.SetAttribute(string.Format(subItemName, "AckStateComing"), sslData[offset + 9]); // 0x00 == no ack  0x01  == ack
                                    offset += 2 + specLenght;
                                }
                                break;
                            default:
                                {
                                    throw new Exception($"Unknown alarm subfunction {subfunction}");
                                }
                        }
                    }
                    else
                    {
                        offset += 2;
                    }
                }
            }
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            return msg;
        }
    }
}