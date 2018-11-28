using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Communication.Socket
{
    internal class TcpTransport : Transport
    {
        private readonly Rfc1006ProtocolContext _context;

        public TcpTransport(Rfc1006ProtocolContext context, ClientSocketConfiguration config) : base(context, config)
        {
            _context = context;
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
            if (state == ConnectionState.Closed && connected)
            {
                return SendTcpConnectionRequest();
            }
            else if (state == Protocols.ConnectionState.Opened && !connected)
            {
                return OnUpdateConnectionState?.Invoke(ConnectionState.Closed);
            }
            return Task.CompletedTask;
        }


        private async Task<int> Rfc1006DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            var processed = 0;
            var context = _context;
            if (datagramType == typeof(ConnectionConfirmedDatagram))
            {
                var res = ConnectionConfirmedDatagram.TranslateFromMemory(buffer, out processed);
                context.UpdateFrameSize(res);
                await OnUpdateConnectionState?.Invoke(Protocols.ConnectionState.TransportOpened);
            }
            else if (datagramType == typeof(DataTransferDatagram))
            {
                var datagram = DataTransferDatagram.TranslateFromMemory(buffer.Slice(processed), context, out var needMoreData, out processed);
                if (!needMoreData)
                {
                    await OnDetectAndReceive?.Invoke(datagram.Payload);
                }
            }

            return processed;
        }



        private async Task SendTcpConnectionRequest()
        {
            var result = await Client.SendAsync(ConnectionRequestDatagram.TranslateToMemory(ConnectionRequestDatagram.BuildCr(_context)));
            if (result == SocketError.Success)
            {
                OnUpdateConnectionState?.Invoke(ConnectionState.PendingOpenTransport);
            }
        }

        public override void ConfigureClient(ILoggerFactory loggerFactory)
        {
            Client = new ClientSocket(Configuration as ClientSocketConfiguration, loggerFactory)
            {
                OnRawDataReceived = OnTcpSocketRawDataReceived,
                OnConnectionStateChanged = OnTcpSocketConnectionStateChanged
            };
        }

        public override Memory<byte> Build(Memory<byte> buffer) => DataTransferDatagram.TranslateToMemory(DataTransferDatagram.Build(_context, buffer).FirstOrDefault());
    }
}
