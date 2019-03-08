﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
using System.Buffers;

namespace Dacs7.Domain
{
    internal class TagParser
    {

        public struct TagParserResult
        {
            public PlcArea Area { get; internal set; }
            public ushort DbNumber { get; internal set; }
            public int Offset { get; internal set; }
            public ushort Length { get; internal set; }
            public Type VarType { get; internal set; }
            public Type ResultType { get; internal set; }
            public bool Unicode { get; internal set; }

            public TagParserState ErrorState { get; internal set; }
        }

        public static TagParserResult ParseTag(string tag) => ParseTag(tag, true);


        // DB1.80000,x,1
        public static bool TryParseTag(string tag, out TagParserResult result)
        {
            result = ParseTag(tag, false);
            return result.ErrorState == TagParserState.Success;
        }

        public static bool TryDetectArea(ReadOnlySpan<char> area, out PlcArea selector, out ushort db)
        {
            db = 0;
            var singleElement = area.Length == 1;
            switch (area[0])
            {
                // Inputs
                case 'i' when singleElement: selector = PlcArea.IB; return true;  // English
                case 'e' when singleElement: selector = PlcArea.IB; return true;  // German
                case 'I' when singleElement: selector = PlcArea.IB; return true;  // English
                case 'E' when singleElement: selector = PlcArea.IB; return true;  // German

                // Marker
                case 'm' when singleElement: selector = PlcArea.FB; return true;  // English and German
                case 'M' when singleElement: selector = PlcArea.FB; return true;  // English and German

                // Ouputs
                case 'q' when singleElement: selector = PlcArea.QB; return true;  // English
                case 'a' when singleElement: selector = PlcArea.QB; return true;  // German

                case 'Q' when singleElement: selector = PlcArea.QB; return true;  // English
                case 'A' when singleElement: selector = PlcArea.QB; return true;  // German

                // Timer
                case 't' when singleElement: selector = PlcArea.TM; return true;  // English and German
                case 'T' when singleElement: selector = PlcArea.TM; return true;  // English and German

                // Counter
                case 'c' when singleElement: selector = PlcArea.CT; return true;  // English
                case 'z' when singleElement: selector = PlcArea.CT; return true;  // German

                case 'C' when singleElement: selector = PlcArea.CT; return true;  // English
                case 'Z' when singleElement: selector = PlcArea.CT; return true;  // German

                case 'd' when area.Length > 2:
                case 'D' when area.Length > 2:
                    {
                        // TODO: ReadOnlySpan<char>   !!!! 
                        // Datablocks
                        //if (Regex.IsMatch(area.ToString(), "^db\\d+$", RegexOptions.IgnoreCase))
                        if ((area[0] == 'D' || area[0] == 'd') && (area[1] == 'B' || area[1] == 'b'))
                        {
                            selector = PlcArea.DB;
#if SPANSUPPORT
                            db = ushort.Parse(area.Slice(2));
#else
                            db = SpanToUShort(area.Slice(2));
#endif

                            if (db <= 0) break;

                            return true;
                        }

                    }
                    break;

            }
            selector = PlcArea.DB;
            return false;
        }




        private static TagParserResult ParseTag(string tag, bool throwException)
        {
            var result = new TagParserResult();
            using (var buffer = MemoryPool<char>.Shared.Rent(tag.Length))
            {
                var input = buffer.Memory.Slice(0, tag.Length).Span;
                tag.AsSpan().ToLowerInvariant(input);
                var indexStart = 0;
                var state = TagParserState.Area;
                ReadOnlySpan<char> type = null;

                for (var i = 0; i < input.Length; i++)
                {
                    if (input[i] != '.' && input[i] != ',') continue;
                    Parse(tag, ref result, ref indexStart, ref state, ref type, input.Slice(indexStart, i - indexStart), i, throwException);
                }
                Parse(tag, ref result, ref indexStart, ref state, ref type, input.Slice(indexStart), input.Length - 1, throwException);


                state = TagParserState.TypeValidation;
                Parse(tag, ref result, ref indexStart, ref state, ref type, input, input.Length - 1, throwException);

                if (state == TagParserState.Success)
                {
                    result.ErrorState = TagParserState.Success;
                }
            }

            return result;
        }

        private static void Parse(string tag, ref TagParserResult result, ref int indexStart, ref TagParserState state, ref ReadOnlySpan<char> type, ReadOnlySpan<char> data, int index, bool throwException = false)
        {
            if (!TryExtractData(ref result, data, ref indexStart, ref state, ref type, index) && throwException)
            {
                result.ErrorState = state;
                ExceptionThrowHelper.ThrowTagParseException(TagParserState.Area, data.ToString(), tag);
            }
        }

