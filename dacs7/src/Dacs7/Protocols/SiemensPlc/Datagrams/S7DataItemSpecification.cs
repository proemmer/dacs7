// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    internal sealed class S7DataItemSpecification
    {

        public byte ReturnCode { get; set; }


        public byte TransportSize { get; set; }
        public ushort Length { get; set; }


        public Memory<byte> Data { get; set; }


        public Memory<byte> FillByte { get; set; }


        public ushort ElementSize { get; set; }


        #region IDatagramPropertyConverter
        public object ConvertLength()
        {
            var ts = (DataTransportSize)TransportSize;
            if (ts != DataTransportSize.OctetString && ts != DataTransportSize.Real && ts != DataTransportSize.Bit)
                return (ushort)(Length * 8);
            return Length;
        }


        public ushort ConvertLengthBack()
        {
            var ts = (DataTransportSize)TransportSize;
            if (ts != DataTransportSize.OctetString && ts != DataTransportSize.Real && ts != DataTransportSize.Bit)
                return (ushort)(Length >> 3);  // value / 3
            return Length;
        }
        #endregion

        #region Helper
        public static ushort GetDataLength(IEnumerable<WriteItem> items)
        {
            var fullLength = (ushort)0;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if ((fullLength % 2) != 0)
                        fullLength++;
                    fullLength += (ushort)item.Data.Length;

                    // No special handling for string because we have already translated it to a byte array
                    //if (item.VarType == typeof(string))
                    //    fullLength += 2;
                }
            }
            return fullLength;
        }

        public ushort GetSpecificationLength() => (ushort)(4 + (ElementSize * Length));

        public static byte GetTransportSize(Type t)
        {

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
                return (byte)DataTransportSize.Bit;

            if (t == typeof(byte) || t == typeof(string) || t == typeof(Memory<byte>))
                return (byte)DataTransportSize.Byte;

            if (t == typeof(ushort))
                return (byte)DataTransportSize.Int;

            return 0;
        }

        public static byte GetTransportSize(PlcArea area, Type t)
        {
            if (area == PlcArea.CT || area == PlcArea.TM)
                return (byte)DataTransportSize.OctetString;

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
            {
                return (byte)DataTransportSize.Bit;
            }

            if (t == typeof(byte) || t == typeof(string) || t == typeof(Memory<byte>))
            {
                return (byte)DataTransportSize.Byte;
            }

            if (t == typeof(char))
            {
                return (byte)DataTransportSize.OctetString;
            }

            if (t == typeof(short))
            {
                return (byte)DataTransportSize.Int;
            }

            if (t == typeof(int))
            {
                return (byte)DataTransportSize.Int;
            }

            if (t == typeof(ushort))
            {
                return (byte)DataTransportSize.Byte;
            }

            if (t == typeof(uint))
            {
                return (byte)DataTransportSize.Byte;
            }

            if (t == typeof(float))
            {
                return (byte)DataTransportSize.Real;
            }

            return (byte)DataTransportSize.Byte;
        }

        public static ushort GetDataLength(int datalength, byte transportSize)
        {
            if (transportSize != (byte)DataTransportSize.OctetString && transportSize != (byte)DataTransportSize.Real && transportSize != (byte)DataTransportSize.Bit)
                datalength *= 8;
            return (ushort)datalength;
        }

        public static ushort SetDataLength(int datalength, byte transportSize)
        {
            var ts = (DataTransportSize)transportSize;
            if (ts != DataTransportSize.OctetString && ts != DataTransportSize.Real && ts != DataTransportSize.Bit)
                return (ushort)((ushort)datalength >> 3);  // value / 3
            return (ushort)datalength;
        }
        #endregion



        public static Memory<byte> TranslateToMemory(S7DataItemSpecification datagram, Memory<byte> memory)
        {
            var result = memory.IsEmpty ? new Memory<byte>(new byte[12]) : memory;  // check if we could use ArrayBuffer
            var span = result.Span;

            span[0] = datagram.ReturnCode;
            span[1] = datagram.TransportSize;

            if (datagram.ReturnCode == (byte)ItemResponseRetValue.Success || datagram.ReturnCode == (byte)ItemResponseRetValue.Reserved)
            {
                var size = datagram.ElementSize * datagram.Length;
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), GetDataLength(size, datagram.TransportSize));
                datagram.Data.CopyTo(result.Slice(4, size));
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), 0);
            }
            return result;
        }

        public static S7DataItemSpecification TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7DataItemSpecification
            {
                ReturnCode = span[0],
                TransportSize = span[1],
                Length = SetDataLength(BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2)), span[1]),
                ElementSize = TransportSizeToElementSize((DataTransportSize)span[1])
            };
            var size = result.Length;
            result.Data = new byte[size];
            data.Slice(4, size).CopyTo(result.Data);

            return result;
        }


        private static ushort TransportSizeToElementSize(DataTransportSize t) => t switch {
                   DataTransportSize.Bit => 1,
                   DataTransportSize.Byte => 1,
                   DataTransportSize.Int => 2,
                   DataTransportSize.Dint => 3,
                   DataTransportSize.Real => 4,
                   DataTransportSize.OctetString => 2,
                   _ => 1
               };

    }
}