using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Insite.OpcUa
{
    public static class NodeValueConverter
    {
        public static object ConvertFromNodeId(string nodeId, object value)
        {
            var parts = nodeId.Split(',');
            var array = parts.Length == 3;
            var type = parts[1];

            switch (type.ToLower())
            {
                case "b": return array ? Convert.ChangeType(value, typeof(byte[])) : Convert.ChangeType(value, typeof(byte));
                case "c": return array ? Convert.ChangeType(value, typeof(char[])) : Convert.ChangeType(value, typeof(char));
                case "w": return array ? Convert.ChangeType(value, typeof(UInt16[])) : Convert.ChangeType(value, typeof(UInt16));
                case "dw": return array ? Convert.ChangeType(value, typeof(UInt32[])) : Convert.ChangeType(value, typeof(UInt32));
                case "i": return array ? Convert.ChangeType(value, typeof(Int16[])) : Convert.ChangeType(value, typeof(Int16));
                case "di": return array ? Convert.ChangeType(value, typeof(Int32[])) : Convert.ChangeType(value, typeof(Int32));
                case "r": return array ? Convert.ChangeType(value, typeof(Single[])) : Convert.ChangeType(value, typeof(Single));
                case "s": return array ? Convert.ChangeType(value, typeof(String[])) : Convert.ChangeType(value, typeof(String));
                case var s when Regex.IsMatch(s, "^x\\d+$"): return array ? Convert.ChangeType(value, typeof(bool[])) :  Convert.ChangeType(value, typeof(bool));
            }
            return value;
        }


    }
}
