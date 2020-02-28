// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Text;

namespace Dacs7.Metadata
{

    public class PlcBlockInfo : IPlcBlockInfo
    {
        public PlcBlockAttributes BlockFlags { get; internal set; }
        public PlcBlockLanguage BlockLanguage { get; internal set; }
        public PlcSubBlockType SubBlockType { get; internal set; }
        public ushort BlockNumber { get; internal set; }
        public uint LengthLoadMemory { get; internal set; }
        public uint BlockSecurity { get; internal set; }

        public DateTime LastCodeChange { get; internal set; }
        public DateTime LastInterfaceChange { get; internal set; }


        public ushort SSBLength { get; internal set; }
        public ushort ADDLength { get; internal set; }

        public ushort LocalDataSize { get; internal set; }
        public ushort CodeSize { get; internal set; }


        public string Author { get; internal set; }
        public string Family { get; internal set; }
        public string Name { get; internal set; }


        public int VersionHeaderMajor { get; internal set; }
        public int VersionHeaderMinor { get; internal set; }


        public ushort Checksum { get; internal set; }


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
                case 0x1a:
                    return "S7-Pdiag";
                case 0x1d:
                    return "SFM";                     
            }
            return string.Empty;
        }

    }
}
