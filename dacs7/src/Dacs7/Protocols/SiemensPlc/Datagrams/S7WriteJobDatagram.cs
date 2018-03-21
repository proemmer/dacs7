// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7WriteJobDatagram
    {

        public S7CommHeaderDatagram CommHeader { get; set; } = new S7CommHeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };

        public byte Function { get; set; } = 0x05; //Write Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();

        public List<S7DataItemSpecification> Data { get; set; } = new List<S7DataItemSpecification>();

        public S7WriteJobDatagram BuildWrite(SiemensPlcProtocolContext context, int id, IEnumerable<WriteItemSpecification> vars)
        {
            var numberOfItems = 0;
            var result = new S7WriteJobDatagram();
            result.CommHeader.ProtocolDataUnitReference = (ushort)id;
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
            result.CommHeader.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.CommHeader.DataLength = (ushort)(S7DataItemSpecification.GetDataLength(vars) + result.Items.Count * 4);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }
    }
}
