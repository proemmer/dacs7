// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

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
                if (item.ResultType == typeof(char[]))
                {
                    data = dataS.ToCharArray();
                }
                else if (item.ResultType == typeof(byte[]))
                {
                    data = item.Encoding == PlcEncoding.Unicode ? Encoding.BigEndianUnicode.GetBytes(dataS) : Encoding.ASCII.GetBytes(dataS);
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
                        if (item.VarType == typeof(string))
                        {
                            if (item.Encoding == PlcEncoding.Unicode)
                            {
                                Memory<byte> result = new byte[(s.Length * 2) + ReadItem.UnicodeStringHeaderSize];

                                BinaryPrimitives.WriteUInt16BigEndian(result.Span, (ushort)((item.NumberOfItems - ReadItem.StringHeaderSize) / 2));
                                BinaryPrimitives.WriteUInt16BigEndian(result.Span.Slice(2), (ushort)s.Length);
                                Encoding.BigEndianUnicode.GetBytes(s).AsSpan().CopyTo(result.Span.Slice(ReadItem.UnicodeStringHeaderSize));
                                return result;
                            }
                            else
                            {
                                Memory<byte> result = new byte[s.Length + ReadItem.StringHeaderSize];

                                result.Span[0] = (byte)(item.NumberOfItems - ReadItem.StringHeaderSize);
                                result.Span[1] = (byte)s.Length;


                                Encoding usedEncoding = Encoding.UTF7;
                                switch(item.Encoding)
                                {
                                    case PlcEncoding.Windows1252:
                                        {
                                            usedEncoding = Encoding.GetEncoding(1252);
                                            break;
                                        }
                                }


                                usedEncoding.GetBytes(s).AsSpan().CopyTo(result.Span.Slice(ReadItem.StringHeaderSize));
                                return result;
                            }
                        }
                        else if (item.VarType == typeof(char))
                        {
                            Memory<byte> result = new byte[2];
                            Encoding.BigEndianUnicode.GetBytes(s).AsSpan().CopyTo(result.Span);
                            return result;
                        }
                        ThrowHelper.ThrowInvalidCastException();
                        return null;
                    }
                case short i16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteInt16BigEndian(result.Span, i16);
                        return result;
                    }
                case ushort ui16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(result.Span, ui16);
                        return result;
                    }
                case int i32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteInt32BigEndian(result.Span, i32);
                        return result;
                    }
                case uint ui32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteUInt32BigEndian(result.Span, ui32);
                        return result;
                    }
                case float s:
                    {
                        // TODO: Find a Span method to do this
                        return WriteSingleBigEndian(s);
                    }
                case long i64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteInt64BigEndian(result.Span, i64);
                        return result;
                    }
                case ulong ui64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteUInt64BigEndian(result.Span, ui64);
                        return result;
                    }

                case List<short> i16:
                    {
                        return ConvertInt16ToMemory(i16);
                    }
                case short[] i16:
                    {
                        return ConvertInt16ToMemory(i16);
                    }
                case List<ushort> ui16:
                    {
                        return ConvertUInt16ToMemory(ui16);
                    }
                case ushort[] ui16:
                    {
                        return ConvertUInt16ToMemory(ui16);
                    }
                case List<int> i32:
                    {
                        return ConvertIn32ToMemory(i32);
                    }
                case int[] i32:
                    {
                        return ConvertIn32ToMemory(i32);
                    }
                case List<uint> ui32:
                    {
                        return ConvertUInt32ToMemory(ui32);
                    }
                case uint[] ui32:
                    {
                        return ConvertUInt32ToMemory(ui32);
                    }
                case List<long> i64:
                    {
                        return ConvertInt64ToMemory(i64);
                    }
                case long[] i64:
                    {
                        return ConvertInt64ToMemory(i64);
                    }
                case List<ulong> ui64:
                    {
                        return ConvertUInt64ToMemory(ui64);
                    }
                case ulong[] ui64:
                    {
                        return ConvertUInt64ToMemory(ui64);
                    }
                case List<float> single:
                    {
                        return ConvertSingleToMemory(single);
                    }
                case float[] single:
                    {
                        return ConvertSingleToMemory(single);
                    }

            }
            ThrowHelper.ThrowInvalidCastException();
            return null;
        }

        private static Memory<byte> ConvertSingleToMemory(IList<float> single)
        {
            // TODO: Find a Span method to do this
            var buffer = new byte[4 * single.Count];
            for (var i = 0; i < single.Count; i++)
            {
                WriteSingleBigEndian(single[i], buffer, i * 4);
            }
            return buffer;
        }

        private static Memory<byte> ConvertUInt64ToMemory(IList<ulong> ui64)
        {
            Memory<byte> result = new byte[8 * ui64.Count];
            for (var i = 0; i < ui64.Count; i++)
            {
                BinaryPrimitives.WriteUInt64BigEndian(result.Span.Slice(i * 8), ui64[i]);
            }

            return result;
        }

        private static Memory<byte> ConvertInt64ToMemory(IList<long> i64)
        {
            Memory<byte> result = new byte[8 * i64.Count];
            for (var i = 0; i < i64.Count; i++)
            {
                BinaryPrimitives.WriteInt64BigEndian(result.Span.Slice(i * 8), i64[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertUInt32ToMemory(IList<uint> ui32)
        {
            Memory<byte> result = new byte[4 * ui32.Count];
            for (var i = 0; i < ui32.Count; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(result.Span.Slice(i * 4), ui32[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertIn32ToMemory(IList<int> i32)
        {
            Memory<byte> result = new byte[4 * i32.Count];
            for (var i = 0; i < i32.Count; i++)
            {
                BinaryPrimitives.WriteInt32BigEndian(result.Span.Slice(i * 4), i32[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertUInt16ToMemory(IList<ushort> ui16)
        {
            Memory<byte> result = new byte[2 * ui16.Count];
            for (var i = 0; i < ui16.Count; i++)
            {
                BinaryPrimitives.WriteUInt16BigEndian(result.Span.Slice(i * 2), ui16[i]);
            }
            return result;
        }

        private static Memory<byte> ConvertInt16ToMemory(IList<short> i16)
        {
            Memory<byte> result = new byte[2 * i16.Count];
            for (var i = 0; i < i16.Count; i++)
            {
                BinaryPrimitives.WriteInt16BigEndian(result.Span.Slice(i * 2), i16[i]);
            }
            return result;
        }

    }
}
