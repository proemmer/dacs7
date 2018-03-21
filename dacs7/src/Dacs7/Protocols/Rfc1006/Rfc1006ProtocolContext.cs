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
    public class Rfc1006ProtocolContext
    {
        private int _frameSize;
        private const byte TpduSizeMin = 0x04;
        private const byte TpduSizeMax = 0x0e;
        private const int defaultFrameSize = 1024;    // THis is for testing the slip and merge function!!
        public const int TpktHeaderSize = 4;
        public const int DataHeaderSize = TpktHeaderSize + 3;
        public bool DoNotBuildConnectionConfirm { get; set; }

        public string Name { get; set; }

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

        public IList<DataTransferDatagram> FrameBuffer { get; set; } = new List<DataTransferDatagram>();


        public Rfc1006ProtocolContext()
        {
            FrameSize = defaultFrameSize;
        }

        public void CalculateTpduSize(int frameSize = defaultFrameSize)
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

        internal void DetectDatagramType()
        {

        }


    }
}
