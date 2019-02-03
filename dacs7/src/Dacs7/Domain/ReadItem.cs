// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7
{
    public class ReadItem
    {

        public const int StringHeaderSize = 2;


        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public int Offset { get; private set; }
        public ushort NumberOfItems { get; internal set; }
        public Type VarType { get; private set; }
        public Type ResultType { get; private set; }

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
                ElementSize = ElementSize
            };
        }

        /// <summary>
        /// Create a readitem from a given Tag
        /// </summary>
        /// <param name="tag">Format:  [Area].[Offset],[Type],[Number Of Items  or in Case of bytes and strings the length of them]</param>
        /// <returns></returns>
        public static ReadItem CreateFromTag(string tag)
        {
            var result = TagParser.ParseTag(tag);
            var readItem = new ReadItem
            {
                Area = result.Area,
                DbNumber = result.DbNumber,
                Offset = result.VarType == typeof(string) && ReadItem.StringHeaderSize == 1 ? result.Offset + 1 : result.Offset,
                NumberOfItems = result.VarType == typeof(string) ? (ushort)(result.Length + ReadItem.StringHeaderSize) :  result.Length,
                VarType = result.VarType,
                ResultType = result.ResultType,
                ElementSize = GetElementSize(result.Area, result.VarType)
            };
            ConvertHelpers.EnsureSupportedType(readItem);
            return readItem;
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
                ExceptionThrowHelper.ThrowInvalidAreaException(area);
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
                Parent = item,
                ElementSize = item.ElementSize
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
            else if(t == typeof(string) )
            {
                result.VarType = result.ResultType  = t;
                if (ReadItem.StringHeaderSize == 1)
                {
                    result.Offset++;
                }
                result.NumberOfItems += ReadItem.StringHeaderSize;
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


            ConvertHelpers.EnsureSupportedType(result);
            result.ElementSize = GetElementSize(result.Area, result.VarType);
            return result;
        }



        public static ushort GetElementSize(PlcArea area, Type t)
        {
            if (area == PlcArea.CT || area == PlcArea.TM)
            {
                return 2;
            }

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
            {
                return 1;
            }

            if (t == typeof(byte) || t == typeof(string) || t == typeof(Memory<byte>))
            {
                return 1;
            }

            if (t == typeof(char))
            {
                return 1;
            }

            if (t == typeof(short))
            {
                return 2;
            }

            if (t == typeof(int))
            {
                return 4;
            }

            if (t == typeof(ushort))
            {
                return 2;
            }

            if (t == typeof(uint))
            {
                return 4;
            }

            if (t == typeof(Single))
            {
                return 4;
            }

            return 1;
        }

    }
}
