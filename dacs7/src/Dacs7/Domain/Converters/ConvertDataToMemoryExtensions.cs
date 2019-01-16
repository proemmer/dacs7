using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static Dacs7.Domain.ConvertHelpers;

namespace Dacs7.Domain
{
    internal static class ConvertDataToMemoryExtensions
    {

        public static Memory<byte> ConvertDataToMemory(this ReadItem item, object data)
        {
            if (data is string dataS && item.ResultType != typeof(string))
            {
                if(item.ResultType == typeof(char[]))
                {
                    data = dataS.ToCharArray();
                }
                else if (item.ResultType == typeof(byte[]))
                {
                    data = Encoding.ASCII.GetBytes(dataS);
                }
                else
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
                        Memory<byte> result = new byte[s.Length + ReadItem.StringHeaderSize];

                        if (ReadItem.StringHeaderSize == 2)
                        {
                            result.Span[0] = (byte)(item.NumberOfItems - ReadItem.StringHeaderSize);
                            result.Span[1] = (byte)s.Length;
                        }
                        else if(ReadItem.StringHeaderSize == 1)
                        {
                            result.Span[0] = (byte)s.Length;
                        }
                        Encoding.ASCII.GetBytes(s).AsSpan().CopyTo(result.Span.Slice(ReadItem.StringHeaderSize));
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

    }
}
