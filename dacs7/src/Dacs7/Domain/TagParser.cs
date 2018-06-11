using System;
using System.Text.RegularExpressions;

namespace Dacs7.Domain
{
    public class TagParser
    {

        public struct TagParserResult
        {
            public PlcArea Area { get; internal set; }
            public ushort DbNumber { get; internal set; }
            public int Offset { get; internal set; }
            public ushort Length { get; internal set; }
            public Type VarType { get; internal set; }
            public Type ResultType { get; internal set; }
        }

        private enum ParseState
        {
            ParseArea,
            ParseOffset,
            ReadType,
            ParseNumberOfItems,
            ParseType,
            Finished
        }


        // DB1.80000,x,1
        public static bool TryParseTag(string tag, out TagParserResult result)
        {
            var input = tag.ToLower().AsSpan();
            var indexStart = 0;
            var state = ParseState.ParseArea;
            ReadOnlySpan<char> type = null;
            result = new TagParserResult();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != '.' && input[i] != ',') continue;

                TryExtractData(ref result, input.Slice(indexStart, i - indexStart), ref indexStart, ref state, ref type, i);
            }
            TryExtractData(ref result, input.Slice(indexStart), ref indexStart, ref state, ref type, input.Length - 1);


            state = ParseState.ParseType;
            TryExtractData(ref result, input, ref indexStart, ref state, ref type, input.Length - 1);

            return state == ParseState.Finished;
        }

        public static bool TryDetectArea(ReadOnlySpan<char> area, out PlcArea selector, out ushort db)
        {
            db = 0;
            switch (area[0])
            {
                // Inputs
                case 'i': selector = PlcArea.IB; return true;  // English
                case 'e': selector = PlcArea.IB; return true;  // German
                case 'I': selector = PlcArea.IB; return true;  // English
                case 'E': selector = PlcArea.IB; return true;  // German

                // Marker
                case 'm': selector = PlcArea.FB; return true;  // English and German
                case 'M': selector = PlcArea.FB; return true;  // English and German

                // Ouputs
                case 'q': selector = PlcArea.QB; return true;  // English
                case 'a': selector = PlcArea.QB; return true;  // German

                case 'Q': selector = PlcArea.QB; return true;  // English
                case 'A': selector = PlcArea.QB; return true;  // German

                // Timer
                case 't': selector = PlcArea.TM; return true;  // English and German
                case 'T': selector = PlcArea.TM; return true;  // English and German

                // Counter
                case 'c': selector = PlcArea.CT; return true;  // English
                case 'z': selector = PlcArea.CT; return true;  // German

                case 'C': selector = PlcArea.CT; return true;  // English
                case 'Z': selector = PlcArea.CT; return true;  // German

                case 'd' when area.Length > 2:
                case 'D' when area.Length > 2:
                    {
                        // TODO: ReadOnlySpan<char>   !!!! 
                        // Datablocks
                        if (Regex.IsMatch(area.ToString(), "^db\\d+$", RegexOptions.IgnoreCase))
                        {
                            selector = PlcArea.DB;
#if NETCOREAPP21
                            db = ushort.Parse(area.Slice(2));
#else
                            db = ushort.Parse(area.Slice(2).ToString());
#endif
                            return true;
                        }

                    }
                    break;

            }
            selector = PlcArea.DB;
            return true;
        }



        private static bool TryExtractData(ref TagParserResult result, ReadOnlySpan<char> input, ref int indexStart, ref ParseState state, ref ReadOnlySpan<char> type, int i)
        {
            switch (state)
            {
                case ParseState.ParseArea:
                    {
                        if (TryDetectArea(input, out var selector, out var db))
                        {
                            result.Area = selector;
                            result.DbNumber = db;
                            indexStart = i + 1;
                            state = ParseState.ParseOffset;
                            return true;
                        }
                    }
                    break;
                case ParseState.ParseOffset:
                    {
                        // TODO:  !!!!!!!
#if NETCOREAPP21
                        if (Int32.TryParse(input, out var offset))
#else
                        if (Int32.TryParse(input.ToString(), out var offset))
#endif

                        {
                            result.Offset = offset;
                            indexStart = i + 1;
                            state = ParseState.ReadType;
                            return true;
                        }
                    }
                    break;
                case ParseState.ReadType:
                    {
                        type = input;
                        state = ParseState.ParseNumberOfItems;
                        indexStart = i + 1;
                        return true;
                    }
                case ParseState.ParseNumberOfItems:
                    {
                        // TODO:  !!!!!!!
#if NETCOREAPP21
                        if (UInt16.TryParse(input, out var length))
#else
                        if (UInt16.TryParse(input.ToString(), out var length))
#endif
                        {
                            result.Length = length;
                            state = ParseState.ParseType;
                        }
                    }
                    break;
                case ParseState.ParseType:
                    {
                        var offset = result.Offset;

                        if (!type.IsEmpty && TryDetectTypes(type, result.Length, ref offset, out Type vtype, out Type rType))
                        {
                            result.Length = 1;
                            result.Offset = offset;
                            result.VarType = vtype;
                            result.ResultType = rType;
                            indexStart = i + 1;
                            state = ParseState.Finished;
                            return true;
                        }
                    }
                    break;
                case ParseState.Finished:
                    return true;
            }
            return false;
        }

        private static bool TryDetectTypes(ReadOnlySpan<char> type, int length, ref int offset, out Type vtype, out Type rType)
        {
            vtype = typeof(object);
            rType = typeof(object);

            switch (type.ToString())
            {
                case "b":
                    vtype = typeof(byte);
                    rType = length > 1 ? typeof(byte[]) : vtype;
                    return true;
                case "c":
                    vtype = typeof(char);
                    rType = length > 1 ? typeof(char[]) : vtype;
                    return true;
                case "w":
                    vtype = typeof(UInt16);
                    rType = length > 1 ? typeof(UInt16[]) : vtype;
                    return true;
                case "dw":
                    vtype = typeof(UInt32);
                    rType = length > 1 ? typeof(UInt32[]) : vtype;
                    return true;
                case "i":
                    vtype = typeof(Int16);
                    rType = length > 1 ? typeof(Int16[]) : vtype;
                    return true;
                case "di":
                    vtype = typeof(Int32);
                    rType = length > 1 ? typeof(Int32[]) : vtype;
                    return true;
                case "r":
                    vtype = typeof(Single);
                    rType = length > 1 ? typeof(Single[]) : vtype;
                    return true;
                case "s":
                    vtype = typeof(string);
                    rType = length > 1 ? typeof(string[]) : vtype;
                    return true;
                case var s when Regex.IsMatch(s, "^x\\d+$", RegexOptions.IgnoreCase):
                    vtype = typeof(bool);
                    rType = length > 1 ? typeof(bool[]) : vtype;
#if NETCOREAPP21
                    offset = ((offset * 8) + Int32.Parse(type.Slice(1)));
#else
                     offset = ((offset * 8) + Int32.Parse(type.Slice(1).ToString()));
#endif


                    return true;
            }

            return false;
        }



        }
    }
