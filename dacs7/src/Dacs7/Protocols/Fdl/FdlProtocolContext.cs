using System;
using System.Threading;

namespace Dacs7.Protocols.Fdl
{
    public class FdlProtocolContext : IProtocolContext
    {
        private readonly object _userLock = new object();
        private int _user;

        public UInt16 User => GetNextReferenceId();


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