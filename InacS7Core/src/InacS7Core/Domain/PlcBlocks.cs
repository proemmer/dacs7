

using InacS7Core.Arch;

namespace InacS7Core.Domain
{
    internal class PlcBlocks : IPlcBlocks
    {
        public int Number { get; set; }
        public byte Flags { get; set; }
        public string Language { get; set; }
    }
}
