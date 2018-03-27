// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7WriteJobDatagram
    {

        public S7HeaderDatagram Header { get; set; } = new S7HeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public byte Function { get; set; } = 0x05; //Write Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();

        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();

        public static S7WriteJobDatagram BuildWrite(SiemensPlcProtocolContext context, int id, IEnumerable<WriteItemSpecification> vars)
        {
            var numberOfItems = 0;
            var result = new S7WriteJobDatagram();
            result.Header.ProtocolDataUnitReference = (ushort)id;
            if (vars != null)
            {
                foreach (var item in vars)
                {
                    numberOfItems++;
                    result.Items.Add(new S7AddressItemSpecificationDatagram
                    {
                        TransportSize = S7AddressItemSpecificationDatagram.GetTransportSize(item.Area, item.VarType),
                        ItemSpecLength = item.Length,
                        DbNumber = item.DbNumber,
                        Area = (byte)item.Area,
                        Address = S7AddressItemSpecificationDatagram.GetAddress(item.Offset, item.VarType)
                    });
                }

                foreach (var item in vars)
                {
                    numberOfItems--;
                    var transportSize = S7DataItemSpecification.GetTransportSize(item.VarType);
                    result.Data.Add(new S7DataItemSpecification
                    {
                        ReturnCode = 0x00,
                        TransportSize = transportSize,
                        Length = (ushort)item.Data.Length, //S7DataItemSpecification.GetDataLength(item.Data.Length, transportSize),
                        Data = item.Data,
                        FillByte = numberOfItems == 0 || item.Length % 2 == 0 ? new byte[0] : new byte[1]
                    });
                }
            }
            result.Header.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.Header.DataLength = (ushort)(S7DataItemSpecification.GetDataLength(vars) + result.Items.Count * 4);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }



        public static Memory<byte> TranslateToMemory(S7WriteJobDatagram datagram)
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

            foreach (var item in datagram.Data)
            {
                S7DataItemSpecification.TranslateToMemory(item, result.Slice(offset));
                offset += item.GetSpecificationLength();
            }

            return result;
        }

        public static S7WriteJobDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7WriteJobDatagram
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
