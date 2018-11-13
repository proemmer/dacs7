using System;
using System.Buffers.Binary;
using System.Text;
using Dacs7.Helper;
using Dacs7.Metadata;

namespace Dacs7.Protocols.SiemensPlc
{

    internal class S7PlcBlockInfoAckDatagram
    {

        public S7UserDataDatagram UserData { get; set; }

        public ushort BlockType { get; set; }
        public ushort LengthOfInfo { get; set; }
        public byte AuthorOffset { get; set; }

        public ushort Unknown1 { get; set; }
        public byte Const1 { get; set; } = 0x070;
        public byte Const2 { get; set; } = 0x070;
        public byte Unknown2 { get; set; }

        public PlcBlockAttributes BlockFlags { get; set; }
        public PlcBlockLanguage BlockLanguage { get; set; }
        public PlcSubBlockType SubBlockType { get; set; }
        public ushort BlockNumber { get; set; }
        public uint LengthLoadMemory { get; set; }
        public uint BlockSecurity { get; set; }

        public DateTime LastCodeChange { get; set; }
        public DateTime LastInterfaceChange { get; set; }


        public ushort SSBLength { get; set; }
        public ushort ADDLength { get; set; }

        public ushort LocalDataSize { get; set; }
        public ushort CodeSize { get; set; }


        public string Author { get; set; }
        public string Family { get; set; }
        public string Name { get; set; }


        public int VersionHeaderMajor { get; set; }
        public int VersionHeaderMinor { get; set; }

        public byte Unknown3 { get; set; }

        public ushort Checksum { get; set; }

        public uint Reserved1 { get; set; }
        public uint Reserved2 { get; set; }



    
        public static string GetCheckSum(int offset, byte[] b)
        {
            return string.Format("0x{0:X2}{1:X2}", b[offset + 1], b[offset]);
        }

        public static string GetCheckSum(int checksum)
        {
            var b = BitConverter.GetBytes(checksum);
            return string.Format("0x{0:X2}{1:X2}", b[1], b[0]);
        }





        //public static Memory<byte> TranslateToMemory(S7PlcBlockInfoAckDatagram datagram)
        //{
        //    var result = S7UserDataDatagram.TranslateToMemory(datagram.UserData);

        //    S7UserDataParameter.TranslateToMemory(datagram.Parameter, result.Slice(datagram.Header.GetHeaderSize()));
        //    result.Span[offset++] = datagram.Data.ReturnCode;
        //    result.Span[offset++] = datagram.Data.TransportSize;
        //    BinaryPrimitives.WriteUInt16BigEndian(result.Slice(offset, 2).Span, datagram.Data.UserDataLength);
        //    datagram.Data.Data.CopyTo(result.Slice(offset + 2, datagram.Data.UserDataLength));
        //    return result;
        //}

        public static S7PlcBlockInfoAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var result = new S7PlcBlockInfoAckDatagram
            {
                UserData = S7UserDataDatagram.TranslateFromMemory(data)
            };

            var offset = 0;
            var span = result.UserData.Data.Data.Span;
            result.BlockType = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));   offset += 2;
            result.LengthOfInfo = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.Unknown1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.Const1 = span[offset++];
            result.Const2 = span[offset++];
            result.Unknown2 = span[offset++];
            result.BlockFlags = (PlcBlockAttributes)span[offset++];
            result.BlockLanguage = (PlcBlockLanguage)span[offset++];
            result.SubBlockType = (PlcSubBlockType)span[offset++];
            result.BlockNumber = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.LengthLoadMemory = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4)); offset += 4;
            result.BlockSecurity = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4)); offset += 4;   // ????
            result.LastCodeChange = GetDt(span.Slice(offset, 6)); offset += 6;
            result.LastInterfaceChange = GetDt(span.Slice(offset, 6)); offset += 6;
            result.SSBLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.ADDLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.LocalDataSize = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.CodeSize = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.Author = Encoding.ASCII.GetString(span.Slice(offset, 8).ToArray()); offset += 8;
            result.Family = Encoding.ASCII.GetString(span.Slice(offset, 8).ToArray()); offset += 8;
            result.Name = Encoding.ASCII.GetString(span.Slice(offset, 8).ToArray()); offset += 8;
            var version = span[offset++];
            result.VersionHeaderMajor = (version & 0xF0) >> 4;
            result.VersionHeaderMinor = (version & 0x0F);
            result.Unknown2 = span[offset++];
            result.Checksum = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2)); offset += 2;
            result.Reserved1 = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4)); offset += 4;
            result.Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4)); offset += 4;
            return result;
        }


        public static DateTime GetDt(Span<byte> b)
        {
            var dt = new DateTime(1984, 1, 1, 0, 0, 0, 0);
            dt = dt.AddMilliseconds(BinaryPrimitives.ReadUInt32BigEndian(b));
            dt = dt.AddDays(BinaryPrimitives.ReadUInt16BigEndian(b.Slice(4)));
            return dt;
        }
    }
}
