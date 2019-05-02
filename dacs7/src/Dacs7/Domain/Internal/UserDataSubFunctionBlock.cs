// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Domain
{
    internal enum UserDataSubFunctionBlock : byte
    {
        List = 0x01,   //List blocks
        ListType = 0x02,  // List blocks of type
        BlockInfo = 0x03  //Get block info
    }
}
