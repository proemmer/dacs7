using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dacs7.Helper.S7;
using Dacs7.Domain;
using Dacs7.Helper;

namespace Dacs7.Helper
{
    public class S7ReadJobAckDataProtocolPolicy : S7AckDataProtocolPolicy
    {
        private static readonly int MinimumJobAckReadSize = MinimumAckSize + Marshal.SizeOf<S7ReadJobParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7ReadJobParameter
        {
            public byte Function;
            public byte ItemCount;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7ReadJobItemData
        {
            public byte ItemReturnCode;
            public byte ItemTransportSize;
            public ushort ItemSpecLength;
            public byte[] ItemData;
        }


        public S7ReadJobAckDataProtocolPolicy()
        {
            AddMarker(new byte[] { 0x00, 0x002 }, 6, false);
            AddMarker(new byte[] { 0x04 }, MinimumAckSize, false);
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumJobAckReadSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            var parentOffset = MinimumAckSize;

            message.SetAttribute("Function", msg[parentOffset + OffsetInPayload("S7ReadJobParameter.Function")]);

            var itemCount = msg[parentOffset + OffsetInPayload("S7ReadJobParameter.ItemCount")];
            message.SetAttribute("ItemCount", itemCount);

            var offset = parentOffset + 2;
            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("Item[{0}].", i);
                message.SetAttribute(prefix + "ItemReturnCode", msg[offset + OffsetInPayload("S7ReadJobItemData.ItemReturnCode")]);
                var transportSize = msg[offset + OffsetInPayload("S7ReadJobItemData.ItemTransportSize")];
                message.SetAttribute(prefix + "ItemTransportSize", transportSize);
                var dataLength = (int)msg.GetSwap<short>(offset + OffsetInPayload("S7ReadJobItemData.ItemSpecLength"));

                if (transportSize != (byte)DataTransportSize.OctetString && transportSize != (byte)DataTransportSize.Real && transportSize != (byte)DataTransportSize.Bit)
                    dataLength = dataLength >> 3;

                message.SetAttribute(prefix + "ItemSpecLength", (ushort)dataLength);
                var dataOffset = offset + OffsetInPayload("S7ReadJobItemData.ItemData");
                message.SetAttribute(prefix + "ItemData", msg.SubArray(dataOffset, (ushort)dataLength));
                offset += dataLength + 4;
                //Fillbyte check
                if (offset % 2 != 0)
                    offset++;
            }
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            msg.Add(message.GetAttribute("Function", (byte)0));
            var itemCount = message.GetAttribute("ItemCount", (byte)0);
            msg.Add(itemCount);

            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("Item[{0}].", i);
                msg.Add(message.GetAttribute(prefix + "ItemReturnCode", (byte)0));
                msg.Add(message.GetAttribute(prefix + "ItemTransportSize", (byte)0));
                var dataLength = message.GetAttribute(prefix + "ItemSpecLength", (ushort) 0);
                msg.AddRange(dataLength.SetSwap());
                msg.AddRange(message.GetAttribute(prefix + "ItemData", new byte[0]));
            }
            return msg;
        }

        private static int OffsetInPayload(string aStructMemberName)
        {
            var parts = aStructMemberName.Split('.');
            var dot = aStructMemberName.Contains('.');
            if (!dot || parts.Length == 2 && parts[0] == "S7CommHeader")
            {
                return (int)Marshal.OffsetOf<S7CommHeader>( dot ? parts[1] : aStructMemberName);
            }
            if (parts.Length == 2 && parts[0] == "S7ReadJobParameter")
            {
                return (int)Marshal.OffsetOf<S7ReadJobParameter>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7ReadJobItemData")
            {
                return (int)Marshal.OffsetOf<S7ReadJobItemData>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}