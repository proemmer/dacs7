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
                return data.ToArray();
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
                var max = data.Span[0];
                var length = data.Span[1];
                var current = data.Span.Length - 2;

                length = (byte)Math.Min(Math.Min((int)max, (int)length), (int)current);
                return length > 0 ? Encoding.ASCII.GetString(data.Span.Slice(2, length).ToArray()) : string.Empty;
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

    }
}
