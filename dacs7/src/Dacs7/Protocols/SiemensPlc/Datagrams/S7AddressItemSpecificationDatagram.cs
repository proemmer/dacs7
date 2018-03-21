// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;

namespace Dacs7.Protocols.SiemensPlc
{

    public class S7AddressItemSpecificationDatagram
    {

        public byte VariableSpecification { get; set; } = 0x12;

        public byte LengthOfAddressSpecification { get; set; } = 0x0a;


        public byte SyntaxId { get; set; } = 0x10; //S7Any

        public byte TransportSize { get; set; }

        public ushort ItemSpecLength { get; set; }

        public ushort DbNumber { get; set; }

        public byte Area { get; set; }

        public byte[] Address { get; set; } = new byte[3];


        public static byte GetTransportSize(PlcArea area, Type t)
        {
            if(area == PlcArea.CT || area == PlcArea.TM)
                return 0x01;

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
                return (byte)ItemDataTransportSize.Bit;

            if (t == typeof(byte) || t == typeof(string))
                return (byte)ItemDataTransportSize.Byte;

            if (t == typeof(char))
                return (byte)ItemDataTransportSize.Char;

            if (t == typeof(ushort))
                return (byte)ItemDataTransportSize.Word;

            if (t == typeof(short))
                return (byte)ItemDataTransportSize.Int;

            if (t == typeof(uint))
                return (byte)ItemDataTransportSize.Dword;

            if (t == typeof(int))
                return (byte)ItemDataTransportSize.Dint;

            if (t == typeof(Single))
                return (byte)ItemDataTransportSize.Real;

            return 0;
        }

        public static byte[] GetAddress(int offset, Type t)
        {
            offset = t == typeof(bool) ? offset : (offset * 8);
            var address = new byte[3];
            address[2] = (byte)(offset & 0x000000FF);
            offset = offset >> 8;
            address[1] = (byte)(offset & 0x000000FF);
            offset = offset >> 8;
            address[0] = (byte)(offset & 0x000000FF);
            return address;
        }
    }
}
