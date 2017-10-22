using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dacs7.Protocols.S7
{
    public class S7InvocationProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumJobSetupSize = MinimumSize + Marshal.SizeOf<S7InvocationParameter>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7InvocationParameter
        {
            public byte Function; // 0xf0
            public byte Reserved; // 0x00
            public byte LengthPart2; 
            public byte[] Pi; 
        }


        public S7InvocationProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Dacs7.Domain.PduType.Job }, 1, false);
            AddMarker(new byte[] { 0x00, 0x10 }, 6, false); // param length of 16
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumSize;
        }


        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            message.SetAttribute("Function", msg[OffsetInPayload("S7InvocationParameter.Function")]);
            message.SetAttribute("Reserved", msg[OffsetInPayload("S7InvocationParameter.Reserved")]);
            var size = msg.GetSwap<byte>(OffsetInPayload("S7InvocationParameter.LengthPart2"));
            message.SetAttribute("LengthPart2", size);
            message.SetAttribute("Pi", msg.SubArray(OffsetInPayload("S7InvocationParameter.Pi"), size));
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            msg.Add(message.GetAttribute("Function", (byte)0));
            msg.AddRange(message.GetAttribute("Reserved", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }));
            msg.Add(message.GetAttribute("LengthPart2", (byte)0x00));
            msg.AddRange(message.GetAttribute("Pi", new byte[0]));
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
            if (parts.Length == 2 && parts[0] == "S7InvocationParameter")
            {
                return (int)Marshal.OffsetOf<S7InvocationParameter>( parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
