// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal sealed class S7UserData
    {
        public byte ReturnCode { get; set; }
        public byte TransportSize { get; set; }
        public ushort UserDataLength { get; set; }

        public Memory<byte> Data { get; set; }



        public int GetUserDataLength()
        {
            return 4 + UserDataLength;
        }
    }
}
