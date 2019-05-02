// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7.Protocols.Rfc1006
{
    internal sealed class TpktDatagram
    {
        public byte Sync1 { get; set; }
        public byte Sync2 { get; set; }
        public ushort Length { get; set; } = 4;
    }
}
