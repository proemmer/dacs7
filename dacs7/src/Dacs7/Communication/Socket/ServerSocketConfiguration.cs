// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7.Communication.Socket
{
    public class ServerSocketConfiguration : ISocketConfiguration
    {
        public string Hostname { get; set; } = "localhost";
        public int ServiceName { get; set; } = 22112;
        public int ReceiveBufferSize { get; set; } = 10 * 1024;  // buffer size to use for each socket I/O operation 
        public int AutoconnectTime { get; set; } = 5000; // <= 0 means disabled
        public string NetworkAdapter { get; set; }
        public bool KeepAlive { get; set; } = false;

        public ServerSocketConfiguration()
        {
        }

        public sealed override string ToString()
        {
            return $"Socket: Hostname={Hostname}; ServiceName={ServiceName}; ReceiveBufferSize={ReceiveBufferSize}; KeepAlive={KeepAlive}";
        }
    }
}