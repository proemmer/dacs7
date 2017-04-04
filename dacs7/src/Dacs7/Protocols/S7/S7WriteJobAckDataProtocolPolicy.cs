using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dacs7.Protocols.S7
{
    public class S7WriteJobAckDataProtocolPolicy : S7AckDataProtocolPolicy
    {
        private static readonly int MinimumJobAckReadSize = MinimumAckSize + Marshal.SizeOf<S7WriteJobParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7WriteJobParameter
        {
            public byte Function;
            public byte ItemCount;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7WriteJobItemData
        {
            public byte ItemReturnCode;
        }


        public S7WriteJobAckDataProtocolPolicy()
        {
            AddMarker(new byte[] { 0x00, 0x002 }, 6, false);
            AddMarker(new byte[] { 0x05 }, MinimumAckSize, false);
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

            message.SetAttribute("Function", msg[parentOffset + OffsetInPayload("S7WriteJobParameter.Function")]);

            var itemCount = msg[parentOffset + OffsetInPayload("S7WriteJobParameter.ItemCount")];
            message.SetAttribute("ItemCount", itemCount);

            var offset = parentOffset + 2;
            for (var i = 0; i < itemCount; i++)
            {
                var prefix = $"Item[{i}].";
                message.SetAttribute(prefix + "ItemReturnCode", msg[offset + OffsetInPayload("S7WriteJobItemData.ItemReturnCode")]);
                offset += 1;
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
                var prefix = $"Item[{i}].";
                msg.Add(message.GetAttribute(prefix + "ItemReturnCode", (byte)0));
            }
            return msg;
        }

        private static int OffsetInPayload(string aStructMemberName)
        {
            var parts = aStructMemberName.Split('.');
            var dot = aStructMemberName.Contains('.');
            if (!dot || parts.Length == 2 && parts[0] == "S7CommHeader")
            {
                return (int)Marshal.OffsetOf<S7CommHeader>(dot ? parts[1] : aStructMemberName);
            }
            if (parts.Length == 2 && parts[0] == "S7WriteJobParameter")
            {
                return (int)Marshal.OffsetOf<S7WriteJobParameter>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7WriteJobItemData")
            {
                return (int)Marshal.OffsetOf<S7WriteJobItemData>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}