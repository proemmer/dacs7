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
            var length = message.GetAttribute("UserDataLength", (UInt16)0);
            var sslData = message.GetAttribute("SSLData", new Byte[0]);
            if (length > 6)
            {
                var offset = 0;
                var timestamp = sslData.ToDateTime(offset);
                var functionIdentifier = sslData.GetSwap<UInt16>(offset += 8);
                var numberOfMsgObjects = sslData[offset += 1];
                message.SetAttribute("NumberOfAlarms", numberOfMsgObjects);
                for (int i = 0; i < numberOfMsgObjects; i++)
                { 
                    var variableSpec = sslData[offset += 1];
                    var specLength = sslData[offset += 1];
                    var syntaxId = sslData[offset += 1];

                    var subItemName = string.Format("Alarm[{0}].", i) + "{0}";
                    var subItemExtended = string.Format("Alarm[{0}].ExtendedData[{1}].", i, 0) + "{0}";

                    message.SetAttribute(string.Format(subItemExtended, "Timestamp"), timestamp);
                    message.SetAttribute(string.Format(subItemName, "AlarmSource"), sslData.GetSwap<UInt16>(offset += 2)); // 0x00 == Pdiag,  0x20 = Graph,  0x40 = 2. Graph Störung
                    message.SetAttribute(string.Format(subItemName, "Id"), sslData.GetSwap<UInt16>(offset += 2));
                    message.SetAttribute(string.Format(subItemName, "MsgNumber"), new byte[] { 0x60, 0x00, sslData[offset], sslData[offset + 1] }.GetSwap<UInt32>());
                    message.SetAttribute(string.Format(subItemName, "IsComing"), sslData[offset += 2] == 0x01); // 0x00 == going   0x01  == coming
                    message.SetAttribute(string.Format(subItemName, "IsAck"), sslData[offset += 1] == 0x01);

                    if (length > 20)
                    {
                        message.SetAttribute(string.Format(subItemName, "AllwaysTrue"), sslData[offset += 1] == 0x01);
                        message.SetAttribute(string.Format(subItemName, "Ack"), sslData[offset += 1] != 0x00); // 0x00 == no ack  0x01  == ack
                        if (offset < sslData.Length)
                        {

                            message.SetAttribute(string.Format(subItemExtended, "AssotiatedValueSuccessCode"), sslData[offset += 1]);
                            var transportSize = sslData[offset += 1];
                            var subItemLength = sslData.GetSwap<UInt16>(offset += 1);
                            offset += 2;
                            var lengthInByte = transportSize <= (int)DataTransportSize.Int ? subItemLength / 8 : subItemLength;

                            message.SetAttribute(string.Format(subItemExtended, "TransportSize"), transportSize);
                            message.SetAttribute(string.Format(subItemExtended, "AssotiatedValueLength"), lengthInByte);

                            if (lengthInByte > 0)
                            {
                                message.SetAttribute(string.Format(subItemExtended, "AssotiatedValue"), sslData.Skip(offset).Take(lengthInByte).ToArray());
                                message.SetAttribute(string.Format(subItemExtended, "NumberOfAssotiatedValues"), 1);
                            }
                        }
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