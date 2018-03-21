// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Linq;

namespace Dacs7.Protocols.Rfc1006
{
    public class ConnectionRequestDatagram 
    {
        internal static ConnectionRequestDatagram Default = new ConnectionRequestDatagram();


        public TpktDatagram Tkpt { get; set; } = new TpktDatagram
        {
            Sync1 = 0x03,
            Sync2 = 0x00
        };

        public byte Li { get; set; }                                    //Calculate length = 6 + ParamLength(11)

        public byte PduType { get; set; } = 0xe0;

        public Int16 DstRef { get; set; } = 0x0000;                     // TPDU Destination Reference

        public Int16 SrcRef { get; set; } = 0x0001;                     // TPDU Source-Reference (my own reference, should not be zero)


        public byte ClassOption { get; set; } = 0x00;                   // PDU Class 0 and no Option


        public byte ParmCodeTpduSize { get; set; } = 0xc0;              // code that identifies TPDU size


        public byte Unknown { get; set; } = 0x01;

        public byte SizeTpduReceiving { get; set; }

        public byte ParmCodeSrcTsap { get; set; } = 0xc1;

        public byte SourceTsapLength { get; set; }


        public Memory<byte> SourceTsap { get; set; }


        public byte ParmCodeDestTsap { get; set; } = 0xc2;


        public byte DestTsapLength { get; set; }


        public Memory<byte> DestTsap { get; set; }



        public static ConnectionRequestDatagram BuildCr(Rfc1006ProtocolContext context)
        {
            context.CalcLength(context, out byte li, out ushort length);
            var result =  new ConnectionRequestDatagram
            {
                Li = li,
                SizeTpduReceiving = context.SizeTpduReceiving,
                SourceTsapLength = Convert.ToByte(context.SourceTsap.Length),
                SourceTsap = context.SourceTsap,
                DestTsapLength = Convert.ToByte(context.DestTsap.Length),
                DestTsap = context.DestTsap
            };
            result.Tkpt.Length = length;
            return result;
        }

        public bool Correlate(ConnectionRequestDatagram o1, ConnectionConfirmedDatagram o2)
        {
            //Test ack
            if (o1.SourceTsap.Span.SequenceEqual(o2.SourceTsap.Span) && o1.DestTsap.Span.SequenceEqual(o2.DestTsap.Span))
                return true;

            return false;
        }

        public static Memory<byte> TranslateToMemory(ConnectionRequestDatagram datagram)
        {
            var length = datagram.Tkpt.Length;
            var result = new Memory<byte>(new byte[length]);  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.Tkpt.Sync1;
            span[1] = datagram.Tkpt.Sync2;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.Tkpt.Length);
            span[4] = datagram.Li;
            span[5] = datagram.PduType;
            BinaryPrimitives.WriteInt16BigEndian(span.Slice(6, 2), datagram.DstRef);
            BinaryPrimitives.WriteInt16BigEndian(span.Slice(8, 2), datagram.DstRef);
            span[10] = datagram.ClassOption;
            span[11] = datagram.ParmCodeTpduSize;
            span[12] = datagram.Unknown;
            span[13] = datagram.SizeTpduReceiving;


            var offset = 14;
            span[offset++] = datagram.ParmCodeSrcTsap;
            span[offset++] = datagram.SourceTsapLength;
            datagram.SourceTsap.CopyTo(result.Slice(offset));
            offset += (int)datagram.SourceTsapLength;

            span[offset++] = datagram.ParmCodeDestTsap;
            span[offset++] = datagram.DestTsapLength;
            datagram.DestTsap.CopyTo(result.Slice(offset));
            offset += (int)datagram.DestTsapLength;

            return result;
        }

        public static ConnectionRequestDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new ConnectionRequestDatagram
            {
                Tkpt = new TpktDatagram
                {
                    Sync1 = span[0],
                    Sync2 = span[1],
                    Length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2))
                },
                Li = span[4],
                PduType = span[5],
                DstRef = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6, 2)),
                SrcRef = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8, 2)),
                ClassOption = span[10],
                ParmCodeTpduSize = span[11],
                Unknown = span[12],
                SizeTpduReceiving = span[13],
            };

            var offset = 14;
            result.ParmCodeSrcTsap = span[offset++];
            result.SourceTsapLength = span[offset++];
            result.SourceTsap = data.Slice(offset, (int)result.SourceTsapLength);
            offset += (int)result.SourceTsapLength;

            result.ParmCodeDestTsap = span[offset++];
            result.DestTsapLength = span[offset++];
            result.DestTsap = data.Slice(offset, result.DestTsapLength);
            offset += (int)result.DestTsapLength;

            return result;
        }
    }
}
