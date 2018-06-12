// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dacs7
{
    public class WriteItem : ReadItem
    {
        internal Memory<byte> Data { get; set; }

        internal new WriteItem Parent { get; set; }

        internal WriteItem()
        {
        }

        internal override WriteItem Clone()
        {
            var clone = base.Clone();
            clone.Data = Data;
            return clone;
        }

        internal WriteItem Clone(Memory<byte> data)
        {
            var clone = base.Clone();
            clone.Data = data;
            return clone;
        }



        public static WriteItem CreateFromTag(string tag, object data)
        {
            var result = CreateFromTag(tag).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return result;
        }


        public static WriteItem Create<T>(string area, int offset, T data)
        {
            var result = Create<T>(area, offset, GetDataItemCount(data)).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static WriteItem CreateChild(WriteItem item, int offset, ushort length)
        {
            var result = ReadItem.CreateChild(item, offset, length).Clone();
            result.Data = item.Data.Slice(offset - item.Offset, length);
            return result;
        }

        internal static ushort GetDataItemCount<T>(T data)
        {
            if (typeof(T).IsArray)
            {
                return (ushort)(data as Array).Length;
            }
            else if (data is Memory<byte> ba)
            {
                return (ushort)ba.Length;
            }
            else if (data is string s)
            {
                return (ushort)(s.Length + 2);
            }
            else if (data is IEnumerable<object> en)
            {
                return (ushort)en.Count();
            }

            return 1;
        }





    }
}
