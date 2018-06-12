// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dacs7
{
    public class WriteItem : ReadItem
    {
        internal Memory<byte> Data { get; set; }

        internal new WriteItem Parent { get; set; }

        internal WriteItem()
        {
        }

        internal override WriteItem Clone()
        {
            var clone = base.Clone();
            clone.Data = Data;
            return clone;
        }

        internal WriteItem Clone(Memory<byte> data)
        {
            var clone = base.Clone();
            clone.Data = data;
            return clone;
        }



        public static WriteItem CreateFromTag(string tag, object data)
        {
            var result = CreateFromTag(tag).Clone();
            result.Data = ConvertDataToMemory(result, data);
            return result;
        }


        public static WriteItem Create<T>(string area, int offset, T data)
        {



            var result = Create<T>(area, offset, GetDataItemCount(data)).Clone();
            result.Data = ConvertDataToMemory(result, data);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static WriteItem CreateChild(WriteItem item, int offset, ushort length)
        {
            var result = ReadItem.CreateChild(item, offset, length).Clone();
            result.Data = item.Data.Slice(offset - item.Offset, length);
            return result;
        }

        internal static ushort GetDataItemCount<T>(T data)
        {
            if (typeof(T).IsArray)
            {
                return (ushort)(data as Array).Length;
            }
            else if (data is Memory<byte> ba)
            {
                return (ushort)ba.Length;
            }
            else if (data is string s)
            {
                return (ushort)(s.Length + 2);
            }
            else if (data is IEnumerable<object> en)
            {
                return (ushort)en.Count();
            }

            return 1;
        }



        internal static Memory<byte> ConvertDataToMemory(WriteItem item, object data)
        {
            if (data is string && item.ResultType != typeof(string))
            {
                data = Convert.ChangeType(data, item.ResultType, CultureInfo.InvariantCulture);
            }

            switch (data)
            {
                case byte b:
                    return new byte[] { b };
                case byte[] ba:
                    return ba;
                case Memory<byte> ba:
                    return ba;
                case bool b:
                    {
                        return new byte[] { b ? (byte)0x01 : (byte)0x00 };
                    }
                case bool[] b:
                    {
                        return b.Select(x => x ? (byte)0x01 : (byte)0x00).ToArray();
                    }
                case char c:
                    return new byte[] { Convert.ToByte(c) };
                case char[] ca:
                    return ca.Select(x => Convert.ToByte(x)).ToArray();
                case string s:
                    {
                        Memory<byte> result = new byte[s.Length + 2];
                        result.Span[0] = (byte)s.Length;
                        result.Span[1] = (byte)s.Length;
                        Encoding.ASCII.GetBytes(s).AsSpan().CopyTo(result.Span.Slice(2));
                        return result;
                    }
                case Int16 i16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteInt16BigEndian(result.Span, i16);
                        return result;
                    }
                case UInt16 ui16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(result.Span, ui16);
                        return result;
                    }
                case Int32 i32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteInt32BigEndian(result.Span, i32);
                        return result;
                    }
                case UInt32 ui32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteUInt32BigEndian(result.Span, ui32);
                        return result;
                    }
                case Single s:
                    {
                        // TODO: Find a Span method to do this
                        return WriteSingleBigEndian(s);
                    }
                case Int64 i64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteInt64BigEndian(result.Span, i64);
                        return result;
                    }
                case UInt64 ui64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteUInt64BigEndian(result.Span, ui64);
                        return result;
                    }
                case Int16[] i16:
                    {
                        Memory<byte> result = new byte[2 * i16.Length];
                        for (int i = 0; i < i16.Length; i++)
                        {
                            BinaryPrimitives.WriteInt16BigEndian(result.Span.Slice(i * 2), i16[i]);
                        }
                        return result;
                    }
                case UInt16[] ui16:
                    {
                        Memory<byte> result = new byte[2 * ui16.Length];
                        for (int i = 0; i < ui16.Length; i++)
                        {
                            BinaryPrimitives.WriteUInt16BigEndian(result.Span.Slice(i * 2), ui16[i]);
                        }
                        return result;
                    }
                case Int32[] i32:
                    {
                        Memory<byte> result = new byte[4 * i32.Length];
                        for (int i = 0; i < i32.Length; i++)
                        {
                            BinaryPrimitives.WriteInt32BigEndian(result.Span.Slice(i * 4), i32[i]);
                        }
                        return result;
                    }
                case UInt32[] ui32:
                    {
                        Memory<byte> result = new byte[4 * ui32.Length];
                        for (int i = 0; i < ui32.Length; i++)
                        {
                            BinaryPrimitives.WriteUInt32BigEndian(result.Span.Slice(i * 4), ui32[i]);
                        }
                        return result;
                    }
                case Int64[] i64:
                    {
                        Memory<byte> result = new byte[8 * i64.Length];
                        for (int i = 0; i < i64.Length; i++)
                        {
                            BinaryPrimitives.WriteInt64BigEndian(result.Span.Slice(i * 8), i64[i]);
                        }
                        return result;
                    }
                case UInt64[] ui64:
                    {
                        Memory<byte> result = new byte[8 * ui64.Length];
                        for (int i = 0; i < ui64.Length; i++)
                        {
                            BinaryPrimitives.WriteUInt64BigEndian(result.Span.Slice(i * 8), ui64[i]);
                        }
                        
                        return result;
                    }
                case Single[] single:
                    {
                        // TODO: Find a Span method to do this
                        var buffer = new byte[4 * single.Length];
                        for (int i = 0; i < single.Length; i++)
                        {
                            return WriteSingleBigEndian(single[i], buffer, i * 4);
                        }
                        return buffer;
                    }

            }
            throw new InvalidCastException();
        }


        public static Memory<byte> WriteSingleBigEndian(Single value, byte[] buffer = null, int offset = 0)
        {
            var rawdata = buffer ?? new byte[Marshal.SizeOf(value)];
            var handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            handle.Free();
            Swap4BytesInBuffer(rawdata, offset);
            return rawdata;
        }


    }
}
