using System;
using System.Text;
using Dacs7.Helper;

namespace Dacs7.Metadata
{
    public enum BlockSecurity
    {
        Off = 0,
        KnowHowProtected = 3
    }

    internal class PlcBlockInfo : IPlcBlockInfo
    {
        public string Version { get; set; }
        public string VersionHeader { get; set; }
        public string Attribute { get; set; }
        public string Author { get; set; }
        public string Family { get; set; }
        public string Name { get; set; }
        public string Checksum { get; set; }
        public string BlockLanguage { get; set; }
        public string BlockType { get; set; }
        public int BlockNumber { get; set; }
        public double Length { get; set; }
        public DateTime LastCodeChange { get; set; }
        public DateTime LastInterfaceChange { get; set; }
        public string Password { get; set; }
        public int InterfaceSize { get; set; }
        public int LocalDataSize { get; set; }
        public int CodeSize { get; set; }


        public static DateTime GetDateTime(UInt16 days, UInt32 milliseconds)
        {
            var dt = new DateTime(1984, 1, 1, 0, 0, 0, 0);
            dt = dt.AddMilliseconds(milliseconds);
            dt = dt.AddDays(days);
            return dt;
        }

        public static DateTime GetDt(byte[] b)
        {
            var dt = new DateTime(1984, 1, 1, 0, 0, 0, 0);
            dt = dt.AddMilliseconds(b.GetSwap<UInt32>());
            dt = dt.AddDays(b.GetSwap<UInt16>(4));
            return dt;
        }

        public static DateTime GetDt(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6)
        {
            var dt = new DateTime(1984, 1, 1, 0, 0, 0, 0);
            dt = dt.AddMilliseconds((b1 * 0x1000000) + (b2 * 0x10000) + (b3 * 0x100) + b4);
            dt = dt.AddDays((b5 * 0x100) + b6);
            return dt;
        }

        public static string GetString(int start, int count, byte[] bd)
        {
            int i;

            var result = "";
            for (i = 0; i <= count - 1; i++)
                result = result + Convert.ToChar(bd[start + i]);
            result = result.Trim();

            return result;
        }

        public static ushort GetU16(byte[] b, int pos, bool forceNoSwap = false)
        {
            if (!forceNoSwap && BitConverter.IsLittleEndian)
            {
                var b1 = new byte[2];
                b1[1] = b[pos + 0];
                b1[0] = b[pos + 1];
                return BitConverter.ToUInt16(b1, 0);
            }
            return BitConverter.ToUInt16(b, pos);
        }

        public static uint GetU32(byte[] b, int pos)
        {
            if (BitConverter.IsLittleEndian)
            {
                var b1 = new byte[4];
                b1[3] = b[pos];
                b1[2] = b[pos + 1];
                b1[1] = b[pos + 2];
                b1[0] = b[pos + 3];
                return BitConverter.ToUInt32(b1, 0);
            }
            return BitConverter.ToUInt32(b, pos);
        }

        public static string GetVersion(byte b)
        {
            return Convert.ToString((b & 0xF0) >> 4) + "." + Convert.ToString(b & 0x0F);
        }

        public static string GetCheckSum(int offset, byte[] b)
        {
            return string.Format("0x{0:X2}{1:X2}", b[offset + 1], b[offset]);
        }

        public static string GetCheckSum(int checksum)
        {
            var b = BitConverter.GetBytes(checksum);
            return string.Format("0x{0:X2}{1:X2}", b[1], b[0]);
        }

        public static string GetAttributes(byte b)
        {
            var sb = new StringBuilder();
            if (!GetBit(b, 0))
                sb.Append("unlinked;");
            if (GetBit(b, 1))
                sb.Append("standard block and know how protect;");
            if (GetBit(b, 3))
                sb.Append("know how protect;");
            if (GetBit(b, 5))
                sb.Append("not retain;");
            return sb.ToString();
        }

        public static string GetLanguage(byte b)
        {
            switch (b)
            {
                case 0x00:
                    return "Not defined";
                case 0x01:
                    return "AWL";
                case 0x02:
                    return "KOP";
                case 0x03:
                    return "FUP";
                case 0x04:
                    return "SCL";
                case 0x05:
                    return "DB";
                case 0x06:
                    return "GRAPH";
                case 0x07:
                    return "SDB";
                case 0x08:
                    return "CPU-DB";                        /* DB was created from Plc program (CREAT_DB) */
                case 0x11:
                    return "SDB (after overall reset)";     /* another SDB, don't know what it means, in SDB 1 and SDB 2, uncertain*/
                case 0x12:
                    return "SDB (routing)";                 /* another SDB, in SDB 999 and SDB 1000 (routing information), uncertain */
                case 0x29:
                    return "Encrypt";                       /* block is encrypted with S7-Block-Privacy */
            }
            return string.Empty;
        }

        public static string GetPlcBlockType(byte b)
        {
            switch (b)
            {
                case 0x08:
                    return "OB";
                case 0x0A:
                    return "DB";
                case 0x0b:
                    return "SDB";
                case 0x0C:
                    return "FC";
                case 0x0D:
                    return "SFC";
                case 0x0E:
                    return "FB";
                case 0x0F:
                    return "SFB";
            }
            return string.Empty;
        }

        public static bool GetBit(byte data, int bit)
        {
            // Shift the bit to the first location
            data = (byte)(data >> bit);

            // Isolate the value
            return (data & 1) == 1;
        }
    }
}
