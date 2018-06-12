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


            ConvertHelpers.EnsureSupportedType(result);

            return result;
        }
    }
}
