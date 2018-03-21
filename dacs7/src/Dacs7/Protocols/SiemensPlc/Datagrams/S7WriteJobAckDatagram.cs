// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7WriteJobAckDatagram
    {

        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();


        public byte Function { get; set; } = 0x05; //Write Var


        public byte ItemCount { get; set; } = 0x00;


        public List<S7ItemDataWriteResult> Data { get; set; } = new List<S7ItemDataWriteResult>();
    }
}
