using Dacs7.Metadata;
using System;

namespace Dacs7.Metadata
{
    public interface IPlcBlockInfo
    {

        PlcBlockAttributes BlockFlags { get; }
        PlcBlockLanguage BlockLanguage { get; }
        PlcSubBlockType SubBlockType { get; }
        ushort BlockNumber { get; }
        uint LengthLoadMemory { get; }
        uint BlockSecurity { get; }

        DateTime LastCodeChange { get; }
        DateTime LastInterfaceChange { get; }


        ushort SSBLength { get; }
        ushort ADDLength { get; }

        ushort LocalDataSize { get; }
        ushort CodeSize { get; }


        string Author { get; }
        string Family { get; }
        string Name { get; }


        int VersionHeaderMajor { get; }
        int VersionHeaderMinor { get; }



        ushort Checksum { get; }


    }
}
