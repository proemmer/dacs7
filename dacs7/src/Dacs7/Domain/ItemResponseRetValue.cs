// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Helper;

namespace Dacs7
{
    public enum ItemResponseRetValue : byte
    {
        Reserved = 0x00,
        [Description("Hardware error")]
        HardwareFault = 0x01,

        [Description("Accessing the object not allowed")]
        AccessFault = 0x03,

        [Description("Invalid address")]
        OutOfRange = 0x05,       //the desired address is beyond limit for this PLC 

        [Description("Data type not supported")]
        NotSupported = 0x06,     //Type is not supported 

        [Description("Data type inconsistent")]
        SizeMismatch = 0x07,     //Data type inconsistent 

        [Description("Object does not exist")]
        DataError = 0x0a,        //the desired item is not available in the PLC, e.g. when trying to read a non existing DB

        [Description("Success")]
        Success = 0xFF,
    }



}