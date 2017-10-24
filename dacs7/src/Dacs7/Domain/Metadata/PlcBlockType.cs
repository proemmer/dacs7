namespace Dacs7.Metadata
{
    public enum PlcBlockType : byte
    {
        Ob = 0x38,
        Db = 0x41,
        Sdb = 0x42,
        Fc = 0x43,
        Sfc = 0x44,
        Fb = 0x45,
        Sfb = 0x46
    }

    public enum PlcSubBlockType : byte
    {
        Ob = 0x08,
        Db = 0x0a,
        Sdb = 0x0b,
        Fc = 0x0c,
        Sfc = 0x0d,
        Fb = 0x0e,
        Sfb = 0x0f
    }
}
