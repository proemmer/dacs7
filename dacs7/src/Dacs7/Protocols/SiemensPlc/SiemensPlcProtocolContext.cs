using Dacs7.Domain;
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
    internal class SiemensPlcProtocolContext
    {
        private const int MinimumDataSize = 10;
        private const int MinimumAckDetectionSize = MinimumDataSize + 2;
        private const int PduTypeOffset = 1;
        private const int AckDataFunctionCodeOffset = MinimumAckDetectionSize;
        private const byte Prefix = 0x32;

        public ushort MaxParallelJobs { get; set; } = 10; // -> used for negotiation
        public ushort PduSize { get; set; } = 960;  // defautl pdu size -> used for negotiation

        public UInt16 ReadItemMaxLength { get { return (UInt16)(PduSize - 18); } }  //18 Header and some other data    // in the result message
        public UInt16 WriteItemMaxLength { get { return (UInt16)(PduSize - 28); } } //28 Header and some other data



        public bool OptimizeReadAccess { get; set; }  // TODO: Not implemented yet
        public bool OptimizeWriteAccess { get; set; } // TODO: Not implemented yet






        public bool TryDetectDatagramType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length >= MinimumDataSize &&
               memory.Span[0] == Prefix)
            {

                switch ((PduType)memory.Span[PduTypeOffset])  // PDU Type
                {
                    case PduType.Job:  // JOB
                        return TryDetectJobType(memory, out datagramType);
                    case PduType.AckData: // ACKData
                        return TryDetectAckDataType(memory, out datagramType);
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

        private bool TryDetectAckDataType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > MinimumAckDetectionSize)
            {
                switch ((FunctionCode)memory.Span[AckDataFunctionCodeOffset])  // Function Type
                {
                    case FunctionCode.SetupComm:  // Setup communication
                        datagramType = typeof(S7CommSetupAckDataDatagram);
                        return true;
                    case FunctionCode.ReadVar:  // Read Var
                        datagramType = typeof(S7ReadJobAckDatagram);
                        return true;
                    case FunctionCode.WriteVar:  // Write Var
                        datagramType = typeof(S7WriteJobAckDatagram);
                        return true;
                }

            }
            datagramType = null;
            return false;
        }

    }
}
