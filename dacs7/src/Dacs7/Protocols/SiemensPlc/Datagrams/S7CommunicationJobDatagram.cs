// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7CommunicationJobDatagram
    {
        public S7CommHeaderDatagram CommHeader { get; set; } = new S7CommHeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 8
        };



        public byte Function { get; set; } = 0xF0; //Setup communication


        public byte Reserved { get; set; } = 0x00;


        public UInt16 MaxAmQCalling { get; set; }


        public UInt16 MaxAmQCalled { get; set; }


        public UInt16 PduLength { get; set; }

        public Memory<byte> Payload { get; set; }



        public static S7CommunicationJobDatagram Build(SiemensPlcProtocolContext context)
        {
            //TODO we need a parameter for the UnitId
            var result = new S7CommunicationJobDatagram
            {
                MaxAmQCalled = context.MaxParallelJobs,
                MaxAmQCalling = context.MaxParallelJobs,
                PduLength = context.PduSize
            };

            return result;
        }

        public bool Correlate(S7CommunicationJobDatagram o1, S7CommAckDataDatagram o2)
        {
            //Test ack
            if (o1.CommHeader.RedundancyIdentification == o2.Header.CommHeader.RedundancyIdentification)
                return true;

            return false;
        }

        public static Memory<byte> TranslateToMemory(S7CommunicationJobDatagram datagram)
        {
            var length = 10 + datagram.CommHeader.ParamLength + datagram.CommHeader.DataLength;
            var result = new Memory<byte>(new byte[length]);  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.CommHeader.ProtocolId;
            span[1] = datagram.CommHeader.PduType;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.CommHeader.RedundancyIdentification);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), datagram.CommHeader.ProtocolDataUnitReference);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), datagram.CommHeader.ParamLength);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(8, 2), datagram.CommHeader.DataLength);
            span[10] = datagram.Function;
            span[11] = datagram.Reserved;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(12, 2), datagram.MaxAmQCalling);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(14, 2), datagram.MaxAmQCalled);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(16, 2), datagram.PduLength);

            datagram.Payload.CopyTo(result.Slice(18));

            return result;
        }

        public static S7CommunicationJobDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7CommunicationJobDatagram
            {
                CommHeader = new S7CommHeaderDatagram
                {
                    ProtocolId = span[0],
                    PduType = span[1],
                    RedundancyIdentification = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2)),
                    ProtocolDataUnitReference = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2)),
                    ParamLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2)),
                    DataLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8, 2)),
                },
                Function = span[10],
                Reserved = span[11],
                MaxAmQCalling = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(12, 2)),
                MaxAmQCalled = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14, 2)),
                PduLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(16, 2))
            };

            result.Payload = data.Slice(18);

            return result;
        }

    }
}
