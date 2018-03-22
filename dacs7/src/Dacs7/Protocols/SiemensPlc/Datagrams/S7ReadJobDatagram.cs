// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7ReadJobDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public byte Function { get; set; } = 0x04; //Read Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();





        public static S7ReadJobDatagram BuildRead(SiemensPlcProtocolContext context, int id, IEnumerable<ReadItemSpecification> vars)
        {
            var result = new S7ReadJobDatagram();
            result.Header.ProtocolDataUnitReference = (ushort)id;
            if (vars != null)
            {
                foreach (var item in vars)
                {
                    result.Items.Add(new S7AddressItemSpecificationDatagram
                    {
                        TransportSize = S7AddressItemSpecificationDatagram.GetTransportSize(item.Area, item.VarType),
                        ItemSpecLength = item.Length,
                        DbNumber = item.DbNumber,
                        Area = (byte)item.Area,
                        Address = S7AddressItemSpecificationDatagram.GetAddress(item.Offset, item.VarType)
                    });
                }
            }
            result.Header.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }





        public static Memory<byte> TranslateToMemory(S7ReadJobDatagram datagram)
        {
            var result = S7HeaderDatagram.TranslateToMemory(datagram.Header);
            var span = result.Span;
            var offset = datagram.Header.GetHeaderSize();
            span[offset++] = datagram.Function;
            span[offset++] = datagram.ItemCount;


            foreach (var item in datagram.Items)
            {
                S7AddressItemSpecificationDatagram.TranslateToMemory(item, result.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            return result;
        }

        public static S7ReadJobDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7ReadJobDatagram
            {
                Header = S7HeaderDatagram.TranslateFromMemory(data),
            };
            var offset = result.Header.GetHeaderSize();
            result.Function = span[offset++];
            result.ItemCount = span[offset++];

            for (int i = 0; i < result.ItemCount; i++)
            {
                var res = S7AddressItemSpecificationDatagram.TranslateFromMemory(data.Slice(offset));
                result.Items.Add(res);
                offset += res.GetSpecificationLength();
            }

            return result;
        }

    }
}
