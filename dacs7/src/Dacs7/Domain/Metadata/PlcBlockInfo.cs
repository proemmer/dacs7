// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

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

    }
}
