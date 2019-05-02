// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Domain
{
    internal enum UserDataFunctionGroup : byte
    {
        Prog = 0x1,
        Cyclic = 0x2,
        Block = 0x3,
        Cpu = 0x4,
        Sec = 0x5, //Security functions e.g. plc password 
        Time = 0x7
    }
}
