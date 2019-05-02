// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7.Metadata
{
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
        CpuDb = 0x08, // DB was created from Plc program (CREAT_DB) 
        SdbAOR = 0x11,// another SDB, don't know what it means, in SDB 1 and SDB 2, uncertain
        RoutingSdb = 0x12, // another SDB, in SDB 999 and SDB 1000 (routing information), uncertain 
        Encrypted = 0x29  // block is encrypted with S7-Block-Privacy 
    }
}
