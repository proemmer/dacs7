// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Domain
{
    internal enum UserDataSubFunctionProg : byte
    {
        Reqdiagdata1 = 0x01,   // Request diag data (Type 1)   //Start online block view 
        Vartab1 = 0x02,          // VarTab                        //Variable table 
        Erase = 0x0c,            // Read diag data              //online block view 
        Readdiagdata = 0x0e,     // Remove diag data             //Stop online block view 
        Removediagdata = 0x0f,   // Erase" 
        Force = 0x10,            // Forces
        Reqdiagdata2 = 0x13      // Request diag data (Type 2)     //Start online block view 
    }
}
