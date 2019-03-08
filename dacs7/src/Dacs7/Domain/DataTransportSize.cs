// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Domain
{

    public enum DataTransportSize
    {
        Null = 0,
        Bit = 3, //bit access, length is in bits 
        Byte = 4, //byte/word/dword access, length is in bits 
        Int = 5, //integer access, length is in bits 
        Dint = 6, //integer access, length is in bytes 
        Real = 7, //real access, length is in bytes 
        OctetString = 9 //octet string, length is in bytes 
    }
    
}
