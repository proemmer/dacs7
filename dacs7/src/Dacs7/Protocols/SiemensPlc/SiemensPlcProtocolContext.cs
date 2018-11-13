using Dacs7.Domain;
using System;

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
        private const int JobFunctionCodeOffset = MinimumDataSize;
        private const int AckDataFunctionCodeOffset = MinimumAckDetectionSize;

        // 0x32 = S7comm
        // 0x72 = S7commPlus (1200/1500)
        private const byte Prefix = 0x32;

        public const int ReadHeader = 10;        // header for each telegram
        public const int ReadParameter = 2;     // header for each telegram
        public const int ReadItemSize = 12;      // lenght for each address specification

        public const int ReadAckHeader = 12;     // 12 Header   (ACK Header)
        public const int ReadAckParameter = 2;      // header for each telegram
        public const int ReadItemAckHeader = 4;  // header for each item


        public const int WriteHeader = 10;
        public const int WriteParameter = 2;
        public const int WriteParameterItem = 12; // each item
        public const int WriteDataItem = 4;       // each item + length



        public const int WriteItemHeader = 28; // 28 Header and some other data

        public ushort MaxAmQCalling { get; set; } = 10; // -> used for negotiation
        public ushort MaxAmQCalled { get; set; } = 10; // -> used for negotiation
        public ushort PduSize { get; set; } = 960;  // defautl pdu size -> used for negotiation
        public int Timeout { get; set; }


        public UInt16 ReadItemMaxLength { get { return (UInt16)(PduSize - 18); } }  //18 Header and some other data    // in the result message
        public UInt16 WriteItemMaxLength { get { return (UInt16)(PduSize - 28); } } //28 Header and some other data







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
                    case PduType.UserData: // ACKData
                        return TryDetectUserDataDataType(memory, out datagramType);
                }

            }
            datagramType = null;
            return false;
        }

        private bool TryDetectJobType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > MinimumDataSize)
            {
                switch ((FunctionCode)memory.Span[JobFunctionCodeOffset])  // Function Type
                {
                    case FunctionCode.SetupComm:
                        datagramType = typeof(S7CommSetupDatagram);
                        return true;
                    case FunctionCode.ReadVar:   
                        datagramType = typeof(S7ReadJobDatagram);
                        return true;
                    case FunctionCode.WriteVar:
                        datagramType = typeof(S7WriteJobDatagram);
                        return true;
                }

            }
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


        private bool TryDetectUserDataDataType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > 22)
            {
                // currently we do not support other types
                switch ((UserDataSubFunctionBlock)memory.Span[16])  // Function Type
                {
                    case UserDataSubFunctionBlock.BlockInfo:  // Write Var
                        datagramType = typeof(S7PlcBlockInfoAckDatagram);
                        return true;
                }

            }
            datagramType = null;
            return false;
        }
    }
}
