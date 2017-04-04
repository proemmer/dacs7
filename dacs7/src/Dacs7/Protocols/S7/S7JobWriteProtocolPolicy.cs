using Dacs7.Domain;
using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dacs7.Helper
{
    public class S7JobWriteProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumJobReadSize = MinimumSize + Marshal.SizeOf<S7WriteJobParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7WriteJobParameter
        {
            public byte Function;
            public byte ItemCount;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7WriteJobItem
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7WriteJobDataItem
        {
            public byte ItemDataReturnCode;
            public byte ItemDataTransportSize;
            public ushort ItemDataLength;
            public byte[] ItemData;
        }


        public S7JobWriteProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Dacs7.Domain.PduType.Job }, 1, false);
            AddMarker(new byte[] { 0x05 }, MinimumSize, false);
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

            message.SetAttribute("Function", msg[parentOffset + OffsetInPayload("S7WriteJobParameter.Function")]);

            var itemCount = msg[parentOffset + OffsetInPayload("S7WriteJobParameter.ItemCount")];
            message.SetAttribute("ItemCount", itemCount);

            var offset = parentOffset + 2;
            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("Item[{0}].", i);
                message.SetAttribute(prefix + "VariableSpecification", msg[offset + OffsetInPayload("S7WriteJobItem.VariableSpecification")]);
                var specLength = msg[offset + OffsetInPayload("S7WriteJobItem.LengthOfAddressSpecification")];
                message.SetAttribute(prefix + "LengthOfAddressSpecification", specLength);
                message.SetAttribute(prefix + "SyntaxId", msg[offset + OffsetInPayload("S7WriteJobItem.SyntaxId")]);
                message.SetAttribute(prefix + "TransportSize", msg[offset + OffsetInPayload("S7WriteJobItem.TransportSize")]);
                message.SetAttribute(prefix + "ItemSpecLength", msg.GetSwap<ushort>(offset + OffsetInPayload("S7WriteJobItem.ItemSpecLength")));
                message.SetAttribute(prefix + "DbNumber", msg.GetSwap<ushort>(offset + OffsetInPayload("S7WriteJobItem.DbNumber")));
                message.SetAttribute(prefix + "Area", msg[offset + OffsetInPayload("S7WriteJobItem.Area")]);
                message.SetAttribute(prefix + "Address", msg.Skip(offset + OffsetInPayload("S7WriteJobItem.Address")).Take(3).ToArray());
                offset += specLength + 2;
            }


            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("DataItem[{0}].", i);
                message.SetAttribute(prefix + "ItemDataReturnCode", msg[offset + OffsetInPayload("S7WriteJobDataItem.ItemDataReturnCode")]);

                var transportSize = msg[offset + OffsetInPayload("S7WriteJobDataItem.ItemDataTransportSize")];
                message.SetAttribute(prefix + "ItemDataTransportSize", transportSize);

                var dataLength = (int)msg.GetSwap<ushort>(offset + OffsetInPayload("S7WriteJobDataItem.ItemDataLength"));

                if (transportSize != (byte)DataTransportSize.OctetString && transportSize != (byte)DataTransportSize.Real && transportSize != (byte)DataTransportSize.Bit)
                    dataLength = dataLength >> 3;

                message.SetAttribute(prefix + "ItemDataLength", (ushort)dataLength);
                message.SetAttribute(prefix + "ItemData", msg.Skip(offset + OffsetInPayload("S7WriteJobDataItem.ItemData")).Take(dataLength));

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
                msg.Add(message.GetAttribute(prefix + "VariableSpecification", (byte)0));
                var specLength = message.GetAttribute(prefix + "LengthOfAddressSpecification", (byte)0);
                msg.Add(specLength);
                msg.Add(message.GetAttribute(prefix + "SyntaxId", (byte)0));
                msg.Add(message.GetAttribute(prefix + "TransportSize", (byte)0));
                msg.AddRange(message.GetAttribute(prefix + "ItemSpecLength", (ushort)0).SetSwap());
                msg.AddRange(message.GetAttribute(prefix + "DbNumber", (ushort)0).SetSwap());
                msg.Add(message.GetAttribute(prefix + "Area", (byte)0));
                msg.AddRange(message.GetAttribute(prefix + "Address", new byte[] { 0x00, 0x00, 0x00 }));
            }

            for (var i = 0; i < itemCount; i++)
            {
                var prefix = string.Format("DataItem[{0}].", i);
                msg.Add(message.GetAttribute(prefix + "ItemDataReturnCode", (byte)0));
                var transportSize = message.GetAttribute(prefix + "ItemDataTransportSize", (byte)0);
                msg.Add(transportSize);
                
                var dataLength = (int)message.GetAttribute(prefix + "ItemDataLength", (ushort)0) * 1;   //Only bytes and Bits  are *1  !!!!!!!

                if (transportSize != (byte)DataTransportSize.OctetString && transportSize != (byte)DataTransportSize.Real && transportSize != (byte)DataTransportSize.Bit)
                    dataLength = dataLength * 8;
                msg.AddRange(((ushort)dataLength).SetSwap<ushort>());
                msg.AddRange(message.GetAttribute(prefix + "ItemData", new byte[0]));

                if (i != itemCount - 1 && msg.Count % 2 != 0)
                    msg.Add(0x00);
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
            if (parts.Length == 2 && parts[0] == "S7WriteJobItem")
            {
                return (int)Marshal.OffsetOf<S7WriteJobItem>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7WriteJobDataItem")
            {
                return (int)Marshal.OffsetOf<S7WriteJobDataItem>(parts[1]);
            }

            
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}