// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dacs7.Domain.ConvertHelpers;

namespace Dacs7.Domain
{
    internal static class ConvertMemoryToDataExtensions
    {
        public static object ConvertMemoryToData(this ReadItem item, Memory<byte> data)
        {

            if (item.ResultType == typeof(byte))
            {
                return data.Span[0];
            }
            else if (item.ResultType == typeof(byte[]))
            {
                return data.ToArray();  // creates a new array :(
            }
            else if (item.ResultType == typeof(Memory<byte>))
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
                if (item.Encoding == PlcEncoding.Unicode)
                {
                    var max = BinaryPrimitives.ReadUInt16BigEndian(data.Span);
                    var length = BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(2));
                    var dataLength = (data.Span.Length - ReadItem.UnicodeStringHeaderSize);
                    var current = (short)(dataLength / 2);

                    length = (ushort)Math.Min(Math.Min(max, length), current);
#if SPANSUPPORT
                    return length > 0 ? Encoding.BigEndianUnicode.GetString(data.Slice(ReadItem.UnicodeStringHeaderSize, dataLength).Span) : string.Empty;
#else
                    return length > 0 ? Encoding.BigEndianUnicode.GetString(data.Span.Slice(ReadItem.UnicodeStringHeaderSize, dataLength).ToArray()) : string.Empty;
#endif

                }
                else
                {

                    var max = data.Span[0];
                    var length = data.Span[1];
                    var current = data.Span.Length - ReadItem.StringHeaderSize;

                    length = (byte)Math.Min(Math.Min(max, (int)length), current);
#if SPANSUPPORT
                    return length > 0 ? Encoding.ASCII.GetString(data.Slice(ReadItem.StringHeaderSize, length).Span) : string.Empty;
#else
                    return length > 0 ? Encoding.ASCII.GetString(data.Span.Slice(ReadItem.StringHeaderSize, length).ToArray()) : string.Empty;
#endif

                }
            }
            else if (item.ResultType == typeof(short))
            {
                return BinaryPrimitives.ReadInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(ushort))
            {
                return BinaryPrimitives.ReadUInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(int))
            {
                return BinaryPrimitives.ReadInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(uint))
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(long))
            {
                return BinaryPrimitives.ReadInt64BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(ulong))
            {
                return BinaryPrimitives.ReadUInt64BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(float))
            {
                // TODO: Find a Span method to do this
                return BitConverter.ToSingle(Swap4BytesInBuffer(data.Span.ToArray()), 0);
            }
            else if (item.ResultType == typeof(short[]))
            {
                return ConvertMemoryToInt16(item, data);
            }
            else if (item.ResultType == typeof(List<short>))
            {
                return ConvertMemoryToInt16(item, data).ToList();
            }
            else if (item.ResultType == typeof(ushort[]))
            {
                return ConvertMemoryToUInt16(item, data);
            }
            else if (item.ResultType == typeof(List<ushort>))
            {
                return ConvertMemoryToUInt16(item, data).ToList();
            }
            else if (item.ResultType == typeof(int[]))
            {
                return ConvertMemoryToInt32(item, data);
            }
            else if (item.ResultType == typeof(List<int>))
            {
                return ConvertMemoryToInt32(item, data).ToList();
            }
            else if (item.ResultType == typeof(uint[]))
            {
                return ConvertMemoryToUInt32(item, data);
            }
            else if (item.ResultType == typeof(List<uint>))
            {
                return ConvertMemoryToUInt32(item, data).ToList();
            }
            else if (item.ResultType == typeof(long[]))
            {
                return ConvertMemoryToInt64(item, data);
            }
            else if (item.ResultType == typeof(List<long>))
            {
                return ConvertMemoryToInt64(item, data).ToList();
            }
            else if (item.ResultType == typeof(ulong[]))
            {
                return ConvertMemoryToUInt64(item, data);
            }
            else if (item.ResultType == typeof(List<ulong>))
            {
                return ConvertMemoryToUInt64(item, data).ToList();
            }
            else if (item.ResultType == typeof(float[]))
            {
                return ConvertMemoryToSingle(item, data);
            }
            else if (item.ResultType == typeof(List<float>))
            {
                return ConvertMemoryToSingle(item, data).ToList();
            }
            ThrowHelper.ThrowInvalidCastException();
            return null;
        }

        private static float[] ConvertMemoryToSingle(ReadItem item, Memory<byte> data)
        {
            // TODO: Find a Span method to do this
            var result = new float[item.NumberOfItems];
            var buffer = data.Span.ToArray();
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                var offset = i * 4;
                // we need the offset twice because SwapBuffer returns the whole buffer it only swaps the bytes beginning of the given context
                result[i] = BitConverter.ToSingle(Swap4BytesInBuffer(buffer, i * 4), offset);
            }
            return result;
        }

        private static ulong[] ConvertMemoryToUInt64(ReadItem item, Memory<byte> data)
        {
            var result = new ulong[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
            }
            return result;
        }

        private static long[] ConvertMemoryToInt64(ReadItem item, Memory<byte> data)
        {
            var result = new long[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 8).Span);
            }
            return result;
        }

        private static uint[] ConvertMemoryToUInt32(ReadItem item, Memory<byte> data)
        {
            var result = new uint[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 4).Span);
            }
            return result;
        }

        private static int[] ConvertMemoryToInt32(ReadItem item, Memory<byte> data)
        {
            var result = new int[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadInt32BigEndian(data.Slice(i * 4).Span);
            }
            return result;
        }

        private static ushort[] ConvertMemoryToUInt16(ReadItem item, Memory<byte> data)
        {
            var result = new ushort[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i * 2).Span);
            }
            return result;
        }

        private static short[] ConvertMemoryToInt16(ReadItem item, Memory<byte> data)
        {
            var result = new short[item.NumberOfItems];
            for (var i = 0; i < item.NumberOfItems; i++)
            {
                result[i] = BinaryPrimitives.ReadInt16BigEndian(data.Slice(i * 2).Span);
            }
            return result;
        }

    }
}
