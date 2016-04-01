using InacS7Core.Arch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace InacS7Core.Helper.S7
{
    public class S7AckDataProtocolPolicy : S7ProtocolPolicy
    {
        protected static readonly int MinimumAckSize = MinimumSize + Marshal.SizeOf<S7CommHeaderAck>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7CommHeaderAck
        {
            public byte ErrorClass;
            public byte ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7JobParameter
        {
            public byte Function;
            public byte ParameterData;
        }

        public S7AckDataProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Domain.PduType.AckData }, 1, false);
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumAckSize;
        }

        public override int GetDatagramLength(IEnumerable<byte> data)
        {
            var enumerable = data as byte[] ?? data.ToArray();
            return MinimumAckSize + ParamLength(enumerable) + DataLength(enumerable);
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            message.SetAttribute("ErrorClass", msg[MinimumSize + OffsetInPayload("S7CommHeaderAck.ErrorClass")]);
            message.SetAttribute("ErrorCode", msg[MinimumSize + OffsetInPayload("S7CommHeaderAck.ErrorCode")]);

            var paramLength = message.GetAttribute("ParamLength", (ushort)0);
            var dataLength = message.GetAttribute("DataLength", (ushort)0);

            if (paramLength > 0)
            {
                message.SetAttribute("Function", msg[MinimumAckSize + OffsetInPayload("S7JobParameter.Function")]);
                if (paramLength > 1)
                    message.SetAttribute("ParameterData", msg.Skip(MinimumAckSize + OffsetInPayload("S7JobParameter.ParameterData")).Take((ushort)paramLength-1).ToArray());
            }

            if (dataLength > 0)
                message.SetAttribute("Data", msg.Skip(MinimumAckSize + OffsetInPayload("S7JobParameter.ParameterData") + paramLength).Take((ushort)dataLength).ToArray());
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            msg.Add(message.GetAttribute("ErrorClass", (byte)0));
            msg.Add(message.GetAttribute("ErrorCode", (byte)0));

            var paramLength = message.GetAttribute("ParamLength", (ushort)0);
            var dataLength = message.GetAttribute("DataLength", (ushort)0);

            if (paramLength > 0)
            {
                msg.Add(message.GetAttribute("Function", (byte)0));
                if (paramLength > 1)
                    msg.Add(message.GetAttribute("ParameterData", new byte { }));
            }

            if (dataLength > 0)
                msg.Add(message.GetAttribute("Data", new byte { }));
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
            if (parts.Length == 2 && parts[0] == "S7CommHeaderAck")
            {
                return (int)Marshal.OffsetOf<S7CommHeaderAck>( parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7JobParameter")
            {
                return (int)Marshal.OffsetOf<S7JobParameter>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
