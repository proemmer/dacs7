// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7.Domain
{
    internal enum ItemDataTransportSize
    {
        //types of 1 byte length 
        Bit = 1,
        Byte = 2,
        Char = 3,
        //types of 2 bytes length 
        Word = 4,
        Int = 5,
        //types of 4 bytes length 
        Dword = 6,
        Dint = 7,
        Real = 8,
        //Special types 
        Date = 9,
        Tod = 10,
        Time = 11,
        S5Time = 12,
        Dt = 15,
        //Timer or counter 
        Counter = 28,
        Timer = 29,
        IecCounter = 30,
        IecTimer = 31,
        HsCounter = 32
    }

}
