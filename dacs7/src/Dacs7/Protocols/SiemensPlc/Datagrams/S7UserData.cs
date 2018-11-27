using System;

namespace Dacs7.Protocols.SiemensPlc.Datagrams
{
    internal class S7UserData
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
