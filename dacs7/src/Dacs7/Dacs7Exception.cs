using Dacs7.Domain;
using System;
using Dacs7.Helper;
using System.Globalization;
using System.Reflection;

namespace Dacs7
{
    public class Dacs7Exception : Exception
    {
        public ErrorClass ErrorClass { get; private set; }
        public byte ErrorCode { get; private set; }


        public Dacs7Exception(byte eClass, byte code) : 
            base("No success error class and code: class: <{ResolveErrorCode<ErrorClass>(eClass)}>, code: <{code}>")
        {
            ErrorClass = (ErrorClass)eClass;
            ErrorCode = code;
        }

        #region Helpers
        internal static string ResolveErrorCode<T>(byte b) where T : struct
        {
            return Enum.IsDefined(typeof(T), b) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), b)) : b.ToString(CultureInfo.InvariantCulture);
        }

        internal static string ResolveErrorCode<T>(ushort sh) where T : struct
        {
            return Enum.IsDefined(typeof(T), sh) ? ResolveErrorCode<T>(Enum.GetName(typeof(T), sh)) : sh.ToString(CultureInfo.InvariantCulture);
        }

        internal static string ResolveErrorCode<T>(string s) where T : struct
        {
            T result;
            if (Enum.TryParse(s, out result))
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
                var enumAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                if (enumAttributes != null && enumAttributes.Length > 0)
                    return enumAttributes[0].Description;
            }
            return e.ToString();
        }
        #endregion
    }

    public class Dacs7ContentException : Exception
    {
        public int ErrorIndex { get; private set; }
        public ItemResponseRetVaulue ErrorCode { get; private set; }

        public Dacs7ContentException(byte errorCode, int itemIndex) : 
            base($"No success return code form item {itemIndex}: <{Dacs7Exception.ResolveErrorCode<ItemResponseRetVaulue>(errorCode)}>")
        {
            ErrorCode = (ItemResponseRetVaulue) errorCode;
            ErrorIndex = itemIndex;
        }
    }

    public class Dacs7ParameterException : Exception
    {
        public ErrorParameter ErrorCode { get; private set; }

        public Dacs7ParameterException(ushort errorCode) :
            base($"No success error code: <{Dacs7Exception.ResolveErrorCode<ErrorParameter>(errorCode)}>")
        {
            ErrorCode = (ErrorParameter)errorCode;
        }
    }

    public class Dacs7ReturnCodeException : Exception
    {
        public byte ReturnCode { get; private set; }
        public int ItemNumber { get; set; }

        public Dacs7ReturnCodeException(byte returnCode, int itemNumber = -1) :
            base(string.Format($"No success return code{returnCode}: <{(itemNumber != -1 ? string.Format(" for item { 0}",itemNumber) : "")}>" ))
        {
            ReturnCode = returnCode;
        }
    }


    public class Dacs7ToMuchDataPerCallException : Exception
    {
        public Dacs7ToMuchDataPerCallException(int expected, int actual) :
            base(string.Format("There are to much data ({0} bytes) for a single job, please split jobs to a maximum of {1} per call!", actual, expected))
        {
        }
    }

    public class Dacs7NotConnectedException : Exception
    {

        public Dacs7NotConnectedException() :
            base("Dacs7 has no connection to the plc!")
        {
        }
    }
}
