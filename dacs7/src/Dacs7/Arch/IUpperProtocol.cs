using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7
{
    interface IUpperProtocolHandler
    {
        Func<byte[], Task<SocketError>> SocketSendFunc { get; set; } 
        void Connect();
        void Shutdown();
        byte[] AddUpperProtocolFrame(byte[] txData);
        IEnumerable<byte[]> RemoveUpperProtocolFrame(byte[] rxData, int count);
        bool Connected { get; }
    }
}
