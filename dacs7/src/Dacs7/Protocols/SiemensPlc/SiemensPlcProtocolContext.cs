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


        public SiemensPlcProtocolContext()
        {

        }

        //public string Ip { get; set; } = "127.0.0.1";
        //public string LocalTsap { get; set; } = "0100";
        //public string RemoteTsap { get; set; } = CalcRemoteTsap(0x01, 0, 2);

        //private static string CalcRemoteTsap(ushort connectionType, int rack, int slot)
        //{
        //    var value = ((ushort)connectionType << 8) + (rack * 0x20) + slot;
        //    return string.Format("{0:X4}", value);
        //}



        public bool TryDetectDatagramType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length >= 10 &&
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


        public bool TryDetectJobType(Memory<byte> memory, out Type datagramType)
        {
            datagramType = null;
            return false;
        }

        public bool TryDetectAckType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > 12 &&
                memory.Span[0] == 0x32)
            {

                switch (memory.Span[12])  // Function Type
                {
                    case 0xf0:  // Setup communication
                        datagramType = typeof(S7CommAckDataDatagram);
                        return true;
                    case 0x04:  // Read Var
                        datagramType = typeof(S7ReadJobAckDatagram);
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
