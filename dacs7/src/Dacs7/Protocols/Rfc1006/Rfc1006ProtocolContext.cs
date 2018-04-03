// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.Rfc1006
{
    /// <summary>
    /// Context class for the protocol instance
    /// Contains all properties for the protocol
    /// </summary>
    internal class Rfc1006ProtocolContext
    {
        private int _frameSize;

        private const byte Prefix0 = 0x03;
        private const byte Prefix1 = 0x00;
        private const byte TpduSizeMin = 0x04;
        private const byte TpduSizeMax = 0x0e;
        private const int DefaultFrameSize = 1024;
        private const int DatagramTypeOffset = 5;
        private const int TpktHeaderSize = 4;
        


        public bool DoNotBuildConnectionConfirm { get; set; }


        public int Port { get; set; }
        public bool IsClient { get; set; }
        public int FrameSize
        {
            get
            {
                return _frameSize;
            }
            set
            {
                if(_frameSize != value)
                    CalculateTpduSize(value);
            }
        }

        public Memory<byte> SourceTsap { get; set; } = new byte[] { 0x01, 0x00 };
        public Memory<byte> DestTsap { get; set; } 
        public byte SizeTpduReceiving { get; set; }
        public byte SizeTpduSending { get; set; }

        public IList<Tuple<Memory<byte>, int>> FrameBuffer { get; set; } = new List<Tuple<Memory<byte>, int>>();

        /// <summary>
        /// The minimum data size we need to detect the datagram type
        /// </summary>
        public static int MinimumBufferSize => TpktHeaderSize + 2;

        /// <summary>
        /// The size of the data header
        /// </summary>
        public static int DataHeaderSize => TpktHeaderSize + 3;

        public Rfc1006ProtocolContext()
        {
            FrameSize = DefaultFrameSize;
        }

        public void CalculateTpduSize(int frameSize = DefaultFrameSize)
        {
            var b = -1;
            for (var i = frameSize; i > 0; i = i >> 1, ++b) ;
            b = Math.Max(TpduSizeMin, Math.Min(TpduSizeMax, b));
            SizeTpduSending = SizeTpduReceiving = (byte)b;
            _frameSize = frameSize;
        }

        public void CalcLength(Rfc1006ProtocolContext context, out byte li, out ushort length)
        {
            const int optionsMinLength = 7;
            const int TpduCrWithoutProperties = 6;
            var tmp = Convert.ToUInt16(optionsMinLength + context.SourceTsap.Length + context.DestTsap.Length + TpduCrWithoutProperties);
            length = (ushort)(tmp + TpktHeaderSize + 1); // add 1 because li is without li
            li = Convert.ToByte(tmp);
        }

        public static Memory<byte> CalcRemoteTsap(ushort connectionType, int rack, int slot)
        {
            var mem = new Memory<byte>(new byte[2]);
            var value = (ushort)(((ushort)connectionType << 8) + (rack * 0x20) + slot);
            BinaryPrimitives.TryWriteUInt16BigEndian(mem.Span, value);
            return mem;
        }

        public bool TryDetectDatagramType(Memory<byte> memory, out Type datagramType)
        {

            var span = memory.Span;
            if (span[0] == Prefix0 && span[1] == Prefix1)
            {
                switch (span[DatagramTypeOffset])
                {
                    case 0xd0:
                        datagramType = typeof(ConnectionConfirmedDatagram);// CC
                        return true;
                    case 0xe0:
                        datagramType = typeof(ConnectionRequestDatagram);// CR
                        return true;
                    case 0xf0:
                        datagramType = typeof(DataTransferDatagram);// DT
                        return true;
                }
            }

            datagramType = null;
            return false;
        }


    }
}
