// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dacs7
{
    public class ReadItemSpecification
    {
        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public ushort Offset { get; private set; }
        public ushort Length { get; private set; }
        public Type VarType { get; private set; }
        public Type ResultType { get; private set; }



        internal int CallbackReference { get; set; }


        internal ReadItemSpecification()
        {

        }


        internal virtual WriteItemSpecification Clone()
        {
            return new WriteItemSpecification
            {
                Area = Area,
                DbNumber = DbNumber,
                Offset = Offset,
                Length = Length,
                VarType = VarType,
                ResultType = ResultType
            };
        }


        public static ReadItemSpecification CreateFromTag(string tag)
        {
            var parts = tag.Split(new[] { ',' });
            var start = parts[0].Split(new[] { '.' });
            var withPrefix = start.Length == 3;
            PlcArea selector = 0;
            ushort length = 1;
            ushort offset = UInt16.Parse(start[start.Length - 1]);
            ushort db = 0;

            if (!TryDetectArea(start[withPrefix ? 1 : 0], ref selector, ref db))
            {
                throw new ArgumentException($"Invalid area in tag <{tag}>");
            }

            if (parts.Length > 2)
            {
                length = UInt16.Parse(parts[2]);
            }

            offset = DetectTypes(parts[1], length, offset, out Type vtype, out Type rType);

            return new ReadItemSpecification
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                Length = length,
                VarType = vtype,
                ResultType = rType
            };
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="area">Where to read:  e.g.  DB1  or M or...</param>
        /// <param name="offset">offset in bytes, if you address booleans, you have to pass the address in bits (byteoffset * 8 + bitoffset)</param>
        /// <param name="length">The number of items to read</param>
        /// <returns></returns>
        public static ReadItemSpecification Create<T>(string area, ushort offset, ushort length = 1)
        {
            PlcArea selector = 0;
            ushort db = 0;
            if (!TryDetectArea(area, ref selector, ref db))
            {
                throw new ArgumentException($"Invalid area <{area}>");
            }

            var t = typeof(T);
            Type vtype;
            Type rType;
            if (t.IsArray)
            {
                vtype = t.GetElementType();
                rType = t;
            }
            else
            {
                vtype = t;
                rType = length > 1 ? typeof(T[]) : vtype;
            }

            return new ReadItemSpecification
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                Length = length,
                VarType = vtype,
                ResultType = rType
            };
        }





        internal static object ConvertMemoryToData(ReadItemSpecification item, Memory<byte> data)
        {

            if (item.ResultType == typeof(byte))
            {
                return data.Span[0];
            }
            else if (item.ResultType == typeof(byte[]) || item.ResultType == typeof(Memory<byte>))
            {
                return data;
            }
            else if (item.ResultType == typeof(char))
            {
                return Convert.ToChar(data.Span[0]);
            }
            else if (item.ResultType == typeof(char[]) || item.ResultType == typeof(Memory<char>))
            {
                return null; // TODO
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
                case var s when Regex.IsMatch(s, "^x\\d+$"):
                    vtype = typeof(bool);
                    rType = length > 1 ? typeof(bool[]) : vtype;
                    offset = (ushort)((offset * 8) + UInt16.Parse(s.Substring(1)));
                    break;
            }

            return offset;
        }

        private static bool TryDetectArea(string area, ref PlcArea selector, ref ushort db)
        {
            switch (area.ToUpper())
            {
                // Inputs
                case "I": selector = PlcArea.IB; break;  // English
                case "E": selector = PlcArea.IB; break;  // German

                // Marker
                case "M": selector = PlcArea.FB; break;  // English and German

                // Ouputs
                case "Q": selector = PlcArea.QB; break;  // English
                case "A": selector = PlcArea.QB; break;  // German

                // Timer
                case "T": selector = PlcArea.TM; break;  // English and German

                // Counter
                case "C": selector = PlcArea.CT; break;  // English
                case "Z": selector = PlcArea.CT; break;  // German

                // Datablocks
                case var s when Regex.IsMatch(s, "^DB\\d+$", RegexOptions.IgnoreCase):
                    {
                        selector = PlcArea.DB;
                        db = UInt16.Parse(s.Substring(2));
                        break;
                    }

                default: return false;
            }

            return true;
        }

 
    }
}
