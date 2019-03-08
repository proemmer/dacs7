// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Protocols;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;

namespace Dacs7.Communication
{
    /// <summary>
    /// This is the baseclass of the transport mechanism. Currently we support sockets, but there could also be some other transport methods.
    /// </summary>
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
