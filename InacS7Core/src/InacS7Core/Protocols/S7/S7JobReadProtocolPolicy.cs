using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using InacS7Core.Arch;
using InacS7Core.Helper;

namespace InacS7Core.Helper
{
    public class S7JobReadProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumJobReadSize = MinimumSize + Marshal.SizeOf<S7ReadJobParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7ReadJobParameter
        {
            public byte Function; 
            public byte ItemCount; 
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7ReadJobItem
        {
            public byte VariableSpecification;
            public byte LengthOfAddressSpecification;
            public byte SyntaxId;
            public byte TransportSize;
            public ushort ItemSpecLength;
            public ushort DbNumber;
            public byte Area;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Address;
        }

        public S7JobReadProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)InacS7Core.Domain.PduType.Job }, 1, false);
            AddMarker(new byte[] { 0x04 }, MinimumSize, false);
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumJobReadSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            var parentOffset = MinimumSize;

            message.SetAttribute("Function", msg[parentOffset + OffsetInPayload("S7ReadJobParameter.Function")]);

            var itemCount = msg[parentOffset + OffsetInPayload("S7ReadJobParameter.ItemCount")];
            message.SetAttribute("ItemCount", itemCount);

            var offset = parentOffset + 2;
            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("Item[{0}].", i);
                message.SetAttribute(prefix + "VariableSpecification", msg[offset + OffsetInPayload("S7ReadJobItem.VariableSpecification")]);
                var specLength = msg[offset + OffsetInPayload("S7ReadJobItem.LengthOfAddressSpecification")];
                message.SetAttribute(prefix + "LengthOfAddressSpecification", specLength);
                message.SetAttribute(prefix + "SyntaxId", msg[offset + OffsetInPayload("S7ReadJobItem.SyntaxId")]);
                message.SetAttribute(prefix + "TransportSize", msg[offset + OffsetInPayload("S7ReadJobItem.TransportSize")]);
                message.SetAttribute(prefix + "ItemSpecLength", msg.GetSwap<ushort>(offset + OffsetInPayload("S7ReadJobItem.ItemSpecLength")));
                message.SetAttribute(prefix + "DbNumber", msg.GetSwap<ushort>(offset + OffsetInPayload("S7ReadJobItem.DbNumber")));
                message.SetAttribute(prefix + "Area", msg[offset + OffsetInPayload("S7ReadJobItem.Area")]);
                message.SetAttribute(prefix + "Address", msg.Skip(offset + OffsetInPayload("S7ReadJobItem.Address")).Take(3).ToArray());
                offset += specLength + 2;
            }
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            msg.Add(message.GetAttribute("Function", (byte)0));
            var itemCount = message.GetAttribute("ItemCount", (byte) 0);
            msg.Add(itemCount);

            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("Item[{0}].", i);
                msg.Add(message.GetAttribute(prefix + "VariableSpecification", (byte)0));
                var specLength = message.GetAttribute(prefix + "LengthOfAddressSpecification", (byte) 0);
                msg.Add(specLength);
                msg.Add(message.GetAttribute(prefix + "SyntaxId", (byte)0));
                msg.Add(message.GetAttribute(prefix + "TransportSize", (byte)0));
                msg.AddRange(message.GetAttribute(prefix + "ItemSpecLength", (ushort)0).SetSwap());
                msg.AddRange(message.GetAttribute(prefix + "DbNumber", (ushort)0).SetSwap());
                msg.Add(message.GetAttribute(prefix + "Area", (byte)0));
                msg.AddRange(message.GetAttribute(prefix + "Address", new byte[] {0x00,0x00,0x00}));
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
            if (parts.Length == 2 && parts[0] == "S7ReadJobParameter")
            {
                return (int)Marshal.OffsetOf<S7ReadJobParameter>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7ReadJobItem")
            {
                return (int)Marshal.OffsetOf<S7ReadJobItem>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
