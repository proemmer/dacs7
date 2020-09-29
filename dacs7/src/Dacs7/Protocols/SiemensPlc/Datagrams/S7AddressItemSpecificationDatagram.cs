// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;

namespace Dacs7.Protocols.SiemensPlc
{

    internal sealed class S7AddressItemSpecificationDatagram
    {

        public byte VariableSpecification { get; set; } = 0x12;

        public byte LengthOfAddressSpecification { get; set; } = 0x0a;


        public byte SyntaxId { get; set; } = 0x10; //S7Any

        public byte TransportSize { get; set; }

        public ushort ItemSpecLength { get; set; }

        public ushort DbNumber { get; set; }

        public byte Area { get; set; }

        public Memory<byte> Address { get; set; } //= new byte[3];

        public int Offset { get; set; }


        public static byte GetTransportSize(PlcArea area, Type t)
        {
            if (area == PlcArea.CT || area == PlcArea.TM)
                return 0x01;

            if (t.IsArray)
                t = t.GetElementType();


            if (t == typeof(bool))
                return (byte)ItemDataTransportSize.Bit;

            if (t == typeof(byte) || t == typeof(string) || t == typeof(Memory<byte>))
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

            if (t == typeof(float))
                return (byte)ItemDataTransportSize.Real;

            return 0;
        }

        public static byte[] GetAddress(int offset, Type t)
        {
            offset = t == typeof(bool) ? offset : (offset * 8);
            var address = new byte[3];
            address[2] = (byte)(offset & 0x000000FF);
            offset >>= 8;
            address[1] = (byte)(offset & 0x000000FF);
            offset >>= 8;
            address[0] = (byte)(offset & 0x000000FF);
            return address;
        }

        public static int CalcOffsetFromAddress(Memory<byte> address, Type t)
        {
            Memory<byte> mem = new byte[4];
            address.CopyTo(mem.Slice(1));
            var offset = BinaryPrimitives.ReadInt32BigEndian(mem.Span);
            offset >>= 3;

            return t != typeof(bool) ? offset : (offset * 8);
        }


        public int GetSpecificationLength() => 12;



        public static Memory<byte> TranslateToMemory(S7AddressItemSpecificationDatagram datagram, Memory<byte> memory)
        {
            var result = memory.IsEmpty ? new Memory<byte>(new byte[12]) : memory;  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.VariableSpecification;
            span[1] = datagram.LengthOfAddressSpecification;
            span[2] = datagram.SyntaxId;
            span[3] = datagram.TransportSize;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), datagram.ItemSpecLength);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), datagram.DbNumber);
            span[8] = datagram.Area;
            if (!datagram.Address.IsEmpty)
                datagram.Address.CopyTo(result.Slice(9));

            return result;
        }

        public static S7AddressItemSpecificationDatagram TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7AddressItemSpecificationDatagram
            {
                VariableSpecification = span[0],
                LengthOfAddressSpecification = span[1],
                SyntaxId = span[2],
                TransportSize = span[3],
                ItemSpecLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2)),
                DbNumber = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2)),
                Area = span[8]
            };
            result.Address = new byte[3];
            data.Slice(9, 3).CopyTo(result.Address);
            result.Offset = S7AddressItemSpecificationDatagram.CalcOffsetFromAddress(result.Address, result.TransportSize == (byte)ItemDataTransportSize.Bit ? typeof(bool) : typeof(byte));

            return result;
        }



    }
}
