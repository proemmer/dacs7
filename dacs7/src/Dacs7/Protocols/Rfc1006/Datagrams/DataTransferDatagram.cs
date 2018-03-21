// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace Dacs7.Protocols.Rfc1006
{

    public class DataTransferDatagram
    {
        internal static DataTransferDatagram Default = new DataTransferDatagram();

        public TpktDatagram Tkpt { get; set; } = new TpktDatagram
        {
            Sync1 = 0x03,
            Sync2 = 0x00
        };


        public byte Li { get; set; } = 0x02; // Header Length -> sizeof DT -1

        public byte PduType { get; set; } = 0xf0;

        public byte TpduNr { get; set; } = 0x80;  //EOT

        public Memory<byte> Payload { get; set; }


        public static IEnumerable<DataTransferDatagram> Build(Rfc1006ProtocolContext context, Memory<byte> rawPayload)
        {
            var result = new List<DataTransferDatagram>();
            var payload = rawPayload;
            do
            {
                var frame = payload.Slice(0, Math.Min(payload.Length, context.FrameSize));
                payload = payload.Slice(frame.Length);
                var current = new DataTransferDatagram()
                {
                    Payload = new byte[frame.Length]
                };
                frame.CopyTo(current.Payload);
                if (payload.Length > 0)
                    current.TpduNr = 0x00;

                current.Tkpt.Length = Convert.ToUInt16(frame.Length + Rfc1006ProtocolContext.DataHeaderSize);
                result.Add(current);
            } while (payload.Length > 0);
            return result;
        }

        //public DataTransferDatagram Decode(Rfc1006ProtocolContext context, DataTransferDatagram incoming)
        //{
        //    if (incoming.TpduNr == 0x80 && !context.FrameBuffer.Any())
        //        return incoming;

        //    context.FrameBuffer.Add(incoming);
        //    if (incoming.TpduNr == 0x80) //EOT
        //    {
        //        var length = context.FrameBuffer.Sum(x => x.Payload.Length);
        //        var res = new byte[length];
        //        var index = 0;
        //        foreach (var item in context.FrameBuffer)
        //        {
        //            item.Payload.CopyTo(res.Sl);
        //            index += item.Payload.Length;
        //        }
        //        incoming.Payload = res;
        //        context.FrameBuffer.Clear();
        //        return incoming;
        //    }
        //    return null;
        //}


        public static Memory<byte> TranslateToMemory(DataTransferDatagram datagram)
        {
            var length = datagram.Tkpt.Length;
            var result = new Memory<byte>(new byte[length]);  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.Tkpt.Sync1;
            span[1] = datagram.Tkpt.Sync2;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.Tkpt.Length);
            span[4] = datagram.Li;
            span[5] = datagram.PduType;
            span[6] = datagram.TpduNr;

            datagram.Payload.CopyTo(result.Slice(7));

            return result;
        }

        public static DataTransferDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new DataTransferDatagram
            {
                Tkpt = new TpktDatagram
                {
                    Sync1 = span[0],
                    Sync2 = span[1],
                    Length = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2))
                },
                Li = span[4],
                PduType = span[5],
                TpduNr = span[6]
            };

            result.Payload = data.Slice(7);

            return result;
        }
    }
}
