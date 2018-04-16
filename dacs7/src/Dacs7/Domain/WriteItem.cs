// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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


        public static WriteItem Create<T>(string area, ushort offset, T data)
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
        public static WriteItem CreateChild(WriteItem item, ushort offset, ushort length)
        {
            var result = ReadItem.CreateChild(item, offset, length).Clone();
            result.Data = item.Data.Slice(offset - item.Offset, length);
            return result;
        }

        internal static ushort GetDataItemCount<T>(T data)
        {
            switch (data)
            {
                case byte b:
                    return 1;
                case byte[] ba:
                    return (ushort)ba.Length;
                case Memory<byte> ba:
                    return (ushort)ba.Length;
                case char c:
                    return 1;
                case char[] ca:
                    return (ushort)ca.Length;
                case string s:
                    return (ushort)(s.Length + 2);
                case Int16 i16:
                        return 1;
                case UInt16 ui16:
                        return 1;
                case Int32 i32:
                        return 1;
                case UInt32 ui32:
                        return 1;
                case Int64 i64:
                        return 1;
                case UInt64 ui64:
                        return 1;
                case bool b:
                        return 1;
                case bool[] bb:
                    return (ushort)bb.Length;
                case object[] oa:
                    return (ushort)oa.Length;
                case IEnumerable<object> ie:
                    return (ushort)ie.Count();
            }
            return 1;
        }

        internal static Memory<byte> ConvertDataToMemory(WriteItem item, object data)
        {
            if (data is string && item.ResultType != typeof(string))
                data = Convert.ChangeType(data, item.ResultType);

            switch (data)
            {
                case byte b:
                    return new byte[] { b };
                case byte[] ba:
                    return ba;
                case Memory<byte> ba:
                    return ba;
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
            }
            throw new InvalidCastException();
        }

    }
}
