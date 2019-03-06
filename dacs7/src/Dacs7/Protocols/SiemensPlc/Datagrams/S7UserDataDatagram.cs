using Dacs7.Domain;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.SiemensPlc
{
    internal class S7UserDataDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x07, //UserData - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public S7UserDataParameter Parameter { get; set; }

        public S7UserData Data { get; set; }






        public static S7UserDataDatagram BuildBlockInfoRequest(SiemensPlcProtocolContext context, int id, PlcBlockType blockType, int blockNumber)
        {
            var result = new S7UserDataDatagram
            {
                Parameter = new S7UserDataParameter
                {
                    ParamDataLength = 4,
                    TypeAndGroup = ((byte)UserDataFunctionType.Request << 4) | (byte)UserDataFunctionGroup.Block,
                    SubFunction = (byte)UserDataSubFunctionBlock.BlockInfo,
                    SequenceNumber = 0,
                    ParameterType = (byte)UserDataParamTypeType.Request 
                },
                Data = new S7UserData
                {
                    ReturnCode = (byte)ItemResponseRetValue.Success,
                    TransportSize = (byte)DataTransportSize.OctetString,
                }
            };

            result.Header.ProtocolDataUnitReference = (ushort)id;
            result.Header.DataLength = 12;
            result.Header.ParamLength = 8;

            result.Data.Data = new byte[] { 0x30, (byte)blockType, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41 };
            Encoding.Default.GetBytes(string.Format("{0:00000}", blockNumber)).AsSpan().CopyTo(result.Data.Data.Span.Slice(2, 5));
            result.Data.UserDataLength = (ushort)result.Data.Data.Length;

            return result;
        }

        public static S7UserDataDatagram BuildPendingAlarmRequest(SiemensPlcProtocolContext context, ushort id, byte sequenceNumber)
        {
            var data = sequenceNumber == 0 ? new byte[] { 0x00, 0x01, 0x12, 0x08, 0x1a, 0x00, 0x01, 0x34, 0x00, 0x00, 0x00, 0x04 } : new byte[0]; ;
            var result = new S7UserDataDatagram
            {
                Parameter = new S7UserDataParameter
                {
                    ParamDataLength = sequenceNumber == 0 ? (byte)4 : (byte)8,
                    TypeAndGroup = ((byte)UserDataFunctionType.Request << 4) | (byte)UserDataFunctionGroup.Cpu,
                    SubFunction = (byte)UserDataSubFunctionCpu.AlarmInit,
                    SequenceNumber = sequenceNumber,
                    ParameterType = sequenceNumber == 0 ? (byte)UserDataParamTypeType.Request : (byte)UserDataParamTypeType.Response
                },
                Data = new S7UserData
                {
                    Data = data,
                    UserDataLength = (ushort)data.Length,
                    ReturnCode = data.Length > 0 ? (byte)ItemResponseRetValue.Success : (byte)ItemResponseRetValue.DataError,
                    TransportSize = data.Length > 0 ? (byte)DataTransportSize.OctetString : (byte)DataTransportSize.Null
                }
            };

            result.Header.ProtocolDataUnitReference = id;
            result.Header.DataLength = sequenceNumber == 0 ? (ushort)16 : (ushort)4;
            result.Header.ParamLength = sequenceNumber == 0 ? (ushort)8 : (ushort)12;

            return result;
        }


        public static S7UserDataDatagram BuildAlarmUpdateRequest(SiemensPlcProtocolContext s7Context, ushort id, bool activate = true)
        {
            var data = new byte[] { 0x86, 0x00, 0x61, 0x73, 0x6d, 0x65, 0x73, 0x73, 0x00, 0x00, activate ? (byte)0x09 : (byte)0x08, 0x00 };
            var result = new S7UserDataDatagram
            {
                Parameter = new S7UserDataParameter
                {
                    ParamDataLength = 4,
                    TypeAndGroup = ((byte)UserDataFunctionType.Request << 4) | (byte)UserDataFunctionGroup.Cpu,
                    SubFunction = (byte)UserDataSubFunctionCpu.Msgs,
                    SequenceNumber = 0,
                    ParameterType = (byte)UserDataParamTypeType.Request
                },
                Data = new S7UserData
                {
                    Data = data,
                    UserDataLength = (ushort)data.Length,
                    ReturnCode = data.Length > 0 ? (byte)ItemResponseRetValue.Success : (byte)ItemResponseRetValue.DataError,
                    TransportSize = data.Length > 0 ? (byte)DataTransportSize.OctetString : (byte)DataTransportSize.Null
                }
            };

            result.Header.ProtocolDataUnitReference = id;
            result.Header.DataLength = 16;
            result.Header.ParamLength = 8;

            return result;
        }



        public static IMemoryOwner<byte> TranslateToMemory(S7UserDataDatagram datagram, out int memoryLength)
        {
            var offset = datagram.Header.GetHeaderSize() + datagram.Parameter.GetParamSize();
            var dataSize = offset + datagram.Data.GetUserDataLength();
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header, dataSize, out memoryLength);
            var mem = result.Memory.Slice(0, memoryLength);
            S7UserDataParameter.TranslateToMemory(datagram.Parameter, mem.Slice(datagram.Header.GetHeaderSize()));
            mem.Span[offset++] = datagram.Data.ReturnCode;
            mem.Span[offset++] = datagram.Data.TransportSize;
            BinaryPrimitives.WriteUInt16BigEndian(mem.Slice(offset, 2).Span, datagram.Data.UserDataLength);
            datagram.Data.Data.CopyTo(mem.Slice(offset+2, datagram.Data.UserDataLength));
            return result;
        }

        public static S7UserDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7UserDataDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data),
                Data = new S7UserData()
            };

            result.Parameter = S7UserDataParameter.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));
            var offset = result.Header.GetHeaderSize() + result.Parameter.GetParamSize();
            result.Data.ReturnCode = span[offset++];
            result.Data.TransportSize = span[offset++];
            result.Data.UserDataLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
            offset += 2;
            result.Data.Data = new byte[result.Data.UserDataLength];
            data.Slice(offset, result.Data.UserDataLength).CopyTo(result.Data.Data);
            return result;
        }


    }
}
