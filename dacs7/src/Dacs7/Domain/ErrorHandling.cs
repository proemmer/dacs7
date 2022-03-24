// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
using System;
using System.Globalization;

namespace Dacs7
{
    public static class ErrorHandling
    {

        public static string ResolveErrorCode<T>(byte b) where T : struct
        {
            return Enum.IsDefined(typeof(T), b) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), b)) : b.ToString(CultureInfo.InvariantCulture);
        }

        public static string ResolveErrorCode<T>(ushort sh) where T : struct
        {
            return Enum.IsDefined(typeof(T), sh) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), sh)) : sh.ToString(CultureInfo.InvariantCulture);
        }

        public static string ResolveErrorCode<T>(string s) where T : struct
        {
            if (Enum.TryParse(s, out T result))
            {
                string r = GetEnumDescription(result);
                if (!r.IsNullOrEmpty())
                {
                    return r;
                }
            }
            return s;
        }

        public static string GetEnumDescription(object e)
        {
            System.Reflection.FieldInfo fieldInfo = e.GetType().GetField(e.ToString());
            if (fieldInfo != null)
            {
                if (fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] enumAttributes && enumAttributes.Length > 0)
                {
                    return enumAttributes[0].Description;
                }
            }
            return e.ToString();
        }

    }

}