// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

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

        internal new bool IsPart => Parent != null;

        internal WriteItem()
        {
        }

        internal sealed override WriteItem Clone()
        {
            var clone = base.Clone();
            clone.Data = Data;
            return NormalizeAndValidate(clone);
        }

        internal WriteItem Clone(Memory<byte> data)
        {
            var clone = base.Clone();
            clone.Data = data;
            return NormalizeAndValidate(clone);
        }

        /// <summary>
        /// Create a <see cref="WriteItem"/> from the given tag name.
        /// Tag names are:
        /// [Area].[OffsetInByte],[Datatype],[NumberofItems]
        /// </summary>
        /// <param name="tag">Format:  [Area].[Offset],[Type],[Number Of Items  or in Case of bytes and strings the length of them]</param>
        /// <param name="data">data to write</param>
        /// <returns><see cref="WriteItem"/></returns>
        public static WriteItem CreateFromTag(string tag, object data)
        {
            var result = CreateFromTag(tag).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return NormalizeAndValidate(result);
        }

        /// <summary>
        /// Create a <see cref="WriteItem"/> from the given parameter. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Area in the plc (e.g.  db1, i or e, m, q or a, t, c or z </param>
        /// <param name="offset">offset in byte</param>
        /// <param name="length">number of items</param>
        /// <param name="data">data to write</param>
        /// <param name="encoding">write unicode data (only TIA  WString and WChar</param>
        /// <returns><see cref="WriteItem"/></returns>
        public static WriteItem Create<T>(string area, int offset, ushort length, T data, PlcEncoding encoding = PlcEncoding.Windows1252)
        {
            var result = Create<T>(area, offset, length, encoding).Clone();
            result.Data = result.ConvertDataToMemory(data);
            return NormalizeAndValidate(result);
        }

        /// <summary>
        /// Create a <see cref="WriteItem"/> from the given parameter. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Area in the plc (e.g.  db1, i or e, m, q or a, t, c or z </param>
        /// <param name="offset">offset in byte</param>
        /// <param name="data">data to write</param>
        /// <param name="encoding">write unicode data (only TIA  WString and WChar</param>
        /// <returns><see cref="WriteItem"/></returns>
        public static WriteItem Create<T>(string area, int offset, T data, PlcEncoding encoding = PlcEncoding.Windows1252)
            => Create(area, offset, GetDataItemCount(data), data, encoding);


        /// <summary>
        /// Create a child from a write item. (becuase of max write item size)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static WriteItem CreateChild(WriteItem item, int offset, ushort length)
        {
            var result = ReadItem.CreateChild(item, offset, length).Clone();
            result.Parent = item;
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


        internal static WriteItem NormalizeAndValidate(WriteItem result)
        {
            if (result.VarType == typeof(string))
            {
                var length = (ushort)result.Data.Length;
                if (length > result.NumberOfItems) ThrowHelper.ThrowStringToLongException(nameof(result.Data));
                // special handling of string because we want to write only the given string, not the whole on.
                result.NumberOfItems = (ushort)result.Data.Length;
            }
            else if (result.VarType == typeof(bool))
            {
                if (result.NumberOfItems > 1 || result.Data.Length > 1)
                {
                    // If the number of items is greater then 1, the result is a array
                    // but bit arrays are not supported!
                    // this code area is called, if you create a write item from tag without length specification, but an array as value
                    ThrowHelper.ThrowTypeNotSupportedException(typeof(bool[]));
                }
            }
            else
            {
                var expectedSize = result.NumberOfItems * result.ElementSize;
                if (result.Data.Length != expectedSize)
                {
                    ThrowHelper.ThrowInvalidWriteResult(result);
                }
            }
            return result;
        }


    }
}
