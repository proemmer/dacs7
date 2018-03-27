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

        public static readonly int MinimumDataSize = 10;
        public static readonly int MinimumAckDetectionSize = MinimumDataSize + 2;



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

























        public static ReadItemSpecification CreateBitReadItemSpec(string selector, ushort db, ushort offset, int bit)
        {
            return new ReadItemSpecification 
            {
                Area = (PlcArea)Enum.Parse(typeof(PlcArea), selector),
                DbNumber = db,
                Offset = (ushort)(offset + bit),
                Length = 1,
                VarType = typeof(bool)
            };
        }

        public static ReadItemSpecification CreateWordReadItemSpec(string selector, ushort db, ushort offset)
        {
            return new ReadItemSpecification   
            {
                Area = (PlcArea)Enum.Parse(typeof(PlcArea), selector),
                DbNumber = db,
                Offset = offset,
                Length = 1,
                VarType = typeof(short)
            };
        }

        public static ReadItemSpecification CreateByteReadItemSpec(string selector, ushort db, ushort offset, ushort length)
        {
            return new ReadItemSpecification  
            {
                Area = (PlcArea)Enum.Parse(typeof(PlcArea), selector),
                DbNumber = db,
                Offset = offset,
                Length = length,
                VarType = typeof(byte)
            };
        }

        public static WriteItemSpecification CreateBitWriteItemSpec(string selector, ushort db, ushort offset, int bit)
        {
            return new WriteItemSpecification
            {
                Area = (PlcArea)Enum.Parse(typeof(PlcArea), selector),
                DbNumber = db,
                Offset = (ushort)(offset + (int)Convert.ChangeType(bit, typeof(int))),
                Length = 1,
                VarType = typeof(bool)
            };
        }
    }
}
