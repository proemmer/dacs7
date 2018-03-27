// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Buffers;

namespace Dacs7.Protocols.Rfc1006
{

    public class DataTransferDatagram
    {
        public static byte EndOfTransmition = 0x80;
        internal static DataTransferDatagram Default = new DataTransferDatagram();

        public TpktDatagram Tkpt { get; set; } = new TpktDatagram
        {
            Sync1 = 0x03,
            Sync2 = 0x00
        };


        public byte Li { get; set; } = 0x02; // Header Length -> sizeof DT -1

        public byte PduType { get; set; } = 0xf0;

        public byte TpduNr { get; set; } = EndOfTransmition;  //EOT

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

        public static DataTransferDatagram TranslateFromMemory(Memory<byte> data, out int processed)
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

            var length = result.Tkpt.Length - 7;
            result.Payload = data.Slice(7, length);
            processed = result.Tkpt.Length;
            return result;
        }

        public static DataTransferDatagram TranslateFromMemory(Memory<byte> buffer, 
                                                                Rfc1006ProtocolContext context, 
                                                                out bool needMoteData,
                                                                out int processed)
        {
            var datagram = TranslateFromMemory(buffer, out processed);
            if (datagram.TpduNr == EndOfTransmition)
            {
                Memory<byte> payload = Memory<byte>.Empty;
                if (context.FrameBuffer.Any())
                {
                    context.FrameBuffer.Add(new Tuple<Memory<byte>, int>(datagram.Payload, datagram.Payload.Length));
                    var length = context.FrameBuffer.Sum(x => x.Item1.Length);
                    payload = new byte[length];
                    var index = 0;
                    foreach (var item in context.FrameBuffer)
                    {
                        item.Item1.Slice(0, item.Item2).CopyTo(payload.Slice(index));
                        if (!ReferenceEquals(datagram.Payload, item.Item1))
                        {
                            ArrayPool<byte>.Shared.Return(item.Item1.ToArray());
                        }
                        index += item.Item2;
                    }
                    datagram.Payload = payload;
                    context.FrameBuffer.Clear();
                }

                needMoteData = false;
                return datagram;
            }
            else if(!datagram.Payload.IsEmpty)
            {
                Memory<byte> copy = ArrayPool<byte>.Shared.Rent(datagram.Payload.Length);
                datagram.Payload.CopyTo(copy);
                context.FrameBuffer.Add(new Tuple<Memory<byte>, int>(copy, datagram.Payload.Length));
            }
            needMoteData = true;
            return datagram;
        }

    }
}
