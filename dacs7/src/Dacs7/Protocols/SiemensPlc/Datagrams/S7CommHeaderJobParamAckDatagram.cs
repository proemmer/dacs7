// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7CommHeaderJobParamAckDatagram
    {
        public byte Function { get; set; } = 0xF0; //Setup communication
        public byte Reserved { get; set; } = 0x00;

        public UInt16 MaxAmQCalling { get; set; }

        public UInt16 MaxAmQCalled { get; set; }

        public UInt16 PduLength { get; set; }





    }
}
