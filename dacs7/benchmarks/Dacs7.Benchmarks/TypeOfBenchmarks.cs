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

        public static Type UInt16 = typeof(ushort);
        public static Type UInt16Array = typeof(ushort[]);
        public static Type UInt16List = typeof(List<ushort>);

        public static Type Int16 = typeof(short);
        public static Type Int16Array = typeof(short[]);
        public static Type Int16List = typeof(List<short>);

        public static Type UInt32 = typeof(uint);
        public static Type UInt32Array = typeof(uint[]);
        public static Type UInt32List = typeof(List<uint>);

        public static Type Int32 = typeof(int);
        public static Type Int32Array = typeof(int[]);
        public static Type Int32List = typeof(List<int>);

        public static Type UInt64 = typeof(ulong);
        public static Type UInt64Array = typeof(ulong[]);
        public static Type UInt64List = typeof(List<ulong>);

        public static Type Int64 = typeof(long);
        public static Type Int64Array = typeof(long[]);
        public static Type Int64List = typeof(List<long>);

        public static Type Single = typeof(float);
        public static Type SingleArray = typeof(float[]);
        public static Type SingleList = typeof(List<float>);

        public static Type Bool = typeof(bool);

        [Params(typeof(List<float>), typeof(byte), typeof(char), typeof(bool), typeof(TypeOfBenchmarks))]
        public Type Type;

        [Benchmark]
        public bool EnsureSupportedTypeNonCached()
        {
            if (Type == typeof(byte) || Type == typeof(byte[]) || Type == typeof(List<byte>) ||
                Type == typeof(Memory<byte>) ||
                Type == typeof(string) || Type == typeof(bool) ||
                Type == typeof(char) || Type == typeof(char[]) || Type == typeof(List<char>) ||
                Type == typeof(ushort) || Type == typeof(ushort[]) || Type == typeof(List<ushort>) ||
                Type == typeof(uint) || Type == typeof(uint[]) || Type == typeof(List<uint>) ||
                Type == typeof(ulong) || Type == typeof(ulong[]) || Type == typeof(List<ulong>) ||
                Type == typeof(short) || Type == typeof(short[]) || Type == typeof(List<short>) ||
                Type == typeof(int) || Type == typeof(int[]) || Type == typeof(List<int>) ||
                Type == typeof(long) || Type == typeof(long[]) || Type == typeof(List<long>) ||
                Type == typeof(float) || Type == typeof(float[]) || Type == typeof(List<float>))
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
