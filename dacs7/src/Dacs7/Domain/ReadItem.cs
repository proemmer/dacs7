// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
            else if (item.ResultType == typeof(byte[]))
            {
                return data.ToArray();
            }
            else if(item.ResultType == typeof(Memory<byte>))
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
                return ConvertMemoryToInt16(item, data);
            }
            else if (item.ResultType == typeof(List<Int16>))
            {
                return ConvertMemoryToInt16(item, data).ToList();
            }
            else if (item.ResultType == typeof(UInt16[]))
            {
                return ConvertMemoryToUInt16(item, data);
            }
            else if (item.ResultType == typeof(List<UInt16>))
            {
                return ConvertMemoryToUInt16(item, data).ToList();
            }
            else if (item.ResultType == typeof(Int32[]))
            {
                return ConvertMemoryToInt32(item, data);
            }
            else if (item.ResultType == typeof(List<Int32>))
            {
                return ConvertMemoryToInt32(item, data).ToList();
            }
            else if (item.ResultType == typeof(UInt32[]))
            {
                return ConvertMemoryToUInt32(item, data);
            }
            else if (item.ResultType == typeof(List<UInt32>))
            {
                return ConvertMemoryToUInt32(item, data).ToList();
            }
            else if (item.ResultType == typeof(Int64[]))
            {
                return ConvertMemoryToInt64(item, data);
            }
            else if (item.ResultType == typeof(List<Int64>))
            {
                return ConvertMemoryToInt64(item, data).ToList();
            }
            else if (item.ResultType == typeof(UInt64[]))
            {
                return ConvertMemoryToUInt64(item, data);
            }
            else if (item.ResultType == typeof(List<UInt64>))
            {
                return ConvertMemoryToUInt64(item, data).ToList();
            }
            else if (item.ResultType == typeof(Single[]))
            {
                return ConvertMemoryToSingle(item, data);
            }
            else if (item.ResultType == typeof(List<Single>))
            {
                return ConvertMemoryToSingle(item, data).ToList();
            }
            throw new InvalidCastException();
        }

        private static Single[] ConvertMemoryToSingle(ReadItem item, Memory<byte> data)
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

        private static UInt64[] ConvertMemoryToUInt64(ReadItem item, Memory<byte> data)
        {
            var result = new UInt64[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
            }
            return result;
        }

        private static Int64[] ConvertMemoryToInt64(ReadItem item, Memory<byte> data)
        {
            var result = new Int64[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
            }
            return result;
        }

        private static UInt32[] ConvertMemoryToUInt32(ReadItem item, Memory<byte> data)
        {
            var result = new UInt32[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 4).Span);
            }
            return result;
        }

        private static Int32[] ConvertMemoryToInt32(ReadItem item, Memory<byte> data)
        {
            var result = new Int32[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadInt32BigEndian(data.Slice(i * 4).Span);
            }
            return result;
        }

        private static UInt16[] ConvertMemoryToUInt16(ReadItem item, Memory<byte> data)
        {
            var result = new UInt16[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i * 2).Span);
            }
            return result;
        }

        private static Int16[] ConvertMemoryToInt16(ReadItem item, Memory<byte> data)
        {
            var result = new Int16[item.NumberOfItems];
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadInt16BigEndian(data.Slice(i * 2).Span);
            }
            return result;
        }





        internal static Memory<byte> ConvertDataToMemory(ReadItem item, object data)
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

                case List<Int16> i16:
                    {
                        return ConvertInt16ToMemory(i16);
                    }
                case Int16[] i16:
                    {
                        return ConvertInt16ToMemory(i16);
                    }
                case List<UInt16> ui16:
                    {
                        return ConvertUInt16ToMemory(ui16);
                    }
                case UInt16[] ui16:
                    {
                        return ConvertUInt16ToMemory(ui16);
                    }
                case List<Int32> i32:
                    {
                        return ConvertIn32ToMemory(i32);
                    }
                case Int32[] i32:
                    {
                        return ConvertIn32ToMemory(i32);
                    }
                case List<UInt32> ui32:
                    {
                        return ConvertUInt32ToMemory(ui32);
                    }
                case UInt32[] ui32:
                    {
                        return ConvertUInt32ToMemory(ui32);
                    }
                case List<Int64> i64:
                    {
                        return ConvertInt64ToMemory(i64);
                    }
                case Int64[] i64:
                    {
                        return ConvertInt64ToMemory(i64);
                    }
                case List<UInt64> ui64:
                    {
                        return ConvertUInt64ToMemory(ui64);
                    }
                case UInt64[] ui64:
                    {
                        return ConvertUInt64ToMemory(ui64);
                    }
                case List<Single> single:
                    {
                        return ConvertSingleToMemory(single);
                    }
                case Single[] single:
                    {
                        return ConvertSingleToMemory(single);
                    }

            }
            throw new InvalidCastException();
        }

        private static Memory<byte> ConvertSingleToMemory(IList<float> single)
        {
            // TODO: Find a Span method to do this
            var buffer = new byte[4 * single.Count];
            for (int i = 0; i < single.Count; i++)
            {
                WriteSingleBigEndian(single[i], buffer, i * 4);
            }
            return buffer;
        }

        private static Memory<byte> ConvertUInt64ToMemory(IList<ulong> ui64)
        {
            Memory<byte> result = new byte[8 * ui64.Count];
            for (int i = 0; i < ui64.Count; i++)
            {
                BinaryPrimitives.WriteUInt64BigEndian(result.Span.Slice(i * 8), ui64[i]);
            }

            return result;
        }

        private static Memory<byte> ConvertInt64ToMemory(IList<long> i64)
        {
            Memory<byte> result = new byte[8 * i64.Count];
            for (int i = 0; i < i64.Count; i++)
            {
                BinaryPrimitives.WriteInt64BigEndian(result.Span.Slice(i * 8), i64[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertUInt32ToMemory(IList<uint> ui32)
        {
            Memory<byte> result = new byte[4 * ui32.Count];
            for (int i = 0; i < ui32.Count; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(result.Span.Slice(i * 4), ui32[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertIn32ToMemory(IList<int> i32)
        {
            Memory<byte> result = new byte[4 * i32.Count];
            for (int i = 0; i < i32.Count; i++)
            {
                BinaryPrimitives.WriteInt32BigEndian(result.Span.Slice(i * 4), i32[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertUInt16ToMemory(IList<ushort> ui16)
        {
            Memory<byte> result = new byte[2 * ui16.Count];
            for (int i = 0; i < ui16.Count; i++)
            {
                BinaryPrimitives.WriteUInt16BigEndian(result.Span.Slice(i * 2), ui16[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertInt16ToMemory(IList<short> i16)
        {
            Memory<byte> result = new byte[2 * i16.Count];
            for (int i = 0; i < i16.Count; i++)
            {
                BinaryPrimitives.WriteInt16BigEndian(result.Span.Slice(i * 2), i16[i]);
            }
            return result;
        }






        private static byte[] Swap4BytesInBuffer(byte[] rawdata, int offset = 0)
        {
            var b = rawdata[offset];
            rawdata[offset] = rawdata[offset + 3];
            rawdata[offset + 3] = b;
            b = rawdata[offset + 1];
            rawdata[offset + 1] = rawdata[offset + 2];
            rawdata[offset + 2] = b;
            return rawdata;
        }


        private static ReadItem SetupTypes<T>(ReadItem result)
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                result.VarType = t.GetElementType();
                result.ResultType = t;
            }
            else if (t.GetInterface(typeof(IList<>).FullName) != null)
            {
                result.VarType = t.GetGenericArguments().Single();
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
            if (item.ResultType == typeof(byte) || item.ResultType == typeof(byte[]) || item.ResultType == typeof(List<byte>) || 
                item.ResultType == typeof(Memory<byte>) ||
                item.ResultType == typeof(string) || item.ResultType == typeof(bool) ||
                item.ResultType == typeof(char) || item.ResultType == typeof(char[]) || item.ResultType == typeof(List<char>) ||
                item.ResultType == typeof(UInt16) || item.ResultType == typeof(UInt16[]) || item.ResultType == typeof(List<UInt16>) ||
                item.ResultType == typeof(UInt32) || item.ResultType == typeof(UInt32[]) || item.ResultType == typeof(List<UInt32>) ||
                item.ResultType == typeof(Int16) || item.ResultType == typeof(Int16[]) || item.ResultType == typeof(List<Int16>) ||
                item.ResultType == typeof(Int32) || item.ResultType == typeof(Int32[]) || item.ResultType == typeof(List<Int32>) ||
                item.ResultType == typeof(Single) || item.ResultType == typeof(Single[]) || item.ResultType == typeof(List<Single>))
            {
                return;
            }

            throw new Dacs7TypeNotSupportedException(item.ResultType);
        }

        private static Memory<byte> WriteSingleBigEndian(Single value, byte[] buffer = null, int offset = 0)
        {
            var rawdata = buffer ?? new byte[Marshal.SizeOf(value)];
            var handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject() + offset, false);
            handle.Free();
            Swap4BytesInBuffer(rawdata, offset);
            return rawdata;
        }

    }
}
