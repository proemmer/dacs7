using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Protocols.SiemensPlc
{
    /// <summary>
    /// Context class for the protocol instance
    /// Contains all properties for the protocol
    /// </summary>
    public class SiemensPlcProtocolContext
    {

        public string Name { get; set; }
        public ushort MaxParallelJobs { get; set; } = 10;
        public ushort PduSize { get; set; } = 960;

        public UInt16 ReadItemMaxLength { get { return (UInt16)(PduSize - 18); } }  //18 Header and some other data    // in the result message
        public UInt16 WriteItemMaxLength { get { return (UInt16)(PduSize - 28); } } //28 Header and some other data

        public static readonly int MinimumDataSize = 10;
        public static readonly int MinimumAckDetectionSize = MinimumDataSize + 2;

        public bool OptimizeReadAccess { get; set; }
        public bool OptimizeWriteAccess { get; set; }

        public bool TryDetectDatagramType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length >= MinimumDataSize &&
               memory.Span[0] == 0x32)
            {

                switch (memory.Span[1])  // PDU Type
                {
                    case 0x01:  // JOB
                        return TryDetectJobType(memory, out datagramType);
                    case 0x03: // ACK
                        return TryDetectAckType(memory, out datagramType);
                }

            }
            datagramType = null;
            return false;
        }


        private bool TryDetectJobType(Memory<byte> memory, out Type datagramType)
        {
            datagramType = null;
            return false;
        }

        private bool TryDetectAckType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > MinimumAckDetectionSize &&
                memory.Span[0] == 0x32)
            {

                switch (memory.Span[12])  // Function Type
                {
                    case 0xf0:  // Setup communication
                        datagramType = typeof(S7CommSetupAckDataDatagram);
                        return true;
                    case 0x04:  // Read Var
                        datagramType = typeof(S7ReadJobAckDatagram);
                        return true;
                    case 0x05:  // Write Var
                        datagramType = typeof(S7WriteJobAckDatagram);
                        return true;
                }

            }
            datagramType = null;
            return false;
        }

    }
}
