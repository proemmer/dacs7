using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dacs7.Helper
{
    public class S7JobSetupProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumJobSetupSize = MinimumSize + Marshal.SizeOf<S7SetupJobParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7SetupJobParameter
        {
            public byte Function; // 0xf0
            public byte Reserved; // 0x00
            public ushort MaxAmQCalling; // Max AmQ (parallel jobs with ack) calling: 2
            public ushort MaxAmQCalled; // Max AmQ (parallel jobs with ack) called: 2
            public ushort PduLength; // PduLength
        }


        public S7JobSetupProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Dacs7.Domain.PduType.Job }, 1, false);
            AddMarker(new byte[] { 0xf0 }, MinimumSize, false);
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumSize;
        }


        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            message.SetAttribute("Function", msg[OffsetInPayload("S7SetupJobParameter.Function")]);
            message.SetAttribute("Reserved", msg[OffsetInPayload("S7SetupJobParameter.Reserved")]);
            message.SetAttribute("MaxAmQCalling", msg.GetSwap<UInt16>(OffsetInPayload("S7SetupJobParameter.MaxAmQCalling")));
            message.SetAttribute("MaxAmQCalled", msg.GetSwap<UInt16>(OffsetInPayload("S7SetupJobParameter.MaxAmQCalled")));
            message.SetAttribute("PduLength", msg.GetSwap<UInt16>(OffsetInPayload("S7SetupJobParameter.PduLength")));
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            msg.Add(message.GetAttribute("Function", (byte)0));
            msg.Add(message.GetAttribute("Reserved", (byte)0));
            msg.AddRange(message.GetAttribute("MaxAmQCalling", UInt16.MinValue).SetSwap());
            msg.AddRange(message.GetAttribute("MaxAmQCalled", UInt16.MinValue).SetSwap());
            msg.AddRange(message.GetAttribute("PduLength", UInt16.MinValue).SetSwap());
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
            if (parts.Length == 2 && parts[0] == "S7SetupJobParameter")
            {
                return (int)Marshal.OffsetOf<S7SetupJobParameter>( parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
