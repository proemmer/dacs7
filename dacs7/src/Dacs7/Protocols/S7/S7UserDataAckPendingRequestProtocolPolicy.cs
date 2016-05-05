using System;
using System.Collections.Generic;
using System.Linq;
using Dacs7.Arch;
using Dacs7.Domain;
using Dacs7.Helper;

namespace Dacs7.Helper
{
    public class S7UserDataAckPendingRequestProtocolPolicy : S7UserDataProtocolPolicy
    {
        private const int MinimumUserDataSize = 24;
        private readonly List<byte> _sslDataCache = new List<byte>();
        private byte _sequenceNumber;

        public S7UserDataAckPendingRequestProtocolPolicy()
        {
            //0x84 ->  Response and Type 8
            AddMarker(new byte[] { (byte)UserDataParamTypeType.Response, 0x84 }, MinimumSize + 4, false);
        }


        public override int GetMinimumCountDataBytes()
        {
            return MinimumUserDataSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            SetupSslData(message);
        }

        public void SetupSslData(IMessage message)
        {
            var skipHeader = false;
            var sslData = message.GetAttribute("SSLData", new Byte[0]);
            var sequenceNumber = message.GetAttribute("SequenceNumber", (byte)0);
            var last = message.GetAttribute("LastDataUnit", true);

            if (_sequenceNumber == sequenceNumber)
            {
                if (_sslDataCache.Any())
                {
                    _sslDataCache.AddRange(sslData);
                    sslData = _sslDataCache.ToArray();
                    _sslDataCache.Clear();
                }

                if (last)
                    _sequenceNumber = 0;
                skipHeader = true;
            }
            


            var length = sslData.Length;
            if (skipHeader || length > 6)
            {

                if (!skipHeader)
                {
                    message.SetAttribute("SSLDataId", sslData.GetSwap<UInt16>(0));
                    message.SetAttribute("SSLDataSuccessCode", sslData[2]);
                    message.SetAttribute("SSLDataTransportSize", sslData[3]);
                    message.SetAttribute("SSLDataLength", sslData.GetSwap<UInt16>(4));
                }

                var offset = skipHeader ? 0 : 6;
                var alarmId = 0;
                while (offset < sslData.Length)
                {
                    var subItemName = string.Format("Alarm[{0}].", alarmId) + "{0}";
                    var lastItemOffset = offset;
                    var itemLength = sslData.GetNoSwap<UInt16>(offset);
                    switch (itemLength)
                    {
                        case 42:
                        case 74: // because 74 isn't correct
                            itemLength = 42;
                            break;
                        case 38:
                            itemLength = 38;
                            break;
                        case 78:
                            itemLength = 48;
                            break;
                    }


                    if (sslData.Length >= (offset + itemLength))
                    {
                        message.SetAttribute(string.Format(subItemName, "DataLength"), itemLength);
                        message.SetAttribute(string.Format(subItemName, "TransportSize"), sslData.GetSwap<UInt16>(offset += 2));
                        message.SetAttribute(string.Format(subItemName, "AlarmSource"), sslData.GetSwap<UInt16>(offset += 2)); // 0x00 == Pdiag,  0x20 = Graph,  0x40 = 2. Graph Störung
                        message.SetAttribute(string.Format(subItemName, "Id"), sslData.GetSwap<UInt16>(offset += 2));
                        var msgNumber = new byte[] {0x60, 0x00, sslData[offset], sslData[offset + 1]}.GetSwap<UInt32>();
                        message.SetAttribute(string.Format(subItemName, "MsgNumber"), msgNumber);
                        message.SetAttribute(string.Format(subItemName, "IsComing"), sslData[offset += 2] == 0x01); // 0x00 == going   0x01  == coming
                        message.SetAttribute(string.Format(subItemName, "IsAck"), sslData[offset += 1] == 0x01);
                        message.SetAttribute(string.Format(subItemName, "AllwaysTrue"), sslData[offset += 1] == 0x01);
                        message.SetAttribute(string.Format(subItemName, "Ack"), sslData[offset += 1] == 0x01); // 0x00 == no ack  0x01  == ack

                        var isGoing = !message.GetAttribute(string.Format(subItemName, "IsComing"), false);
                        //If is Going, Ack and Going could be in the Data
                        for (var i = 0; i < 2; i++)
                        {
                            subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].", alarmId,i) + "{0}";
                            offset = i > 0 ? offset : offset + 1;
                            if (offset < sslData.Length)
                            {
                                var extendedData = false;
                                if (sslData[offset] != 0 && (i == 0 || (i > 0 && isGoing)))
                                {
                                    try
                                    {
                                        message.SetAttribute(string.Format(subItemName, "Timestamp"), sslData.ToDateTime(offset));
                                        extendedData = true;
                                    }
                                    catch (FormatException){}
                                    
                                }
                                offset += 8;

                                if (offset < lastItemOffset + itemLength && offset < sslData.Length)
                                {
                                    if (extendedData)
                                        message.SetAttribute(string.Format(subItemName, "AssotiatedValueSuccessCode"), sslData[offset]);
                                    var transportSize = sslData[offset += 1];
                                    var subItemLength = sslData.GetSwap<UInt16>(offset += 1);
                                    offset += 2;
                                    var lengthInByte = transportSize == 3 || transportSize == 4 ? subItemLength / 8 : subItemLength;

                                    if (extendedData)
                                    {
                                        message.SetAttribute(string.Format(subItemName, "TransportSize"), transportSize);
                                        message.SetAttribute(string.Format(subItemName, "AssotiatedValueLength"), lengthInByte);
                                    }

                                    if (lengthInByte > 0)
                                    {
                                        if (extendedData)
                                        {
                                            message.SetAttribute(string.Format(subItemName, "AssotiatedValue"), sslData.Skip(offset).Take(lengthInByte).ToArray());
                                            message.SetAttribute(string.Format(subItemName, "NumberOfAssotiatedValues"), 1);
                                        }
                                        offset += lengthInByte;
                                    }
                                }
                            }
                        }
                        alarmId++;
                    }
                    else
                    {
                        if (!last)
                        {
                            if (!_sslDataCache.Any())
                            {
                                _sequenceNumber = sequenceNumber;
                                _sslDataCache.AddRange(sslData.Skip(offset));
                            }
                        }
                        break;
                    }
                }
                message.SetAttribute("NumberOfAlarms", alarmId);
            }
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            return msg;
        }
    }
}