// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System.Collections;
using System.Text;

namespace Dacs7
{

    public delegate string DataValueFormatter(object value, string formattedValue);


    public static class DataValueFormatterExtensions
    {

        public static string GetValueAsString(this DataValue dataValue, string separator = " ") => GetValueAsString(dataValue, null, separator);

        public static string GetValueAsString(this DataValue dataValue, DataValueFormatter formatter, string separator = " ")
        {
            var result = FormattedResult(dataValue, separator);
            return formatter != null ? formatter(dataValue.Value, result) : result;
        }

        private static string FormattedResult(DataValue dataValue, string seperator)
        {
            if (dataValue.Type.IsArray)
            {
                if (seperator == null) seperator = string.Empty;
                var result = new StringBuilder();
                var enumerable = dataValue.Value as IEnumerable;
                foreach (var item in enumerable)
                {
                    result.Append(item.ToString());
                    result.Append(seperator);
                }
                return result.Length > 0 ? result.ToString(0, result.Length - seperator.Length) : string.Empty;
            }
            return dataValue.Value.ToString();
        }

    }
}
