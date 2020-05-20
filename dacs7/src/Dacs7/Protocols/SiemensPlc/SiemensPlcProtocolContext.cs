// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using System;

namespace Dacs7.Protocols.SiemensPlc
{
    /// <summary>
    /// Context class for the protocol instance
    /// Contains all properties for the protocol
    /// </summary>
    internal sealed class SiemensPlcProtocolContext
    {
        private const int _minimumDataSize = 10;
        private const int _minimumAckDetectionSize = _minimumDataSize + 2;
        private const int _pduTypeOffset = 1;
        private const int _jobFunctionCodeOffset = _minimumDataSize;
        private const int _ackDataFunctionCodeOffset = _minimumAckDetectionSize;

        // 0x32 = S7comm
        // 0x72 = S7commPlus (1200/1500)
        private const byte _prefix = 0x32;

        public const int ReadHeader = 10;        // header for each telegram
        public const int ReadParameter = 2;     // header for each telegram
        public const int ReadItemSize = 12;      // lenght for each address specification

        public const int ReadAckHeader = 12;     // 12 Header   (ACK Header)
        public const int ReadAckParameter = 2;      // header for each telegram
        public const int ReadItemAckHeader = 4;  // header for each item
        public const int MinimumReadAckItemSize = ReadAckHeader + ReadAckParameter + ReadItemAckHeader;

        public const int WriteHeader = 10;
        public const int WriteParameter = 2;
        public const int WriteParameterItem = 12; // each item
        public const int WriteDataItem = 4;       // each item + length
        public const int MinimumWriteItemSize = WriteHeader + WriteParameter + WriteParameterItem + WriteDataItem;



        public const int WriteItemHeader = 28; // 28 Header and some other data

        public ushort MaxAmQCalling { get; set; } = 5; // -> used for negotiation
        public ushort MaxAmQCalled { get; set; } = 5; // -> used for negotiation
        public ushort PduSize { get; set; } = 960;  // defautl pdu size -> used for negotiation
        public int Timeout { get; set; }


        public ushort ReadItemMaxLength => (ushort)(PduSize - MinimumReadAckItemSize);   //18 Header and some other data    // in the result message
        public ushort WriteItemMaxLength => (ushort)(PduSize - MinimumWriteItemSize);  //28 Header and some other data


        #region datagram detection

        public bool TryDetectDatagramType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length >= _minimumDataSize &&
               memory.Span[0] == _prefix)
            {

                switch ((PduType)memory.Span[_pduTypeOffset])  // PDU Type
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

        private static bool TryDetectJobType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > _minimumDataSize)
            {
                switch ((FunctionCode)memory.Span[_jobFunctionCodeOffset])  // Function Type
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

        private static bool TryDetectAckDataType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > _minimumAckDetectionSize)
            {
                switch ((FunctionCode)memory.Span[_ackDataFunctionCodeOffset])  // Function Type
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

        private static bool TryDetectUserDataDataType(Memory<byte> memory, out Type datagramType)
        {
            if (memory.Length > 22)
            {
                switch ((UserDataFunctionGroup)(memory.Span[15] & 0x0F))
                {
                    case UserDataFunctionGroup.Block:
                        {
                            // currently we do not support other types
                            switch ((UserDataSubFunctionBlock)memory.Span[16])  // Function Type
                            {
                                case UserDataSubFunctionBlock.BlockInfo:  // Write Var
                                    datagramType = typeof(S7PlcBlockInfoAckDatagram);
                                    return true;
                                case UserDataSubFunctionBlock.List:  // Write Var
                                    datagramType = typeof(S7PlcBlocksCountAckDatagram);
                                    return true;
                                case UserDataSubFunctionBlock.ListType:  // Write Var
                                    datagramType = typeof(S7PlcBlocksOfTypeAckDatagram);
                                    return true;
                            }
                        }
                        break;
                    case UserDataFunctionGroup.Cpu:
                        {
                            // currently we do not support other types
                            switch ((UserDataSubFunctionCpu)memory.Span[16])  // Function Type
                            {
                                case UserDataSubFunctionCpu.AlarmInit:  // Pending Alarms
                                    datagramType = typeof(S7PendingAlarmAckDatagram);
                                    return true;
                                case UserDataSubFunctionCpu.Msgs:  // Registration Ok
                                    datagramType = typeof(S7AlarmUpdateAckDatagram);
                                    return true;
                                case UserDataSubFunctionCpu.AlarmInd:  // Alarm Received
                                    datagramType = typeof(S7AlarmIndicationDatagram);
                                    return true;
                                case UserDataSubFunctionCpu.AlarmAck2:  // Alarm Received
                                    datagramType = typeof(S7AlarmIndicationDatagram);
                                    return true;
                                    
                            }
                        }
                        break;
                }

            }
            datagramType = null;
            return false;
        }

        #endregion
    }
}
