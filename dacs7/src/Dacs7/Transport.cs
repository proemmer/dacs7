using Dacs7.Communication;
using Dacs7.Protocols;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;

namespace Dacs7
{
    internal abstract class Transport
    {
        public OnUpdateConnectionState OnUpdateConnectionState;
        public OnDetectAndReceive OnDetectAndReceive;
        public OnGetConnectionState OnGetConnectionState;

        public Transport(IProtocolContext context, IConfiguration config)
        {
            ProtocolContext = context;
            Configuration = config;
        }


        public SocketBase Client { get; protected set; }
        public IConfiguration Configuration { get; private set; }
        public IProtocolContext ProtocolContext { get; private set; }


        public abstract void ConfigureClient(ILoggerFactory loggerFactory);
        public abstract IMemoryOwner<byte> Build(Memory<byte> buffer, out int length);
    }
}
