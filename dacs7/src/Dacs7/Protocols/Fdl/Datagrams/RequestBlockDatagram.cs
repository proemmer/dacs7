using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Dacs7.Protocols.Fdl
{
    internal class RequestBlockDatagram
    {
        public RequestBlockHeader Header { get; set; }

        public ApplicationBlock ApplicationBlock { get; set; }

        public byte[] Reserved { get; set; } = new byte[12];
        public byte[] Reverence { get; set; } = new byte[2];
        public Memory<byte> UserData1 { get; set; } = new byte[260];
        public Memory<byte> UserData2 { get; set; } = new byte[260];





        public static RequestBlockDatagram BuildCr(FdlProtocolContext context)
        {
            var result = new RequestBlockDatagram
            {
                Header = new RequestBlockHeader
                {
                    Length = 80,
                    User = context.User,
                    RbType = 2,
                    Priority = 1,
                    Subsystem = 0x7a,
                    OpCode = 128,
                    Response = 0xff,
                    FillLength1 = 12,
                    SegLength1 = 0,
                    Offset1 = 80,
                    FillLength2 = 0,
                    SegLength2 = 0,
                    Offset2 = 0,
                },
                ApplicationBlock = new ApplicationBlock
                {
                    Opcode = 0,
                    Subsystem = 0,
                    Id = 0,
                    Service = ServiceCode.FdlReadValue,
                    LocalAddress = new RemoteAddress
                    {

                    },

                    RemoteAddress = new RemoteAddress
                    {

                    },
                    ServiceClass = ServiceClass.Low,
                    Receive1Sdu = new LinkServiceDataUnit
                    {

                    },

                    Send1Sdu = new LinkServiceDataUnit
                    {

                    },

                    Reserved2 = new ushort[2]
                }
            };


            //  66 + 12 bytes reserved!!
            result.Reverence = new byte[2];
            result.UserData1 = new byte[260];
            result.UserData2 = new byte[260];
            return result;
        }






        public static Memory<byte> TranslateToMemory(RequestBlockDatagram datagram)
        {
            var length = datagram.Header.Length + datagram.Header.FillLength1 + datagram.Header.FillLength2;
            var result = new Memory<byte>(new byte[length]);  // check if we could use ArrayBuffer
            var span = result.Span;

            //BinaryPrimitives.WriteUInt16BigEndian(span.Slice(0, 2), datagram.Header.Reserved[0]);
            //BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), datagram.Header.Reserved[1]);
            span[4] = datagram.Header.Length;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(5, 2), datagram.Header.User);
            span[7] = datagram.Header.RbType;
            span[8] = datagram.Header.Priority;
            //span[9] = datagram.Header.Reserved1;
            //BinaryPrimitives.WriteUInt16BigEndian(span.Slice(10, 2), datagram.Header.Reserved2);
            span[12] = datagram.Header.Subsystem;
            span[13] = datagram.Header.OpCode;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(14, 2), datagram.Header.Response);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(16, 2), datagram.Header.FillLength1);
            //span[18] = datagram.Header.Reserved3;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(19, 2), datagram.Header.SegLength1);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(21, 2), datagram.Header.Offset1);
            //BinaryPrimitives.WriteUInt16BigEndian(span.Slice(23, 2), datagram.Header.Reserved4);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(25, 2), datagram.Header.FillLength2);
            //span[27] = datagram.Header.Reserved5;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(28, 2), datagram.Header.SegLength2);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(30, 2), datagram.Header.Offset2);
            //BinaryPrimitives.WriteUInt16BigEndian(span.Slice(32, 2), datagram.Header.Reserved6);



            span[34] = datagram.ApplicationBlock.Opcode;
            span[35] = datagram.ApplicationBlock.Subsystem;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(36, 2), datagram.ApplicationBlock.Id);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(38, 2), (ushort)datagram.ApplicationBlock.Service);
            span[40] = datagram.ApplicationBlock.LocalAddress.Station;
            span[41] = datagram.ApplicationBlock.LocalAddress.Segment;
            span[42] = datagram.ApplicationBlock.Ssap;
            span[43] = datagram.ApplicationBlock.Dsap;
            span[44] = datagram.ApplicationBlock.RemoteAddress.Station;
            span[45] = datagram.ApplicationBlock.RemoteAddress.Segment;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(46, 2), (ushort)datagram.ApplicationBlock.ServiceClass);

            BinaryPrimitives.WriteInt32BigEndian(span.Slice(48, 4), datagram.ApplicationBlock.Receive1Sdu.BufferPtr);
            span[52] = datagram.ApplicationBlock.Receive1Sdu.Length;

            span[53] = datagram.ApplicationBlock.Reserved1;
            span[54] = datagram.ApplicationBlock.Reserved;


            BinaryPrimitives.WriteInt32BigEndian(span.Slice(55, 4), datagram.ApplicationBlock.Send1Sdu.BufferPtr);
            span[59] = datagram.ApplicationBlock.Send1Sdu.Length;


            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(60, 2), datagram.ApplicationBlock.LinkSatus);

            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(62, 2), datagram.ApplicationBlock.Reserved2[0]);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(64, 2), datagram.ApplicationBlock.Reserved2[1]);



            //  66 + 12 bytes reserved!!
            span[78] = datagram.Reverence[0];
            span[79] = datagram.Reverence[1];

            if (datagram.Header.SegLength1 > 0)
            {
                datagram.UserData1.CopyTo(result.Slice(datagram.Header.Offset1, datagram.Header.SegLength1));
            }
            if (datagram.Header.SegLength2 > 0)
            {
                datagram.UserData2.CopyTo(result.Slice(340, 260));
            }

            return result;
        }

        public static RequestBlockDatagram TranslateFromMemory(Memory<byte> data, out int processed)
        {
            var span = data.Span;
            var result = new RequestBlockDatagram
            {
                Header = new RequestBlockHeader
                {
                    Length = span[4],
                    User = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(5, 2)),
                    RbType = span[7],
                    Priority = span[8],
                    //Reserved1 = span[9],
                    //Reserved2 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(10, 2)),
                    Subsystem = span[12],
                    OpCode = span[13],
                    Response = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14, 2)),
                    FillLength1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(16, 2)),
                    //Reserved3 = span[18],
                    SegLength1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(19, 2)),
                    Offset1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(21, 2)),
                    //Reserved4 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(23, 2)),
                    FillLength2 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(25, 2)),
                    //Reserved5 = span[27],
                    SegLength2 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(28, 2)),
                    Offset2 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(30, 2)),
                    //Reserved6 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(32, 2)),
                },
                ApplicationBlock = new ApplicationBlock
                {
                    Opcode = span[34],
                    Subsystem = span[35],
                    Id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(36, 2)),
                    Service = (ServiceCode)BinaryPrimitives.ReadUInt16BigEndian(span.Slice(38, 2)),
                    LocalAddress = new RemoteAddress
                    {
                        Station = span[40],
                        Segment = span[41],
                    },
                    Ssap = span[42],
                    Dsap = span[43],
                    RemoteAddress = new RemoteAddress
                    {
                        Station = span[44],
                        Segment = span[45],
                    },
                    ServiceClass = (ServiceClass)BinaryPrimitives.ReadUInt16BigEndian(span.Slice(46, 2)),
                    Receive1Sdu = new LinkServiceDataUnit
                    {
                        BufferPtr = BinaryPrimitives.ReadInt32BigEndian(span.Slice(48, 4)),
                        Length = span[52]
                    },
                    Reserved1 = span[53],
                    Reserved = span[54],
                    Send1Sdu = new LinkServiceDataUnit
                    {
                        BufferPtr = BinaryPrimitives.ReadInt32BigEndian(span.Slice(55, 4)),
                        Length = span[59]
                    },
                    LinkSatus = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(60, 2)),
                    Reserved2 = new ushort[] { BinaryPrimitives.ReadUInt16BigEndian(span.Slice(62, 2)), BinaryPrimitives.ReadUInt16BigEndian(span.Slice(64, 2)) }
                }
            };


            //  66 + 12 bytes reserved!!


            result.Reverence = new byte[] { span[78], span[79] };

            result.UserData1 = new byte[result.Header.FillLength1];
            if (result.Header.FillLength1 > 0)
            {
                span.Slice(result.Header.Offset1, result.Header.FillLength1).CopyTo(result.UserData1.Span);
            }
            result.UserData2 = new byte[result.Header.FillLength1];
            if (result.Header.FillLength2 > 0)
            {
                span.Slice(result.Header.Offset2, result.Header.FillLength2).CopyTo(result.UserData2.Span);
            }

            processed = 600;
            return result;
        }

        public static RequestBlockDatagram TranslateFromMemory(Memory<byte> buffer,
                                                                FdlProtocolContext context,
                                                                out bool needMoteData,
                                                                out int processed)
        {
            var datagram = TranslateFromMemory(buffer, out processed);
            //if (datagram.TpduNr == EndOfTransmition)
            //{
            //    Memory<byte> payload = Memory<byte>.Empty;
            //    if (context.FrameBuffer.Any())
            //    {
            //        context.FrameBuffer.Add(new Tuple<Memory<byte>, int>(datagram.Payload, datagram.Payload.Length));
            //        var length = context.FrameBuffer.Sum(x => x.Item1.Length);
            //        payload = new byte[length];
            //        var index = 0;
            //        foreach (var item in context.FrameBuffer)
            //        {
            //            item.Item1.Slice(0, item.Item2).CopyTo(payload.Slice(index));
            //            if (!ReferenceEquals(datagram.Payload, item.Item1))
            //            {
            //                ArrayPool<byte>.Shared.Return(item.Item1.ToArray());
            //            }
            //            index += item.Item2;
            //        }
            //        datagram.Payload = payload;
            //        context.FrameBuffer.Clear();
            //    }

            //    needMoteData = false;
            //    return datagram;
            //}
            //else if (!datagram.Payload.IsEmpty)
            //{
            //    Memory<byte> copy = ArrayPool<byte>.Shared.Rent(datagram.Payload.Length);
            //    datagram.Payload.CopyTo(copy);
            //    context.FrameBuffer.Add(new Tuple<Memory<byte>, int>(copy, datagram.Payload.Length));
            //}
            needMoteData = true;
            return datagram;
        }
    }
}
