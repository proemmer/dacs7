using System;

namespace Dacs7.Protocols.Fdl
{
    internal class LinkServiceDataUnit
    {
        public Int32 BufferPtr { get; set; }   // address and length of received netto-data, exception:
        public byte Length { get; set; }         // address and length of received netto-data, exception:
    }


}
