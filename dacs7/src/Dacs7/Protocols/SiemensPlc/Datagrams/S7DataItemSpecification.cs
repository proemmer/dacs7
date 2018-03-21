// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Collections.Generic;

namespace Dacs7.Protocols.SiemensPlc
{
    public class S7DataItemSpecification
    {

        public byte ReturnCode{ get; set; }


        public byte TransportSize{ get; set; }
        public ushort Length{ get; set; }


        public byte[] Data{ get; set; }


        public byte[] FillByte { get; set; }


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
        #endregion
    }
}
