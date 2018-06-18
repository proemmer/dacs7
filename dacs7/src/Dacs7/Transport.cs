using Dacs7.Communication;
using Dacs7.Protocols;

namespace Dacs7
{
    internal class Transport
    {
        public IConfiguration Configuration { get; set; }
        public IProtocolContext ProtocolContext { get; set; }
    }
}
