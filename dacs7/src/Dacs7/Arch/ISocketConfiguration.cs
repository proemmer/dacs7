using System.Net;
using System.Net.Sockets;

namespace Dacs7.Arch
{
    public interface ISocketConfiguration
    {
        IPEndPoint Endpoint { get; set; }
        AddressFamily AddressFamily { get; set; }
        bool Autoconnect { get; set; }
        int Backlog { get; set; }
        int NumConnections { get; set; }
        int OpsToPreAlloc { get; set; }
        int ReceiveBufferSize { get; set; }
    }
}
