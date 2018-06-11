using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Globalization;

namespace Dacs7.Domain
{
//    public class TagParser
//    {

//        public class TagParserResult
//        {
//            public PlcArea Area { get; private set; }
//            public ushort DbNumber { get; private set; }
//            public int Offset { get; private set; }
//            public ushort Length { get; private set; }
//            public Type VarType { get; private set; }
//            public Type ResultType { get; private set; }
//        }

//        private enum ParseState
//        {
//            NotStarted,
//            ParsingArea,
//        }


//        // DB1.80000,x,1
//        public static bool TryParseTag(string tag, out TagParserResult result)
//        {
//            var input = tag.ToLower().AsSpan();
//            foreach (var c in tag)
//            {



//            }



//            //    var parts = tag.Split(new[] { ',' });
//            //    var start = parts[0].Split(new[] { '.' });
//            //    var withPrefix = start.Length == 3;
//            //    PlcArea selector = 0;
//            //    ushort length = 1;
//            //    var offset = Int32.Parse(start[start.Length - 1]);
//            //    ushort db = 0;

//            //    if (!TryDetectArea(start[withPrefix ? 1 : 0], ref selector, ref db))
//            //    {
//            //        throw new ArgumentException($"Invalid area in tag <{tag}>");
//            //    }

//            //    if (parts.Length > 2)
//            //    {
//            //        length = UInt16.Parse(parts[2]);
//            //    }

//            //    offset = DetectTypes(parts[1], length, offset, out Type vtype, out Type rType);

//            //}

//            //private static int DetectTypes(ReadOnlySpan<char> type, int length, int offset, out Type vtype, out Type rType)
//            //{
//            //    vtype = typeof(object);
//            //    rType = typeof(object);
//            //    switch (type)
//            //    {
//            //        case "b":
//            //            vtype = typeof(byte);
//            //            rType = length > 1 ? typeof(byte[]) : vtype;
//            //            break;
//            //        case "c":
//            //            vtype = typeof(char);
//            //            rType = length > 1 ? typeof(char[]) : vtype;
//            //            break;
//            //        case "w":
//            //            vtype = typeof(UInt16);
//            //            rType = length > 1 ? typeof(UInt16[]) : vtype;
//            //            break;
//            //        case "dw":
//            //            vtype = typeof(UInt32);
//            //            rType = length > 1 ? typeof(UInt32[]) : vtype;
//            //            break;
//            //        case "i":
//            //            vtype = typeof(Int16);
//            //            rType = length > 1 ? typeof(Int16[]) : vtype;
//            //            break;
//            //        case "di":
//            //            vtype = typeof(Int32);
//            //            rType = length > 1 ? typeof(Int32[]) : vtype;
//            //            break;
//            //        case "r":
//            //            vtype = typeof(Single);
//            //            rType = length > 1 ? typeof(Single[]) : vtype;
//            //            break;
//            //        case "s":
//            //            vtype = typeof(string);
//            //            rType = length > 1 ? typeof(string[]) : vtype;
//            //            break;
//            //        case var s when Regex.IsMatch(s, "^x\\d+$", RegexOptions.IgnoreCase):
//            //            vtype = typeof(bool);
//            //            rType = length > 1 ? typeof(bool[]) : vtype;
//            //            offset = ((offset * 8) + Int32.Parse(s.Substring(1)));
//            //            break;
//            //    }

//            //    return offset;
//            //}

//            private static bool TryDetectArea(ReadOnlySpan<char> area, ref PlcArea selector, ref ushort db)
//            {
 
//                switch (area[0])
//                {
//                    // Inputs
//                    case 'i': selector = PlcArea.IB; break;  // English
//                    case 'e': selector = PlcArea.IB; break;  // German

//                    // Marker
//                    case 'm': selector = PlcArea.FB; break;  // English and German

//                    // Ouputs
//                    case 'q': selector = PlcArea.QB; break;  // English
//                    case 'a': selector = PlcArea.QB; break;  // German

//                    // Timer
//                    case 't': selector = PlcArea.TM; break;  // English and German

//                    // Counter
//                    case 'c': selector = PlcArea.CT; break;  // English
//                    case 'z': selector = PlcArea.CT; break;  // German

//                    case 'd': 
//                        {
//                            // Datablocks
//                            //if(Regex.IsMatch(s, "^db\\d+$", RegexOptions.IgnoreCase))
//                            //{

//                            //}

//                            selector = PlcArea.DB;
//                            db = int.Parse(area.Slice(2));
//                        }
//                        break;
//                    default: return false;
//                }

//                return true;
//            }


//    }
}
