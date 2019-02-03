using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dacs7.Domain
{
    internal static class ConvertHelpers
    {
        public static Memory<byte> WriteSingleBigEndian(Single value, byte[] buffer = null, int offset = 0)
        {
            var rawdata = buffer ?? new byte[Marshal.SizeOf(value)];
            var handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject() + offset, false);
            handle.Free();
            Swap4BytesInBuffer(rawdata, offset);
            return rawdata;
        }

        public static byte[] Swap4BytesInBuffer(byte[] rawdata, int offset = 0)
        {
            var b = rawdata[offset];
            rawdata[offset] = rawdata[offset + 3];
            rawdata[offset + 3] = b;
            b = rawdata[offset + 1];
            rawdata[offset + 1] = rawdata[offset + 2];
            rawdata[offset + 2] = b;
            return rawdata;
        }

        public static void EnsureSupportedType(ReadItem item)
        {
            if (item.ResultType == typeof(byte) || item.ResultType == typeof(byte[]) || item.ResultType == typeof(List<byte>) ||
                item.ResultType == typeof(Memory<byte>) ||
                item.ResultType == typeof(string) || item.ResultType == typeof(bool) ||
                item.ResultType == typeof(char) || item.ResultType == typeof(char[]) || item.ResultType == typeof(List<char>) ||
                item.ResultType == typeof(UInt16) || item.ResultType == typeof(UInt16[]) || item.ResultType == typeof(List<UInt16>) ||
                item.ResultType == typeof(UInt32) || item.ResultType == typeof(UInt32[]) || item.ResultType == typeof(List<UInt32>) ||
                item.ResultType == typeof(Int16) || item.ResultType == typeof(Int16[]) || item.ResultType == typeof(List<Int16>) ||
                item.ResultType == typeof(Int32) || item.ResultType == typeof(Int32[]) || item.ResultType == typeof(List<Int32>) ||
                item.ResultType == typeof(Single) || item.ResultType == typeof(Single[]) || item.ResultType == typeof(List<Single>))
            {
                return;
            }

            ExceptionThrowHelper.ThrowTypeNotSupportedException(item.ResultType);
        }



    }
}
