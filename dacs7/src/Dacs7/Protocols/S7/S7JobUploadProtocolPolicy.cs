using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dacs7.Helper;
using Dacs7.Arch;

namespace Dacs7.Helper
{
    public class S7JobUploadProtocolPolicy : S7ProtocolPolicy
    {
        private static readonly int MinimumJobUploadSize = MinimumSize + 8;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7UploadJobParameter
        {
            public byte Function;
            public byte Reserved; // 0x00
            public ushort UploadErrorCode; // 0x0000
            public uint Reserved2; // 0x00000000

            public byte LengthPart1; // 0x09
            public byte FileIdentifier; // 0x5f  ->  '_'
            public byte Unknown1;
            public byte BlockType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] BlockNumber;
            public byte DestFilesystem;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7DownloadJobParameter
        {
            public byte LengthPart2; // 0x09
            public byte Unknown2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] LoadMemLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] MC7Length;
        }

        

        public S7JobUploadProtocolPolicy()
        {
            AddMarker(new byte[] { (byte)Dacs7.Domain.PduType.Job }, 1, false);
            AddMarker(new byte[] { 0x00, 0x12 }, 6, false);
            AddMarker(new byte[] { 0x00, 0x00 }, 8, false);
            AddMarker(new byte[] { 0x1d }, 10, false);
        }

        public override int GetMinimumCountDataBytes()
        {
            return MinimumJobUploadSize;
        }

        public override void SetupMessageAttributes(IMessage message)
        {
            base.SetupMessageAttributes(message);
            var msg = (message.GetRawMessage() as IEnumerable<byte>).ToArray();
            var parentOffset = MinimumSize;

            message.SetAttribute("Function", msg[parentOffset + OffsetInPayload("S7UploadJobParameter.Function")]);

            var itemCount = msg.GetSwap<ushort>(parentOffset + OffsetInPayload("S7UploadJobParameter.UploadErrorCode"));
            message.SetAttribute("ParamErrorCode", itemCount);

        }

        public override IEnumerable<byte> CreateRawMessage(IMessage message)
        {
            var msg = base.CreateRawMessage(message).ToList();
            var paramLength = message.GetAttribute("ParamLength",(ushort)0);

            msg.Add(message.GetAttribute("Function", (byte)0));
            msg.Add(message.GetAttribute("Reserved", (byte)0));
            msg.AddRange(message.GetAttribute("ParamErrorCode", ((ushort)0).SetSwap()));
            msg.AddRange(message.GetAttribute("Reserved2", (uint)0).SetSwap());

            if (paramLength >= 18) //Up and Download
            {
                msg.Add(message.GetAttribute("LengthPart1", (byte)0));
                msg.Add(message.GetAttribute("FileIdentifier", (byte)0));
                msg.Add(message.GetAttribute("Unknown1", (byte)0));
                msg.Add(message.GetAttribute("BlockType", (byte)0));
                msg.AddRange(message.GetAttribute("BlockNumber", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }));
                msg.Add(message.GetAttribute("DestFilesystem", (byte)0));
            }

            if (paramLength >= 32) //Download
            {
                msg.Add(message.GetAttribute("LengthPart2", (byte)0));
                msg.Add(message.GetAttribute("Unknown2", (byte)0));

                msg.AddRange(message.GetAttribute("LoadMemLength", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }));
                msg.AddRange(message.GetAttribute("MC7Length", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }));
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
            if (parts.Length == 2 && parts[0] == "S7UploadJobParameter")
            {
                return (int)Marshal.OffsetOf<S7UploadJobParameter>(parts[1]);
            }
            if (parts.Length == 2 && parts[0] == "S7DownloadJobParameter")
            {
                return (int)Marshal.OffsetOf<S7DownloadJobParameter>(parts[1]);
            }
            throw new ArgumentException("Argument must be in format <Classname.Property>", aStructMemberName);
        }
    }
}
