using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace Dacs7.Protocols.S7
{
    public abstract class S7ProtocolPolicy : ProtocolPolicyBase
    {
        protected static readonly int MinimumSize = Marshal.SizeOf<S7CommHeader>();
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7CommHeader
        {
            public byte ProtocolId; // Telegram ID, always 32
            public byte PduType; // Header type 1 (Job), 7 (UserData), 3 (AckData)
            public ushort RedundancyIdentification; //0x0000
            public ushort ProtocolDataUnitReference;//0x0000   -> 0x0200 for Alarms in Job
            public ushort ParamLength; // Length of parameters which follow this header
            public ushort DataLength; // Length of data which follow the parameters
        }


        public S7ProtocolPolicy()
        {
            AddMarker(new byte[] {0x32}, 0, false);
        }


        public override int GetMinimumCountDataBytes()
        {
            return MinimumSize;
        }

        public override int GetDatagramLength(IEnumerable<byte> data)
        {
            var enumerable = data as byte[] ?? data.ToArray();
            return MinimumSize + ParamLength(enumerable) + DataLength(enumerable);
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            message.SetAttribute("ProtocolId", msg[OffsetInPayload("ProtocolId")]);
            message.SetAttribute("PduType", msg[OffsetInPayload("PduType")]);
            message.SetAttribute("RedundancyIdentification", msg.GetSwap<UInt16>(OffsetInPayload("RedundancyIdentification")));
            message.SetAttribute("ProtocolDataUnitReference", msg.GetSwap<UInt16>(OffsetInPayload("ProtocolDataUnitReference")));
            message.SetAttribute("ParamLength", msg.GetSwap<UInt16>(OffsetInPayload("ParamLength")));
            message.SetAttribute("DataLength", msg.GetSwap<UInt16>(OffsetInPayload("DataLength")));
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var pduType = message.GetAttribute("PduType", (byte) 0);
            var msg = new List<byte>
            {
                message.GetAttribute("ProtocolId", (byte) 0),
                message.GetAttribute("PduType", (byte) 0)
            };
            msg.AddRange(message.GetAttribute("RedundancyIdentification", UInt16.MinValue).SetSwap());
            msg.AddRange(message.GetAttribute("ProtocolDataUnitReference", UInt16.MinValue).SetSwap());
            msg.AddRange(message.GetAttribute("ParamLength", UInt16.MinValue).SetSwap());
            msg.AddRange(message.GetAttribute("DataLength", UInt16.MinValue).SetSwap());
            return msg;
        }

        public override IEnumerable<byte> CreateReply(IMessage message, object error = null)
        {
            throw new NotImplementedException();
        }


        protected static int PduType(IEnumerable<byte> data)
        {
            return data.Skip(OffsetInPayload("PduType")).First();
        }

        protected static int ParamLength(IEnumerable<byte> data)
        {
            return data.GetSwap<UInt16>(OffsetInPayload("ParamLength"));
        }

        protected static int DataLength(IEnumerable<byte> data)
        {
            return data.GetSwap<UInt16>(OffsetInPayload("DataLength"));
        }

        private static int OffsetInPayload(string aStructMemberName)
        {
            var parts = aStructMemberName.Split('.');
            var dot = aStructMemberName.Contains('.');
            if (!dot || parts.Length == 2 && parts[0] == "S7CommHeader")
            {
                return (int)Marshal.OffsetOf<S7CommHeader>(dot ? parts[1] : aStructMemberName);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }

    }
}