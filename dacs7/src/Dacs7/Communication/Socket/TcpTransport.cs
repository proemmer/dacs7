// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Communication.Socket
{
    internal sealed class TcpTransport : Transport
    {
        private readonly Rfc1006ProtocolContext _context;

        public TcpTransport(Rfc1006ProtocolContext context, ClientSocketConfiguration config) : base(context, config) => _context = context;


        public sealed override void ConfigureClient(ILoggerFactory loggerFactory)
        {
            Client = new ClientSocket(Configuration as ClientSocketConfiguration, loggerFactory)
            {
                OnRawDataReceived = OnTcpSocketRawDataReceived,
                OnConnectionStateChanged = OnTcpSocketConnectionStateChanged
            };
        }


        public sealed override IMemoryOwner<byte> Build(Memory<byte> buffer, out int length)
        {
            using (var dg = DataTransferDatagram.Build(_context, buffer).FirstOrDefault())
            {
                length = DataTransferDatagram.GetRawDataLength(dg);
                var resultBuffer = MemoryPool<byte>.Shared.Rent(length);
                try
                {
                    DataTransferDatagram.TranslateToMemory(dg, resultBuffer.Memory.Slice(0, length));
                }
                catch (Exception)
                {
                    // we have to dispose the buffer when we got an exception, because we are the owner.
                    resultBuffer.Dispose();
                    throw;
                }
                return resultBuffer;
            }
        }


        private Task<int> OnTcpSocketRawDataReceived(string socketHandle, Memory<byte> buffer)
        {
            if (buffer.Length > Rfc1006ProtocolContext.MinimumBufferSize)
            {
                if (_context.TryDetectDatagramType(buffer, out var type))
                {
                    return Rfc1006DatagramReceived(type, buffer);
                }
                // unknown datagram
            }
            else
            {
                return Task.FromResult(0); // no data processed, buffer is to short
            }
            return Task.FromResult(1); // move forward
        }

        private Task OnTcpSocketConnectionStateChanged(string socketHandle, bool connected)
        {
            var state = OnGetConnectionState?.Invoke();
            if (state == ConnectionState.Closed && connected) return SendTcpConnectionRequest();
            if (state != ConnectionState.Closed && !connected) return OnUpdateConnectionState?.Invoke(ConnectionState.Closed); // close in any case
            return Task.CompletedTask;
        }


        private async Task<int> Rfc1006DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            var processed = 0;
            var context = _context;
            if (datagramType == typeof(ConnectionConfirmedDatagram))
            {
                using (var res = ConnectionConfirmedDatagram.TranslateFromMemory(buffer, out processed))
                {
                    context.UpdateFrameSize(res);
                    await (OnUpdateConnectionState?.Invoke(ConnectionState.TransportOpened)).ConfigureAwait(false);
                }
            }
            else if (datagramType == typeof(DataTransferDatagram))
            {
                using (var datagram = DataTransferDatagram.TranslateFromMemory(buffer, context, out var needMoreData, out processed))
                {
                    if (!needMoreData)
                    {
                        await (OnDetectAndReceive?.Invoke(datagram.Payload)).ConfigureAwait(false);
                    }
                }
            }

            return processed;
        }

        private async Task SendTcpConnectionRequest()
        {
            using (var datagram = ConnectionRequestDatagram.TranslateToMemory(ConnectionRequestDatagram.BuildCr(_context), out var memoryLegth))
            {
                var result = await Client.SendAsync(datagram.Memory.Slice(0, memoryLegth)).ConfigureAwait(false);
                if (result == SocketError.Success)
                {
                    OnUpdateConnectionState?.Invoke(ConnectionState.PendingOpenTransport);
                }
            }
        }
    }
}
