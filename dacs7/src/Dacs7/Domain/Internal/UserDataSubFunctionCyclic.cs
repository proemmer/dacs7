// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Domain
{
    internal enum UserDataSubFunctionCyclic : byte
    {
        Mem = 0x01,             //"Memory"                        //read data from memory (DB/M/etc.) 
        Unsubscribe = 0x04,     //"unsubscribe"                   //unsubcribe (disable) cyclic data 
    }
}
