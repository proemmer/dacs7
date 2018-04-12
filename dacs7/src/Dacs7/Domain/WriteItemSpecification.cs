// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;

namespace Dacs7
{
    public class WriteItemSpecification : ReadItemSpecification
    {
        internal Memory<byte> Data { get; set; }

        internal WriteItemSpecification()
        {
        }

        internal override WriteItemSpecification Clone()
        {
            var clone = base.Clone();
            clone.Data = Data;
            return clone;
        }

        internal WriteItemSpecification Clone(Memory<byte> data)
        {
            var clone = base.Clone();
            clone.Data = data;
            return clone;
        }



        public static WriteItemSpecification CreateFromTag(string tag, object data)
        {
            var result = CreateFromTag(tag).Clone();
            result.Data = ConvertDataToMemory(result, data);
            return result;
        }


        public static WriteItemSpecification Create<T>(string area, ushort offset, T data)
        {
            var result = ReadItemSpecification.Create<T>(area, offset, (ushort)(data is object[] x ? x.Length : 1) ).Clone();
            result.Data = ConvertDataToMemory(result, data);
            return result;
        }



        internal static Memory<byte> ConvertDataToMemory(WriteItemSpecification item, object data)
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
