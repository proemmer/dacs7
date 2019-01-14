// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

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
            return NormalizeAndValidate(result);
        }

        public static WriteItem Create<T>(string area, int offset, ushort length, T data)
        {
            var result = Create<T>(area, offset, length).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return NormalizeAndValidate(result);
        }

        public static WriteItem Create<T>(string area, int offset, T data)
        {
            var result = Create<T>(area, offset, GetDataItemCount(data)).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return NormalizeAndValidate(result);
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
                return (ushort)(s.Length);
            }
            else if (data is IEnumerable<object> en)
            {
                return (ushort)en.Count();
            }

            return 1;
        }


        private static WriteItem NormalizeAndValidate(WriteItem result)
        {
            if (result.VarType == typeof(string))
            {
                var length = (ushort)result.Data.Length;
                if (length > result.NumberOfItems)
                    throw new ArgumentOutOfRangeException(nameof(result.Data), $"The given string is to long!");
                // special handling of string because we want to write only the given string, not the whole on.
                result.NumberOfItems = (ushort)result.Data.Length;
            }
            else if(result.VarType == typeof(bool))
            {
                if (result.NumberOfItems > 1 || result.Data.Length > 1)
                {
                    // If the number of items is greater then 1, the result is a array
                    // but bit arrays are not supported!
                    // this code area is called, if you create a write item from tag without length specification, but an array as value
                    throw new Dacs7TypeNotSupportedException(typeof(bool[]));
                }
            }
            else
            {
                var expectedSize = result.NumberOfItems * result.ElementSize;
                if (result.Data.Length != expectedSize)
                {
                    throw new ArgumentOutOfRangeException($"Given number of elements {result.NumberOfItems} and given number of values {result.Data.Length / result.ElementSize} are not matching!");
                }
            }
            return result;
        }


    }
}
