// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace Dacs7
{
    public class ReadItem
    {
        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public int Offset { get; private set; }
        public ushort NumberOfItems { get; private set; }
        public Type VarType { get; private set; }
        public Type ResultType { get; private set; }

        internal int CallbackReference { get; set; }
        internal ReadItem Parent { get; set; }
        internal bool IsPart => Parent != null;

        internal ReadItem()
        {

        }


        internal virtual WriteItem Clone()
        {
            return new WriteItem
            {
                Area = Area,
                DbNumber = DbNumber,
                Offset = Offset,
                NumberOfItems = NumberOfItems,
                VarType = VarType,
                ResultType = ResultType
            };
        }

        /// <summary>
        /// Create a readitem from a given Tag
        /// </summary>
        /// <param name="tag">Format:  [Area].[Offset],[Type],[Number Of Items  or in Case of bytes and strings the length of them]</param>
        /// <returns></returns>
        public static ReadItem CreateFromTag(string tag)
        {
            if (TagParser.TryParseTag(tag, out var result))
            {
                return new ReadItem
                {
                    Area = result.Area,
                    DbNumber = result.DbNumber,
                    Offset = result.Offset,
                    NumberOfItems = result.Length,
                    VarType = result.VarType,
                    ResultType = result.ResultType
                };
            }
            return null;
        }

        /// <summary>
        /// Create a read item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static ReadItem Create<T>(string area, int offset, ushort length = 1)
        {
            if (!TagParser.TryDetectArea(area.AsSpan(), out var selector, out var db ))
            {
                throw new ArgumentException($"Invalid area <{area}>");
            }

            return SetupTypes<T>(new ReadItem
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                NumberOfItems = length
            });
        }

        /// <summary>
        /// Create a child read item. (More than one items are  sharing their memory)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        internal static ReadItem CreateChild(ReadItem item, int offset, ushort length)
        {
            return new ReadItem
            {
                Area = item.Area,
                DbNumber = item.DbNumber,
                Offset = offset,
                NumberOfItems = length,
                VarType = item.VarType,
                ResultType = item.ResultType,
                Parent = item
            };
        }


        internal static object ConvertMemoryToData(ReadItem item, Memory<byte> data)
        {

            if (item.ResultType == typeof(byte))
            {
                return data.Span[0];
            }
            else if (item.ResultType == typeof(byte[]) || item.ResultType == typeof(Memory<byte>))
            {
                return data;
            }
            else if (item.ResultType == typeof(bool))
            {
                return data.Span[0] == 0x01;
            }
            else if (item.ResultType == typeof(bool[]))
            {
                var result = new bool[item.NumberOfItems];
                var index = 0;
                foreach (var aa in data.Span.Slice(0, item.NumberOfItems))
                {
                    result[index++] = aa == 0x01;
                }
                return result;
            }
            else if (item.ResultType == typeof(char))
            {
                return Convert.ToChar(data.Span[0]);
            }
            else if (item.ResultType == typeof(char[]) || item.ResultType == typeof(Memory<char>))
            {
                var result = new char[item.NumberOfItems];
                var index = 0;
                foreach (var aa in data.Span.Slice(0, item.NumberOfItems))
                {
                    result[index++] = Convert.ToChar(aa);
                }
                return result;
            }
            else if (item.ResultType == typeof(string))
            {
                var length = data.Span[1];
                return Encoding.ASCII.GetString(data.Span.Slice(2, length).ToArray());
            }
            else if (item.ResultType == typeof(Int16))
            {
                return BinaryPrimitives.ReadInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt16))
            {
                return BinaryPrimitives.ReadUInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(Int32))
            {
                return BinaryPrimitives.ReadInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt32))
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(Int64))
            {
                return BinaryPrimitives.ReadInt64BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt64))
            {
                return BinaryPrimitives.ReadUInt64BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(Single))
            {
                // TODO: Find a Span method to do this
                return BitConverter.ToSingle(Swap4BytesInBuffer(data.Span.ToArray()), 0);
            }
            else if (item.ResultType == typeof(Int16[]))
            {
                var result = new Int16[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadInt16BigEndian(data.Slice(i * 2).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(UInt16[]))
            {
                var result = new UInt16[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i * 2).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(Int32[]))
            {
                var result = new Int32[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadInt32BigEndian(data.Slice(i * 4).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(UInt32[]))
            {
                var result = new UInt32[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 4).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(Int64[]))
            {
                var result = new Int64[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(UInt64[]))
            {
                var result = new UInt64[item.NumberOfItems];
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
                }
                return result;
            }
            else if (item.ResultType == typeof(Single[]))
            {
                // TODO: Find a Span method to do this
                var result = new Single[item.NumberOfItems];
                var buffer = data.Span.ToArray();
                for (int i = 0; i < item.NumberOfItems; i++)
                {
                    var offset = i * 4;
                    // we nedd the offset twice because SwapBuffer returns the whole buffer it only swaps the bytes beginning of the given context
                    result[i] = BitConverter.ToSingle(Swap4BytesInBuffer(buffer, i * 4), offset);
                }
                return result;
            }
            throw new InvalidCastException();
        }

        private static ReadItem SetupTypes<T>(ReadItem result)
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                result.VarType = t.GetElementType();
                result.ResultType = t;
            }
            else if(t == typeof(string) )
            {
                result.VarType = result.ResultType  = t;
                result.NumberOfItems += 2;
            }
            else if (t == typeof(Memory<byte>))
            {
                result.VarType = result.ResultType = t;
            }
            else
            {
                result.VarType = t;
                result.ResultType = result.NumberOfItems > 1 ? typeof(T[]) : result.VarType;
            }


            EnsureSupportedType(result);

            return result;
        }

        private static void EnsureSupportedType(ReadItem item)
        {
            if (item.ResultType == typeof(byte) || item.ResultType == typeof(byte[]) || 
                item.ResultType == typeof(Memory<byte>) ||
                item.ResultType == typeof(string) || item.ResultType == typeof(bool) ||
                item.ResultType == typeof(char) || item.ResultType == typeof(char[]) ||
                item.ResultType == typeof(UInt16) || item.ResultType == typeof(UInt16[]) ||
                item.ResultType == typeof(UInt32) || item.ResultType == typeof(UInt32[]) ||
                item.ResultType == typeof(Int16) || item.ResultType == typeof(Int16[]) ||
                item.ResultType == typeof(Int32) || item.ResultType == typeof(Int32[]) ||
                item.ResultType == typeof(Single) || item.ResultType == typeof(Single[]))
            {
                return;
            }

            throw new Dacs7TypeNotSupportedException(item.ResultType);
        }


        protected static byte[] Swap4BytesInBuffer(byte[] rawdata, int offset = 0)
        {
            var b = rawdata[offset];
            rawdata[offset] = rawdata[offset + 3];
            rawdata[offset + 3] = b;
            b = rawdata[offset + 1];
            rawdata[offset + 1] = rawdata[offset + 2];
            rawdata[offset + 2] = b;
            return rawdata;
        }

    }
}
