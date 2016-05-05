namespace Dacs7.Domain
{
    public enum PduType : byte
    {
        Job = 0x01,
        Ack = 0x02,
        AckData = 0x03,
        UserData = 0x07
    }
}
