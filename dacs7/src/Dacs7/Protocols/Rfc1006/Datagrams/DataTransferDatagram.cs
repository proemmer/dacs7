// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7.Protocols.Rfc1006
{

    internal sealed class DataTransferDatagram : IDisposable
    {
        private IMemoryOwner<byte> _payload = null;
        public static byte EndOfTransmition = 0x80;
        internal static DataTransferDatagram _default = new();

        public TpktDatagram Tkpt { get; set; } = new TpktDatagram
        {
            Sync1 = 0x03,
            Sync2 = 0x00
        };

        public byte Li { get; set; } = 0x02; // Header Length -> sizeof DT -1

        public byte PduType { get; set; } = 0xf0;

        public byte TpduNr { get; set; } = EndOfTransmition;  //EOT

        public Memory<byte> Payload { get; set; }



        public void Dispose()
        {
            _payload?.Dispose();
            _payload = null;
        }


        public static IEnumerable<DataTransferDatagram> Build(Rfc1006ProtocolContext context, Memory<byte> rawPayload)
        {
            List<DataTransferDatagram> result = new();
            Memory<byte> payload = rawPayload;
            do
            {
                Memory<byte> frame = payload.Slice(0, Math.Min(payload.Length, context.FrameSizeSending));
                payload = payload.Slice(frame.Length);

                DataTransferDatagram current = new()
                {
                    _payload = MemoryPool<byte>.Shared.Rent(frame.Length)
                };
                current.Payload = current._payload.Memory.Slice(0, frame.Length);

                frame.CopyTo(current.Payload);
                if (payload.Length > 0)
                {
                    current.TpduNr = 0x00;
                }

                current.Tkpt.Length = Convert.ToUInt16(frame.Length + Rfc1006ProtocolContext.DataHeaderSize);
                result.Add(current);
            } while (payload.Length > 0);
            return result;
        }

        public static ushort GetRawDataLength(DataTransferDatagram datagram)
        {
            return datagram.Tkpt.Length;
        }

        public static Memory<byte> TranslateToMemory(DataTransferDatagram datagram, Memory<byte> buffer)
        {
            Span<byte> span = buffer.Span;
            span[0] = datagram.Tkpt.Sync1;
            span[1] = datagram.Tkpt.Sync2;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.Tkpt.Length);
            span[4] = datagram.Li;
            span[5] = datagram.PduType;
            span[6] = datagram.TpduNr;
            datagram.Payload.CopyTo(buffer.Slice(7));

            return buffer;
        }

        public static DataTransferDatagram TranslateFromMemory(Memory<byte> data, out int processed)
        {
            Span<byte> span = data.Span;
            ushort tkptLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2));
            if (data.Length < tkptLength)
            {
                processed = 0;
                return null;
            }

            DataTransferDatagram result = new()
            {
                Tkpt = new TpktDatagram
                {
                    Sync1 = span[0],
                    Sync2 = span[1],
                    Length = tkptLength
                },
                Li = span[4],
                PduType = span[5],
                TpduNr = span[6]
            };


            int length = result.Tkpt.Length - Rfc1006ProtocolContext.DataHeaderSize;
            result.Payload = data.Slice(Rfc1006ProtocolContext.DataHeaderSize, length);
            processed = result.Tkpt.Length;
            return result;
        }

        public static DataTransferDatagram TranslateFromMemory(Memory<byte> buffer,
                                                                Rfc1006ProtocolContext context,
                                                                out bool needMoteData,
                                                                out int processed)
        {
            DataTransferDatagram datagram = TranslateFromMemory(buffer, out processed);
            if (datagram != null)
            {
                if (datagram.TpduNr == EndOfTransmition)
                {
                    if (context.FrameBuffer.Any())
                    {
                        ApplyPayloadFromFrameBuffer(context.FrameBuffer, datagram);
                    }

                    needMoteData = false;
                    return datagram;
                }
                else if (!datagram.Payload.IsEmpty)
                {
                    AddPayloadToFrameBuffer(context.FrameBuffer, datagram);
                }
            }
            needMoteData = true;
            return datagram;
        }

        private static void AddPayloadToFrameBuffer(IList<(IMemoryOwner<byte> MemoryOwner, int Length)> framebuffer, DataTransferDatagram datagram)
        {
            IMemoryOwner<byte> copy = MemoryPool<byte>.Shared.Rent(datagram.Payload.Length);
            datagram.Payload.CopyTo(copy.Memory);
            framebuffer.Add(new ValueTuple<IMemoryOwner<byte>, int>(copy, datagram.Payload.Length));
        }

        private static void ApplyPayloadFromFrameBuffer(IList<(IMemoryOwner<byte> MemoryOwner, int Length)> framebuffer, DataTransferDatagram datagram)
        {
            // the payload of the last fragment is part of the receive buffer, so it is copied after the buffered fragments
            int length = framebuffer.Sum(x => x.Length) + datagram.Payload.Length;
            IMemoryOwner<byte> payload = MemoryPool<byte>.Shared.Rent(length);
            int index = 0;
            foreach ((IMemoryOwner<byte> MemoryOwner, int Length) in framebuffer)
            {
                MemoryOwner.Memory.Slice(0, Length).CopyTo(payload.Memory.Slice(index));
                index += Length;
            }
            datagram.Payload.CopyTo(payload.Memory.Slice(index));
            ClearFrameBuffer(framebuffer);

            datagram._payload?.Dispose();
            datagram._payload = payload;
            datagram.Payload = payload.Memory.Slice(0, length);
        }

        /// <summary>
        /// Removes all buffered fragments, e.g. if the connection was closed while a fragmented datagram was received.
        /// </summary>
        public static void ClearFrameBuffer(IList<(IMemoryOwner<byte> MemoryOwner, int Length)> framebuffer)
        {
            foreach ((IMemoryOwner<byte> MemoryOwner, int _) in framebuffer)
            {
                MemoryOwner.Dispose();
            }
            framebuffer.Clear();
        }

    }
}
