// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7ReadJobDatagram
    {

        public S7CommHeaderDatagram CommHeader { get; set; } = new S7CommHeaderDatagram
        {
            PduType = 0x01, //Job - > Should be a marker
            DataLength = 0,
            ParamLength = 14 // default fo r1 item
        };


        public byte Function { get; set; } = 0x04; //Read Var

        public byte ItemCount { get; set; } = 0x00;

        public List<S7AddressItemSpecificationDatagram> Items { get; set; } = new List<S7AddressItemSpecificationDatagram>();





        public S7ReadJobDatagram BuildRead(SiemensPlcProtocolContext context, int id, IEnumerable<ReadItemSpecification> vars)
        {
            var result = new S7ReadJobDatagram();
            result.CommHeader.ProtocolDataUnitReference = (ushort)id;
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
            result.CommHeader.ParamLength = (ushort)(2 + result.Items.Count * 12);
            result.ItemCount = (byte)result.Items.Count;
            return result;
        }




    }
}