        private static bool TryExtractData(ref TagParserResult result, ReadOnlySpan<char> input, ref int indexStart, ref TagParserState state, ref ReadOnlySpan<char> type, int i)
        {
            switch (state)
            {
                case TagParserState.Area:
                    {
                        if (TryDetectArea(input, out var selector, out var db))
                        {
                            result.Area = selector;
                            result.DbNumber = db;
                            indexStart = i + 1;
                            state = TagParserState.Offset;
                            return true;
                        }
                    }
                    break;
                case TagParserState.Offset:
                    {
                        // TODO:  !!!!!!!
#if SPANSUPPORT
                        if (Int32.TryParse(input, out var offset))
#else
                        if (TryConvertSpanToInt32(input, out var offset))
#endif

                        {
                            result.Offset = offset;
                            indexStart = i + 1;
                            state = TagParserState.Type;
                            return true;
                        }
                    }
                    break;
                case TagParserState.Type:
                    {
                        type = input;
                        state = TagParserState.NumberOfItems;
                        indexStart = i + 1;
                        return true;
                    }
                case TagParserState.NumberOfItems:
                    {
                        if (input.IsEmpty) return true;
                        // TODO:  !!!!!!!
#if SPANSUPPORT
                        if (UInt16.TryParse(input, out var length))
#else
                        if (TryConvertSpanToUShort(input, out var length))
#endif
                        {
                            result.Length = length;
                            state = TagParserState.TypeValidation;
                            return true;
                        }

                    }
                    break;
                case TagParserState.TypeValidation:
                    {
                        if (result.Length <= 0) result.Length = 1;
                        var offset = result.Offset;
                        var length = result.Length;

                        if (!type.IsEmpty && TryDetectTypes(type, ref length, ref offset, out var vtype, out var rType, out var unicode))
                        {
                            result.Length = length;
                            result.Offset = offset;
                            result.VarType = vtype;
                            result.ResultType = rType;
                            result.Unicode = unicode;
                            indexStart = i + 1;
                            state = TagParserState.Success;
                            return true;
                        }
                    }
                    break;
                case TagParserState.Success:
                    return true;
            }
            return false;
        }

        private static bool TryDetectTypes(ReadOnlySpan<char> type, ref ushort length, ref int offset, out Type vtype, out Type rType, out bool unicode)
        {
            vtype = typeof(object);
            rType = typeof(object);
            unicode = false;

            switch (type[0])
            {
                case 'b':
                    vtype = typeof(byte);
                    rType = length > 1 ? typeof(byte[]) : vtype;
                    return true;
                case 'c':
                    vtype = typeof(char);
                    rType = length > 1 ? typeof(char[]) : vtype;
                    return true;
                case 'w' when type.Length > 1:
                    {
                        switch (type[1])
                        {
                            case 's':
                                vtype = rType = typeof(string);
                                unicode = true;
                                break;
                            case 'c':
                                vtype = typeof(char);
                                rType = typeof(string);
                                unicode = true;
                                break;
                        }
                        break;
                    }
                case 'w':
                    vtype = typeof(ushort);
                    rType = length > 1 ? typeof(ushort[]) : vtype;
                    return true;
                case 'l' when type.Length > 1 && type[1] == 'i':
                    vtype = typeof(sbyte);
                    rType = length > 1 ? typeof(sbyte[]) : vtype;
                    break;
                case 'i':
                    vtype = typeof(short);
                    rType = length > 1 ? typeof(short[]) : vtype;
                    return true;
                case 'd' when type.Length > 1 && type[1] == 'w':
                    vtype = typeof(uint);
                    rType = length > 1 ? typeof(uint[]) : vtype;
                    return true;
                case 'd' when type.Length > 1 && type[1] == 'i':
                    vtype = typeof(int);
                    rType = length > 1 ? typeof(int[]) : vtype;
                    return true;
                case 'r':
                    vtype = typeof(float);
                    rType = length > 1 ? typeof(float[]) : vtype;
                    return true;
                case 's' when type.Length > 1 && type[1] == 'i':
                    vtype = typeof(sbyte);
                    rType = length > 1 ? typeof(sbyte[]) : vtype;
                    break;
                case 's':
                    vtype = rType = typeof(string);
                    return true;
                case 'x' when type.Length > 1:
                    vtype = rType = typeof(bool);
                    rType = length > 1 ? typeof(bool[]) : vtype;
#if SPANSUPPORT
                    offset = ((offset * 8) + Int32.Parse(type.Slice(1)));
#else
                    offset = ((offset * 8) + SpanToInt(type.Slice(1)));
#endif


                    return true;
            }

            return false;
        }

#if !SPANSUPPORT
        #region SpanHelpers
        /// <summary>
        /// Parse a <see cref="ReadOnlySpan{char}"/> which contains only numbers to a ushort
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static ushort SpanToUShort(ReadOnlySpan<char> data) => TryParseSpanToNumber(data, out var result) ? Convert.ToUInt16(result) : (ushort)0;

        private static int SpanToInt(ReadOnlySpan<char> data) => TryParseSpanToNumber(data, out var result) ? result : 0;

        private static bool TryConvertSpanToUShort(ReadOnlySpan<char> data, out ushort result)
        {
            if (TryParseSpanToNumber(data, out var iresult))
            {
                result = Convert.ToUInt16(iresult);
                return true;
            }
            result = default;
            return false;
        }

        private static bool TryConvertSpanToInt32(ReadOnlySpan<char> data, out int result) => TryParseSpanToNumber(data, out result);

        private static bool TryParseSpanToNumber(ReadOnlySpan<char> data, out int result)
        {
            var multip = 1;
            result = 0;
            for (var i = data.Length - 1; i >= 0; i--)
            {
                var ch = data[i];
                if (ch >= '0' && ch <= '9')
                {
                    result += (ch - '0') * multip;
                }
                else
                {
                    return false;
                }

                multip *= 10;
            }
            return true;
        }

        #endregion
#endif
    }
}
