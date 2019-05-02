// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

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
}
