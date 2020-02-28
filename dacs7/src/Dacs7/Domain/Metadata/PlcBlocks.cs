


namespace Dacs7.Metadata
{
    internal sealed class PlcBlock : IPlcBlock
    {
        public int Number { get; set; }
        public byte Flags { get; set; }
        public string Language { get; set; }
    }
}
