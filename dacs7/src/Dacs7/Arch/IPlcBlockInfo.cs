using System;

namespace Dacs7.Arch
{
    public interface IPlcBlockInfo
    {
        string Version { get; set; }
        string VersionHeader { get; set; }
        string Attribute { get; set; }
        string Author { get; set; }
        string Family { get; set; }
        string Name { get; set; }
        string Checksum { get; set; }
        string BlockLanguage { get; set; }
        string BlockType { get; set; }
        int BlockNumber { get; set; }
        double Length { get; set; }
        DateTime LastCodeChange { get; set; }
        DateTime LastInterfaceChange { get; set; }
        string Password { get; set; }
        int InterfaceSize { get; set; }
        int LocalDataSize { get; set; }
        int CodeSize { get; set; }
    }
}
