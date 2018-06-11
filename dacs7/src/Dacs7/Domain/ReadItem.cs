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
        /// 
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static ReadItem CreateChild(ReadItem item, int offset, ushort length)
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
            throw new InvalidCastException();
        }

        private static ushort DetectTypes(string type, ushort length, ushort offset, out Type vtype, out Type rType)
        {
            vtype = typeof(object);
            rType = typeof(object);
            switch (type.ToLower())
            {
                case "b":
                    vtype = typeof(byte);
                    rType = length > 1 ? typeof(byte[]) : vtype;
                    break;
                case "c":
                    vtype = typeof(char);
                    rType = length > 1 ? typeof(char[]) : vtype;
                    break;
                case "w":
                    vtype = typeof(UInt16);
                    rType = length > 1 ? typeof(UInt16[]) : vtype;
                    break;
                case "dw":
                    vtype = typeof(UInt32);
                    rType = length > 1 ? typeof(UInt32[]) : vtype;
                    break;
                case "i":
                    vtype = typeof(Int16);
                    rType = length > 1 ? typeof(Int16[]) : vtype;
                    break;
                case "di":
                    vtype = typeof(Int32);
                    rType = length > 1 ? typeof(Int32[]) : vtype;
                    break;
                case "r":
                    vtype = typeof(Single);
                    rType = length > 1 ? typeof(Single[]) : vtype;
                    break;
                case "s":
                    vtype = typeof(string);
                    rType = length > 1 ? typeof(string[]) : vtype;
                    break;
                case var s when Regex.IsMatch(s, "^x\\d+$", RegexOptions.IgnoreCase):
                    vtype = typeof(bool);
                    rType = length > 1 ? typeof(bool[]) : vtype;
                    offset = (ushort)((offset * 8) + UInt16.Parse(s.Substring(1)));
                    break;
            }

            return offset;
        }

        private static ReadItem SetupTypes<T>(ReadItem result)
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                result.VarType = t.GetElementType();
                result.ResultType = t;
            }
            else
            {
                result.VarType = t;
                result.ResultType = result.NumberOfItems > 1 ? typeof(T[]) : result.VarType;
            }
            return result;
        }
    }
}
