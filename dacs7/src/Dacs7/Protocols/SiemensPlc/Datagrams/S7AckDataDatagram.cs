// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7AckDataDatagram
    {
        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x03, //Ack_Data - > Should be a marker
        };

        public S7HeaderErrorCodesDatagram Error { get; set; } = new S7HeaderErrorCodesDatagram();





        public int GetParameterOffset()
        {
            return Header.GetHeaderSize() + Error.GetSize();
        }


        public static Memory<byte> TranslateToMemory(S7AckDataDatagram datagram)
        {
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header);
            S7HeaderErrorCodesDatagram.TranslateToMemory(datagram.Error, result.Slice(datagram.Header.GetHeaderSize()));
            return result;
        }

        public static S7AckDataDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7AckDataDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data)
            };
            result.Error = S7HeaderErrorCodesDatagram.TranslateFromMemory(data.Slice(result.Header.GetHeaderSize()));

            return result;
        }
    }
}
