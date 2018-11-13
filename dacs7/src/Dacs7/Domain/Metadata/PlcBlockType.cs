using System;

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


    public enum PlcBlockLanguage : byte
    {
        Undefined = 0x00,
        Awl = 0x01,
        Kop = 0x02,
        Fup = 0x03,
        Scl = 0x04,
        Db = 0x05,
        Graph = 0x06,
        Sdb = 0x07,
        CpuDb = 0x08, /* DB was created from Plc program (CREAT_DB) */
        SdbAOR = 0x11,/* another SDB, don't know what it means, in SDB 1 and SDB 2, uncertain*/
        RoutingSdb = 0x12, /* another SDB, in SDB 999 and SDB 1000 (routing information), uncertain */
        Encrypted = 0x29  /* block is encrypted with S7-Block-Privacy */
    }


    [Flags]
    public enum PlcBlockAttributes : byte
    {
        None = 0,
        Linked = 1,
        StandardBlock = 2,
        KnowHowProtected = 4,
        NotRetain = 6
    }
}
