// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7
{
    public class ReadItem
    {

        public const ushort StringHeaderSize = 2;
        public const ushort UnicodeStringHeaderSize = 4;


        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public int Offset { get; private set; }
        public ushort NumberOfItems { get; internal set; }
        public Type VarType { get; private set; }
        public Type ResultType { get; private set; }
        public PlcEncoding Encoding { get; private set; }

        internal int CallbackReference { get; set; }
        internal ReadItem Parent { get; set; }
        internal bool IsPart => Parent != null;
        internal ushort ElementSize { get; set; }

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
                ResultType = ResultType,
                ElementSize = ElementSize,
                Encoding = Encoding
            };
        }

        /// <summary>
        /// Create a readitem from a given Tag
        /// </summary>
        /// <param name="tag">Format:  [Area].[Offset],[Type],[Number Of Items  or in Case of bytes and strings the length of them]</param>
        /// <returns></returns>
        public static ReadItem CreateFromTag(string tag)
        {
            var tr = TagParser.ParseTag(tag);
            var readItem = BuildReadItemFromTagResult(ref tr);
            EnsureSupportedType(readItem);
            return readItem;
        }

        private static ReadItem BuildReadItemFromTagResult(ref TagParser.TagParserResult result)
        {
            var numberOfItems = result.VarType == typeof(string) ? (ushort)(result.Length + (result.Encoding == PlcEncoding.Unicode ? UnicodeStringHeaderSize : StringHeaderSize)) : result.Length;
            return new ReadItem
            {
                Area = result.Area,
                DbNumber = result.DbNumber,
                Offset = result.Offset,
                NumberOfItems = numberOfItems,
                VarType = result.VarType,
                ResultType = result.ResultType,
                ElementSize = GetElementSize(result.Area, result.VarType, result.Encoding),
                Encoding = result.Encoding
            };
        }


        public static ReadItem Create<T>(string area, int offset, PlcEncoding encoding = PlcEncoding.Ascii)
            => Create<T>(area, offset, 1, encoding);

        /// <summary>
        /// Create a read item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <param name="unicode">IF the given type is a string or char you can also specifiy if its the unicode variant of them (this means 2byte per sign)</param>
        /// <returns></returns>
        public static ReadItem Create<T>(string area, int offset, ushort length, PlcEncoding encoding = PlcEncoding.Ascii)
        {
            if (!TagParser.TryDetectArea(area.AsSpan(), out var selector, out var db))
            {
                ThrowHelper.ThrowInvalidAreaException(area);
            }

            return SetupTypes<T>(new ReadItem
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                NumberOfItems = length,
                Encoding = encoding
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
                Parent = item,
                ElementSize = item.ElementSize,
                Encoding = item.Encoding
            };
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
            else if (t == typeof(string))
            {
                result.VarType = result.ResultType = t;
                result.NumberOfItems += result.Encoding == PlcEncoding.Unicode ? UnicodeStringHeaderSize : StringHeaderSize;
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
            result.ElementSize = GetElementSize(result.Area, result.VarType, result.Encoding);
            return result;
        }



        public static ushort GetElementSize(PlcArea area, Type t, PlcEncoding encoding)
        {
            if (area == PlcArea.CT || area == PlcArea.TM) return 2;

            if (t.IsArray) t = t.GetElementType();

            if (t == typeof(byte) || t == typeof(Memory<byte>)) return 1;
            if (t == typeof(bool)) return 1;
            if (t == typeof(char) || t == typeof(string)) return (ushort)(encoding == PlcEncoding.Unicode ? 2 : 1);
            if (t == typeof(short) || t == typeof(ushort)) return 2;
            if (t == typeof(int) || t == typeof(uint)) return 4;
            if (t == typeof(ulong) || t == typeof(long)) return 8;
            if (t == typeof(float)) return 4;
            if (t == typeof(sbyte)) return 1;

            return 1;
        }

        public static void EnsureSupportedType(ReadItem item)
        {
            if (item.ResultType == typeof(byte) || item.ResultType == typeof(byte[]) || item.ResultType == typeof(List<byte>) || item.ResultType == typeof(Memory<byte>) ||
                item.ResultType == typeof(bool) ||
                item.ResultType == typeof(string) ||
                item.ResultType == typeof(char) || item.ResultType == typeof(char[]) || item.ResultType == typeof(List<char>) ||
                item.ResultType == typeof(short) || item.ResultType == typeof(short[]) || item.ResultType == typeof(List<short>) ||
                item.ResultType == typeof(ushort) || item.ResultType == typeof(ushort[]) || item.ResultType == typeof(List<ushort>) ||
                item.ResultType == typeof(int) || item.ResultType == typeof(int[]) || item.ResultType == typeof(List<int>) ||
                item.ResultType == typeof(uint) || item.ResultType == typeof(uint[]) || item.ResultType == typeof(List<uint>) ||
                item.ResultType == typeof(ulong) || item.ResultType == typeof(ulong[]) || item.ResultType == typeof(List<ulong>) ||
                item.ResultType == typeof(long) || item.ResultType == typeof(long[]) || item.ResultType == typeof(List<long>) ||
                item.ResultType == typeof(float) || item.ResultType == typeof(float[]) || item.ResultType == typeof(List<float>) ||
                item.ResultType == typeof(sbyte))
            {
                return;
            }

            ThrowHelper.ThrowTypeNotSupportedException(item.ResultType);
        }

    }
}
