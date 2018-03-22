// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7ReadJobAckDatagram
    {
        public S7AckDataDatagram Header { get; set; } = new S7AckDataDatagram();

        public byte Function { get; set; } = 0x04; //Read Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();


        public static Memory<byte> TranslateToMemory(S7ReadJobAckDatagram datagram)
        {
            var result = S7AckDataDatagram.TranslateToMemory(datagram.Header);
            var span = result.Span;
            var offset = datagram.Header.Header.GetHeaderSize();
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;


            foreach (var item in datagram.Data)
            {
                S7DataItemSpecification.TranslateToMemory(item, result.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            return result;
        }

        public static S7ReadJobAckDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7ReadJobAckDatagram
            {
                Header = S7AckDataDatagram.TranslateFromMemory(data),
            };
            var offset = result.Header.GetParameterOffset();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (int i = 0; i < result.ItemCount; i++)
            {
                var res = S7DataItemSpecification.TranslateFromMemory(data.Slice(offset));
                result.Data.Add(res);
                offset += res.GetSpecificationLength();
            }

            return result;
        }
    }
}
