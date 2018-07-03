using System;
using System.Net;
using System.Threading;

namespace Dacs7.Protocols.Fdl
{
    public class FdlProtocolContext : IProtocolContext
    {
        private readonly object _userLock = new object();
        private int _user = -1;
        internal const byte NettoDataOffset = 12;
        public static ushort UserDataMaxSize = 260;
        public static int MinimumBufferSize = 80;

        public UInt16 User => 1; // GetNextReferenceId();

        public ComClass OpCode { get; set; } = ComClass.Request;

        public byte RemoteAddress { get; set; }
        public byte RemoteSap { get; set; }
        public byte LocalSap { get; set; }

        /// <summary>
        /// When this is One it is a MPI Connection, zero means TCP Connection!
        /// </summary>
        public byte Subsystem { get; set; } = 0x00;


        public bool IsEthernet => Address != null;
        public bool EnableRouting => RoutingAddress != null;



        public int MpiAddress { get; set; }
        public PlcConnectionType ConnectionType { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public IPAddress Address { get; set; }
        public IPAddress RoutingAddress { get; set; }

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