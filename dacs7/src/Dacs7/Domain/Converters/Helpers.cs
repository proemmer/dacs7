﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;
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
    }
}
