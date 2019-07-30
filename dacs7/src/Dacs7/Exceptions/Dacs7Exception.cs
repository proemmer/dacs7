// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Helper;
using System;
using System.Globalization;

namespace Dacs7
{
    public class Dacs7Exception : Exception
    {
        public ErrorClass ErrorClass { get; private set; }
        public byte ErrorCode { get; private set; }

        public Dacs7Exception()
        {
        }

        public Dacs7Exception(string message) : base(message)
        {
        }

        public Dacs7Exception(string message, Exception innerException) : base(message, innerException)
        {
        }

        public Dacs7Exception(byte eClass, byte code) :
            base($"No success error class and code: class: <{ResolveErrorCode<ErrorClass>(eClass)}>, code: <{code}>")
        {
            ErrorClass = (ErrorClass)eClass;
            ErrorCode = code;
        }

        #region Helpers
        internal static string ResolveErrorCode<T>(byte b) where T : struct => Enum.IsDefined(typeof(T), b) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), b)) : b.ToString(CultureInfo.InvariantCulture);

        internal static string ResolveErrorCode<T>(ushort sh) where T : struct => Enum.IsDefined(typeof(T), sh) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), sh)) : sh.ToString(CultureInfo.InvariantCulture);

        internal static string ResolveErrorCode<T>(string s) where T : struct
        {
            if (Enum.TryParse(s, out T result))
            {
                var r = GetEnumDescription(result);
                if (!string.IsNullOrWhiteSpace(r))
                    return r;
            }
            return s;
        }

        private static string GetEnumDescription(object e)
        {

            var fieldInfo = e.GetType().GetField(e.ToString());
            if (fieldInfo != null)
            {
                if (fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] enumAttributes && enumAttributes.Length > 0)
                    return enumAttributes[0].Description;
            }
            return e.ToString();
        }

        #endregion
    }
}
