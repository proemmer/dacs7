using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Dacs7.Benchmarks
{
    [RankColumn]
    public class TypeOfBenchmarks
    {
        public static Type Byte = typeof(byte);
        public static Type ByteArray = typeof(byte[]);
        public static Type ByteList = typeof(List<byte>);
        public static Type ByteMemory = typeof(Memory<byte>);

        public static Type SByte = typeof(sbyte);
        public static Type SByteArray = typeof(sbyte[]);
        public static Type SByteList = typeof(List<sbyte>);

        public static Type String = typeof(string);

        public static Type Char = typeof(char);
        public static Type CharArray = typeof(char[]);
        public static Type CharList = typeof(List<char>);

        public static Type UInt16 = typeof(UInt16);
        public static Type UInt16Array = typeof(UInt16[]);
        public static Type UInt16List = typeof(List<UInt16>);

        public static Type Int16 = typeof(Int16);
        public static Type Int16Array = typeof(Int16[]);
        public static Type Int16List = typeof(List<Int16>);

        public static Type UInt32 = typeof(UInt32);
        public static Type UInt32Array = typeof(UInt32[]);
        public static Type UInt32List = typeof(List<UInt32>);

        public static Type Int32 = typeof(Int32);
        public static Type Int32Array = typeof(Int32[]);
        public static Type Int32List = typeof(List<Int32>);

        public static Type UInt64 = typeof(UInt64);
        public static Type UInt64Array = typeof(UInt64[]);
        public static Type UInt64List = typeof(List<UInt64>);

        public static Type Int64 = typeof(Int64);
        public static Type Int64Array = typeof(Int64[]);
        public static Type Int64List = typeof(List<Int64>);

        public static Type Single = typeof(Single);
        public static Type SingleArray = typeof(Single[]);
        public static Type SingleList = typeof(List<Single>);

        public static Type Bool = typeof(bool);

        [Params(typeof(List<Single>), typeof(byte), typeof(char), typeof(bool), typeof(TypeOfBenchmarks))]
        public Type Type;

        [Benchmark]
        public bool EnsureSupportedTypeNonCached()
        {
            if (Type == typeof(byte) || Type == typeof(byte[]) || Type == typeof(List<byte>) ||
                Type == typeof(Memory<byte>) ||
                Type == typeof(string) || Type == typeof(bool) ||
                Type == typeof(char) || Type == typeof(char[]) || Type == typeof(List<char>) ||
                Type == typeof(UInt16) || Type == typeof(UInt16[]) || Type == typeof(List<UInt16>) ||
                Type == typeof(UInt32) || Type == typeof(UInt32[]) || Type == typeof(List<UInt32>) ||
                Type == typeof(UInt64) || Type == typeof(UInt64[]) || Type == typeof(List<UInt64>) ||
                Type == typeof(Int16) || Type == typeof(Int16[]) || Type == typeof(List<Int16>) ||
                Type == typeof(Int32) || Type == typeof(Int32[]) || Type == typeof(List<Int32>) ||
                Type == typeof(Int64) || Type == typeof(Int64[]) || Type == typeof(List<Int64>) ||
                Type == typeof(Single) || Type == typeof(Single[]) || Type == typeof(List<Single>))
            {
                return true;
            }
            return false;
        }

        [Benchmark]
        public bool EnsureSupportedTypeCached()
        {
            if (Type == Byte || Type == ByteArray || Type == ByteList ||
                Type == ByteMemory ||
                Type == String || Type == Bool ||
                Type == Char || Type == CharArray || Type == CharList ||
                Type == UInt16 || Type == UInt16Array || Type == UInt16List ||
                Type == UInt32 || Type == UInt32Array || Type == UInt32List ||
                Type == UInt64 || Type == UInt64Array || Type == UInt64List ||
                Type == Int16 || Type == Int16Array || Type == Int16List ||
                Type == Int32 || Type == Int32Array || Type == Int32List ||
                Type == Int64 || Type == Int64Array || Type == Int64List ||
                Type == Single || Type == SingleArray || Type == SingleList)
            {
                return true;
            }
            return false;
        }
    }
}
