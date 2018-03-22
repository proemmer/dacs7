// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7DataItemSpecification
    {

        public byte ReturnCode{ get; set; }


        public byte TransportSize{ get; set; }
        public ushort Length{ get; set; }


        public Memory<byte> Data{ get; set; }


        public Memory<byte> FillByte { get; set; }


        #region IDatagramPropertyConverter
        public object ConvertLength()
        {
            var ts = (DataTransportSize)TransportSize;
            if (ts != DataTransportSize.OctetString && ts != DataTransportSize.Real && ts != DataTransportSize.Bit)
                return (ushort)((ushort)Length * 8);
            return (ushort)Length;
        }


        public ushort ConvertLengthBack()
        {
            var ts = (DataTransportSize)TransportSize;
            if (ts != DataTransportSize.OctetString && ts != DataTransportSize.Real && ts != DataTransportSize.Bit)
                return (ushort)((ushort)Length >> 3);  // value / 3
            return (ushort)Length;
        }
        #endregion

        #region Helper
        public static ushort GetDataLength(IEnumerable<WriteItemSpecification> items)
        {
            var fullLength = (ushort)0;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if ((fullLength % 2) != 0)
                        fullLength++;
                    fullLength += item.Length;

                    if (item.VarType == typeof(string))
                        fullLength += 2;
                }
            }
            return fullLength;
        }

        public ushort GetSpecificationLength()
        {
            return (ushort)(4 + Length);
        }

        public static byte GetTransportSize(Type t)
        {

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
                return (byte)DataTransportSize.Bit;

            if (t == typeof(byte) || t == typeof(string))
                return (byte)DataTransportSize.Byte;

            if (t == typeof(ushort))
                return (byte)DataTransportSize.Int;

            return 0;
        }

        public static ushort GetDataLength(int datalength, byte transportSize)
        {
            if (transportSize != (byte)DataTransportSize.OctetString && transportSize != (byte)DataTransportSize.Real && transportSize != (byte)DataTransportSize.Bit)
                datalength = datalength * 8;
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
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2, 2), GetDataLength(datagram.Length, datagram.TransportSize));
            datagram.Data = new byte[datagram.Length];
            datagram.Data.CopyTo(result.Slice(4, datagram.Length));
            // datagram.Data.CopyTo(result.Slice(4 + datagram.Length, datagram.Length));
            return result;
        }

        public static S7DataItemSpecification TranslateFromMemory(Memory<byte> data)
        {
            var span = data.Span;
            var result = new S7DataItemSpecification
            {
                ReturnCode = span[0],
                TransportSize = span[1],
                Length = SetDataLength(BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2)), span[1])
            };
            result.Data = new byte[result.Length];
            data.Slice(4, result.Length).CopyTo(result.Data);

            return result;
        }


    }
}
