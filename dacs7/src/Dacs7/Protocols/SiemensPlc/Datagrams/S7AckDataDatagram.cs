// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7AckDataDatagram
    {
        public S7CommHeaderDatagram CommHeader { get; set; } = new S7CommHeaderDatagram
        {
            PduType = 0x03, //Ack_Data - > Should be a marker
        };

        public S7CommHeaderAckDatagram Error { get; set; } = new S7CommHeaderAckDatagram();
    }
}
