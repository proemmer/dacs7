// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.Fdl
{
    internal class S7ConnectionConfig
    {
        public byte RoutingEnabled { get; set; }
        public byte B01 { get; set; } = 0x02;
        public byte B02 { get; set; } = 0x01;
        public byte B03 { get; set; }
        public byte B04 { get; set; } = 0x0C;
        public byte B05 { get; set; } = 0x01;
        public byte B06 { get; set; }
        public byte B07 { get; set; }
        public byte B08 { get; set; }
        public byte[] Destination { get; set; }  // 4 byte
        public byte B13 { get; set; }
        public byte B14 { get; set; }
        public byte B15 { get; set; } = 0x01;
        public byte B16 { get; set; }
        public byte B17 { get; set; } = 0x02;
        public byte ConnectionType { get; set; }
        public byte RackSlot { get; set; }
        public byte B20 { get; set; }
        public byte SizeToEnd { get; set; }
        public byte SizeOfSubnet { get; set; }
        public byte Subnet1 { get; set; }
        public byte Subnet2 { get; set; }
        public byte B25 { get; set; }
        public byte B26 { get; set; }
        public byte Subnet3 { get; set; }
        public byte Subnet4 { get; set; }
        public byte SizeOfRoutingDestination { get; set; } = 4;
        public byte[] RoutingDestination { get; set; }



        public static S7ConnectionConfig BuildS7ConnectionConfig(FdlProtocolContext context)
        {
            var result = new S7ConnectionConfig
            {
                RackSlot = (byte)(context.Slot + context.Rack * 32),
                ConnectionType = (byte)context.ConnectionType
            };

            if (context.IsEthernet)
            {
                result.Destination = context.Address.GetAddressBytes();
            }
            else
            {
                result.Destination = new byte[] { (byte)context.MpiAddress, 0x00, 0x00, 0x00 };
            }

            if (context.EnableRouting)
            {
                result.RoutingEnabled = 0x01;
                // TODO!
            }

            return result;
        }


        public static Memory<byte> TranslateToMemory(S7ConnectionConfig config)
        {
            var mem = new Memory<byte>(new byte[126]);

            mem.Span[0] = config.RoutingEnabled;
            mem.Span[1] = config.B01;
            mem.Span[2] = config.B02;
            mem.Span[3] = config.B03;
            mem.Span[4] = config.B04;
            mem.Span[5] = config.B05;
            mem.Span[6] = config.B06;
            mem.Span[7] = config.B07;
            mem.Span[8] = config.B08;
            config.Destination.CopyTo(mem.Span.Slice(9, 4));
            mem.Span[13] = config.B13;
            mem.Span[14] = config.B14;
            mem.Span[15] = config.B15;
            mem.Span[16] = config.B16;
            mem.Span[17] = config.B17;
            mem.Span[18] = config.ConnectionType;
            mem.Span[19] = config.RackSlot;
            mem.Span[20] = config.B20;
            mem.Span[21] = config.SizeToEnd;
            mem.Span[22] = config.SizeOfSubnet;
            mem.Span[23] = config.Subnet1;
            mem.Span[24] = config.Subnet2;
            mem.Span[25] = config.B25;
            mem.Span[26] = config.B26;
            mem.Span[27] = config.Subnet3;
            mem.Span[28] = config.Subnet4;
            mem.Span[29] = config.SizeOfRoutingDestination;
            config.RoutingDestination.CopyTo(mem.Span.Slice(30, config.SizeOfRoutingDestination));


            return mem;
        }
    }
}
