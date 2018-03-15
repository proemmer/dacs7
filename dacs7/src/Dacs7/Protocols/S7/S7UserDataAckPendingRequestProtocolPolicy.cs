using System;
using System.Collections.Generic;
using System.Linq;
using Dacs7.Domain;
using Dacs7.Helper;

namespace Dacs7.Protocols.S7
{
    public class S7UserDataAckPendingRequestProtocolPolicy : S7UserDataProtocolPolicy
    {
        private const int MinimumUserDataSize = 24;
        private readonly List<byte> _sslDataCache = new List<byte>();
        private byte _sequenceNumber;
        private int _expectedLength = 0;

        public S7UserDataAckPendingRequestProtocolPolicy()
        {
            //0x84 ->  Response and Type 8
            AddMarker(new byte[] { (byte)UserDataParamTypeType.Response, 0x84, 0x13 }, MinimumSize + 4, false);
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
            var sslHeaderLength = 6;
            var sslData = message.GetAttribute("SSLData", new Byte[0]);
            var sequenceNumber = message.GetAttribute("SequenceNumber", (byte)0);
            var last = message.GetAttribute("LastDataUnit", true);

            if (_sequenceNumber == 0 && !last)
            {

                if (!_sslDataCache.Any())
                {
                    _sequenceNumber = sequenceNumber;
                    _expectedLength = sslData.GetSwap<UInt16>(4) + sslHeaderLength;  // +6 because of the SSL header
                    _sslDataCache.AddRange(sslData);
                    return;
                }
            }
            else if (_sequenceNumber == sequenceNumber)
            {
                if (_sslDataCache.Any())
                {
                    _sslDataCache.AddRange(sslData);
                    if (last || _sslDataCache.Count >= _expectedLength)
                    {
                        sslData = _sslDataCache.ToArray();
                        _sslDataCache.Clear();
                        _sequenceNumber = 0;
                        _expectedLength = 0;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var length = sslData.Length;
            if (length < sslHeaderLength)
            {
                return;
            }

            // read ssl header
            message.SetAttribute("SSLDataId", sslData.GetSwap<UInt16>(0));
            message.SetAttribute("SSLDataSuccessCode", sslData[2]);
            message.SetAttribute("SSLDataTransportSize", sslData[3]);
            var completeDataLength = sslData.GetSwap<UInt16>(4);
            message.SetAttribute("SSLDataLength", completeDataLength);  // complete data length


            var offset = sslHeaderLength;
            var moHeaderLength = 12;
            var assObjHeaderLength = 12;
            var alarmId = 0;
            while ((offset + 1) < length)
            {
                var subItemName = string.Format("Alarm[{0}].", alarmId) + "{0}";
                var lastItemOffset = offset;
                var itemLength = sslData[offset];

                if (itemLength >= moHeaderLength)
                {
                    message.SetAttribute(string.Format(subItemName, "DataLength"), itemLength);
                    message.SetAttribute(string.Format(subItemName, "TransportSize"), sslData.GetSwap<UInt16>(offset + 1));
                    message.SetAttribute(string.Format(subItemName, "AlarmType"), sslData[offset + 3]);
                    message.SetAttribute(string.Format(subItemName, "AlarmMessageType"), sslData[offset + 3] == 4 ? AlarmMessageType.Alarm_S : AlarmMessageType.Unknown);


                    message.SetAttribute(string.Format(subItemName, "Id"), sslData.GetSwap<UInt16>(offset + 6));
                    var msgNumber = new byte[] { 0x60, 0x00, sslData[offset + 6], sslData[offset + 7] }.GetSwap<UInt32>();
                    message.SetAttribute(string.Format(subItemName, "MsgNumber"), msgNumber);

                    message.SetAttribute(string.Format(subItemName, "EventState"), sslData[offset + 8]); // 0x00 == going   0x01  == coming
                    message.SetAttribute(string.Format(subItemName, "State"), sslData[offset + 9]); // isAck
                    message.SetAttribute(string.Format(subItemName, "AckStateGoing"), sslData[offset + 10]);
                    message.SetAttribute(string.Format(subItemName, "AckStateComing"), sslData[offset + 11]); // 0x00 == no ack  0x01  == ack

                    var extendedOffset = offset + moHeaderLength;
                    //If is Going, Ack and Going could be in the Data
                    for (var i = 0; i < 2; i++)
                    {
                        subItemName = string.Format("Alarm[{0}].ExtendedData[{1}].", alarmId, i) + "{0}";

                        try
                        {
                            message.SetAttribute(string.Format(subItemName, "Timestamp"), sslData.ToDateTime(extendedOffset));
                        }
                        catch (FormatException) { }

                        message.SetAttribute(string.Format(subItemName, "AssociatedValueSuccessCode"), sslData[extendedOffset + 8]);
                        var transportSize = sslData[extendedOffset + 9];
                        var subItemLength = sslData.GetSwap<UInt16>(extendedOffset + 10);
                        var lengthInByte = transportSize <= (int)DataTransportSize.Int ? subItemLength / 8 : subItemLength;
                        message.SetAttribute(string.Format(subItemName, "TransportSize"), transportSize);
                        message.SetAttribute(string.Format(subItemName, "AssociatedValueLength"), lengthInByte);

                        if (lengthInByte > 0)
                        {
                            message.SetAttribute(string.Format(subItemName, "AssociatedValue"), sslData.SubArray(extendedOffset + assObjHeaderLength, lengthInByte));
                            message.SetAttribute(string.Format(subItemName, "NumberOfAssociatedValues"), (byte)1);
                        }

                        extendedOffset += assObjHeaderLength + lengthInByte;
                    }
                }

                offset += itemLength + 2;
                alarmId++;
            }
            message.SetAttribute("NumberOfAlarms", alarmId);
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            return msg;
        }
    }
}