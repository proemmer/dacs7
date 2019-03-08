// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.


namespace Dacs7.Domain
{

    internal enum UserDataFunctionGroups : byte
    {
        Read = 0x1, //Read clock
        Set = 0x2,  //Set clock
        ReadDf = 0x3,  //Read clock (following)
        Set2 = 0x4, //Set clock
    }
}
