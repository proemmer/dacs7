// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7CommHeaderDatagram
    {

        public byte ProtocolId { get; set; } = 0x32;


        public byte PduType { get; set; }

        public UInt16 RedundancyIdentification { get; set; }

        public UInt16 ProtocolDataUnitReference { get; set; }

        public UInt16 ParamLength { get; set; }

        public UInt16 DataLength { get; set; }
    }
}
