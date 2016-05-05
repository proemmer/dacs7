using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dacs7.Arch;
using Dacs7.Helper;

namespace Dacs7.Helper
{
    public class S7UserDataProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumUserDataSize = MinimumSize + Marshal.SizeOf<S7UserDataParameter>();
        private const int ParamHeaderLength = 4;
        private const int UserDataHeaderLength = 4;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7UserDataParameter
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] ParamHeader; // Always 0x00 0x01 0x12
            public byte ParamDataLength; // par len 0x04 or 0x08
            public byte Unknown; // unknown

            public byte TypeAndGroup;
            // type and group  (4 bits type and 4 bits group)     0000 ....   = Type: Follow  (0) // .... 0100   = SZL functions (4)

            public byte SubFunction; // subfunction
            public byte SequenceNumber; // sequence
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7UserDataParameterExt
        {
            public byte DataUnitReferenceNumber; 
            public byte LastDataUnit; 
            public ushort ParamErrorCode;               // present if plen=0x08 (S7 manager online functions)  -> we do not need this at the moment
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7UserData
        {
            public byte ReturnCode; 
            public byte TransportSize; 
            public ushort UserDataLength;          
        }

        public S7UserDataProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Dacs7.Domain.PduType.UserData }, 1, false);
            AddMarker(new byte[] {
                0x00,
                0x01,
                0x12 }, MinimumSize, false);
        }


        public override int GetMinimumCountDataBytes()
        {
            return MinimumUserDataSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            
            var parentOffset = MinimumSize;
            var paramLength = msg[parentOffset + OffsetInPayload("S7UserDataParameter.ParamDataLength")];
            var offset = parentOffset;
            if (paramLength > 0)
            {
                message.SetAttribute("ParamHeader", msg.Skip(parentOffset + OffsetInPayload("S7UserDataParameter.ParamHeader")).Take(3));
                message.SetAttribute("ParamDataLength", paramLength);
                message.SetAttribute("Unknown", msg[parentOffset + OffsetInPayload("S7UserDataParameter.Unknown")]);
                message.SetAttribute("TypeAndGroup", msg[parentOffset + OffsetInPayload("S7UserDataParameter.TypeAndGroup")]);

                message.SetAttribute("$Type", (msg[parentOffset + OffsetInPayload("S7UserDataParameter.TypeAndGroup")] & 0xF0) >> 4);
                message.SetAttribute("$Group", msg[parentOffset + OffsetInPayload("S7UserDataParameter.TypeAndGroup")] & 0x0F);

                message.SetAttribute("SubFunction", msg[parentOffset + OffsetInPayload("S7UserDataParameter.SubFunction")]);


                message.SetAttribute("SequenceNumber", msg[parentOffset + OffsetInPayload("S7UserDataParameter.SequenceNumber")]);

                offset = parentOffset + Marshal.SizeOf(typeof (S7UserDataParameter));
                if (paramLength == 8)
                {
                    message.SetAttribute("DataUnitReferenceNumber", msg[offset + OffsetInPayload("S7UserDataParameterExt.DataUnitReferenceNumber")]);
                    message.SetAttribute("LastDataUnit", msg[offset + OffsetInPayload("S7UserDataParameterExt.LastDataUnit")] == 0x00);
                    message.SetAttribute("ParamErrorCode", msg.GetSwap<ushort>(offset + OffsetInPayload("S7UserDataParameterExt.ParamErrorCode")));
                }
                offset = parentOffset + ParamHeaderLength + paramLength;
            }

            message.SetAttribute("ReturnCode", msg[offset + OffsetInPayload("S7UserData.ReturnCode")]);
            message.SetAttribute("TransportSize", msg[offset + OffsetInPayload("S7UserData.TransportSize")]);
            message.SetAttribute("UserDataLength", msg.GetSwap<ushort>(offset + OffsetInPayload("S7UserData.UserDataLength")));
            message.SetAttribute("SSLData", msg.Skip(offset + UserDataHeaderLength).ToArray());
        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();

            msg.AddRange(message.GetAttribute("ParamHeader", new byte[] {0x00, 0x01, 0x12}));
            var paramLength = message.GetAttribute("ParamDataLength", (byte) 0);

            if (paramLength > 0)
            {
                msg.Add(paramLength);
                msg.Add(message.GetAttribute("Unknown", (byte) 0));
                msg.Add(message.GetAttribute("TypeAndGroup", (byte) 0));
                msg.Add(message.GetAttribute("SubFunction", (byte) 0));
                msg.Add(message.GetAttribute("SequenceNumber", (byte) 0));

                if (paramLength == 8)
                {
                    msg.Add(message.GetAttribute("DataUnitReferenceNumber", (byte) 0));
                    msg.Add(message.GetAttribute("LastDataUnit", (byte) 0));
                    msg.AddRange(message.GetAttribute("ParamErrorCode", (ushort) 0).SetSwap());
                }
            }

            msg.Add(message.GetAttribute("ReturnCode", (byte)0));
            msg.Add(message.GetAttribute("TransportSize", (byte)0));
            msg.AddRange(message.GetAttribute("UserDataLength", (ushort)0).SetSwap());
            msg.AddRange(message.GetAttribute("SSLData", new Byte[0]));
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
            if (parts.Length == 2 && parts[0] == "S7UserDataParameter")
            {
                return (int)Marshal.OffsetOf<S7UserDataProtocolPolicy.S7UserDataParameter>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7UserDataParameterExt")
            {
                return (int)Marshal.OffsetOf<S7UserDataProtocolPolicy.S7UserDataParameterExt>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7UserData")
            {
                return (int)Marshal.OffsetOf<S7UserDataProtocolPolicy.S7UserData>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
