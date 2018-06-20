using System;
using System.Net;
using System.Threading;

namespace Dacs7.Protocols.Fdl
{
    public class FdlProtocolContext : IProtocolContext
    {
        private readonly object _userLock = new object();
        private int _user = -1;


        public static int MinimumBufferSize = 80;

        public UInt16 User => GetNextReferenceId();

        public byte OpCode { get; set; } = 0xFF;
        public byte Subsystem { get; set; } = 0xFF;

        public PlcConnectionType ConnectionType { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public IPAddress Address { get; set; }

        internal UInt16 GetNextReferenceId()
        {
            var id = Interlocked.Increment(ref _user);

            if (id < UInt16.MinValue || id > UInt16.MaxValue)
            {
                lock (_userLock)
                {
                    id = Interlocked.Increment(ref _user);
                    if (id < UInt16.MinValue || id > UInt16.MaxValue)
                    {
                        Interlocked.Exchange(ref _user, 0);
                        id = Interlocked.Increment(ref _user);
                    }
                }
            }
            return Convert.ToUInt16(id);

        }

    }
}