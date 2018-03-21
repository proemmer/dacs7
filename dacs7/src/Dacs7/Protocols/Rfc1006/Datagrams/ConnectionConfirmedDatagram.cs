// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.Rfc1006
{
    public class ConnectionConfirmedDatagram 
    {
        internal static ConnectionConfirmedDatagram Default = new ConnectionConfirmedDatagram();


        public TpktDatagram Tkpt { get; set; } = new TpktDatagram
        {
            Sync1 = 0x03,
            Sync2 = 0x00
        };

        public byte Li { get; set; }

        public byte PduType { get; set; } // = 0xd0;

        public Int16 DstRef { get; set; } //  = 0x0001;                     // TPDU Destination Reference

        public Int16 SrcRef { get; set; } // = 0x0001;                     // TPDU Source-Reference (my own reference, should not be zero)

        public byte ClassOption { get; set; } // = 0x00;                 // PDU Class 0 and no Option

        public byte ParmCodeTpduSize { get; set; } // = 0xc0;

        public byte Unknown { get; set; } // = 0x01;

        public byte SizeTpduReceiving { get; set; }

        public byte ParmCodeSrcTsap { get; set; } = 0xc1;

        public byte SourceTsapLength { get; set; }

        public Memory<byte> SourceTsap { get; set; }

        public byte ParmCodeDestTsap { get; set; } = 0xc2;

        public byte DestTsapLength { get; set; }

        public Memory<byte> DestTsap { get; set; }



        public ConnectionConfirmedDatagram BuildCc(Rfc1006ProtocolContext context, ConnectionRequestDatagram req)
        {
            context.CalcLength(context, out byte li, out ushort length);
            context.SizeTpduSending = req.SizeTpduReceiving;
            var result = new ConnectionConfirmedDatagram
            {
                Li = li,
                SizeTpduReceiving = context.SizeTpduReceiving,
                SourceTsapLength = req.DestTsapLength,
                SourceTsap = req.SourceTsap,
                DestTsapLength = req.DestTsapLength,
                DestTsap = req.DestTsap
            };

            result.Tkpt.Length = length;
            return result;
        }


        public static Memory<byte> TranslateToMemory(ConnectionConfirmedDatagram datagram)
        {
            return new Memory<byte>();
        }

        public static ConnectionConfirmedDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new ConnectionConfirmedDatagram
            {
                Tkpt = new TpktDatagram
                {
                    Sync1 = span[0],
                    Sync2 = span[1],
                    Length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2,2))
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
            result.SourceTsap = data.Slice(offset, result.DestTsapLength);
            offset += (int)result.DestTsapLength;

            return result;
        }
    }
}
