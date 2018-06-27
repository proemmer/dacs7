namespace Dacs7.Protocols.Fdl
{
    internal class RemoteAddress
    {
        public byte Station { get; set; }
        public byte Segment { get; set; } = 0xff;  // No Segment
    }
}
