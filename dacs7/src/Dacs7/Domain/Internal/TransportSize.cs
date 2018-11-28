using System;

namespace Dacs7.Domain
{
    internal enum ItemDataTransportSize
    {
        /* types of 1 byte length */
        Bit = 1,
        Byte = 2,
        Char = 3,
        /* types of 2 bytes length */
        Word = 4,
        Int = 5,
        /* types of 4 bytes length */
        Dword = 6,
        Dint = 7,
        Real = 8,
        /* Special types */
        Date = 9,
        Tod = 10,
        Time = 11,
        S5Time = 12,
        Dt = 15,
        /* Timer or counter */
        Counter = 28,
        Timer = 29,
        IecCounter = 30,
        IecTimer = 31,
        HsCounter = 32
    }

    public enum DataTransportSize
    {
        Null = 0,
        Bit = 3, /* bit access, length is in bits */
        Byte = 4, /* byte/word/dword access, length is in bits */
        Int = 5, /* integer access, length is in bits */
        Dint = 6, /* integer access, length is in bytes */
        Real = 7, /* real access, length is in bytes */
        OctetString = 9 /* octet string, length is in bytes */
    }

    internal static class TransportSizeHelper
    {
        public static byte DataTypeToTransportSize(Type t)
        {
            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
                return (byte)ItemDataTransportSize.Bit;

            if (t == typeof(byte))
                return (byte)ItemDataTransportSize.Byte;

            if (t == typeof(char))
                return (byte)ItemDataTransportSize.Char;

            if (t == typeof(ushort))
                return (byte)ItemDataTransportSize.Word;

            if (t == typeof(short))
                return (byte)ItemDataTransportSize.Int;

            if (t == typeof(uint))
                return (byte)ItemDataTransportSize.Dword;

            if (t == typeof(int))
                return (byte)ItemDataTransportSize.Dint;

            if (t == typeof(Single))
                return (byte)ItemDataTransportSize.Real;

            return 0;
        }

        public static int DataTypeToSizeByte(Type t, PlcArea area)
        {
            if (t.IsArray)
                t = t.GetElementType();

            if (area == PlcArea.CT || area == PlcArea.TM)
                return 1;

            if (t == typeof(bool) || t == typeof(byte) || t == typeof(char))
                return 1;

            if (t == typeof(Int16) || t == typeof(UInt16))
                return 2;

            if (t == typeof(Int32) || t == typeof(UInt32) || t == typeof(double))
                return 4;


            return 0;
        }


        public static byte DataTypeToResultTransportSize(Type t)
        {

            if (t.IsArray)
                t = t.GetElementType();

            if (t == typeof(bool))
                return (byte)DataTransportSize.Bit;

            if (t == typeof(byte))
                return (byte)DataTransportSize.Byte;

            if (t == typeof(ushort))
                return (byte)DataTransportSize.Int;

            return 0;
        }

    }

}
