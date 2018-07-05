using Dacs7.Protocols.Rfc1006;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal partial class ProtocolHandler
    {
        private Task<int> OnTcpSocketRawDataReceived(string socketHandle, Memory<byte> buffer)
        {
            if (buffer.Length > Rfc1006ProtocolContext.MinimumBufferSize)
            {
                if (_RfcContext.TryDetectDatagramType(buffer, out var type))
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

        private Memory<byte> BuildForTcp(Memory<byte> buffer)
        {
            return DataTransferDatagram.TranslateToMemory(DataTransferDatagram.Build(_RfcContext, buffer).FirstOrDefault());
        }

        private Task OnTcpSocketConnectionStateChanged(string socketHandle, bool connected)
        {
            if (ConnectionState == ConnectionState.Closed && connected)
            {
                return SendTcpConnectionRequest();
            }
            else if (ConnectionState == ConnectionState.Opened && !connected)
            {
                return Closed();
            }
            return Task.CompletedTask;
        }


        private async Task<int> Rfc1006DatagramReceived(Type datagramType, Memory<byte> buffer)
        {
            var processed = 0;
            if (datagramType == typeof(ConnectionConfirmedDatagram))
            {
                var res = ConnectionConfirmedDatagram.TranslateFromMemory(buffer, out processed);
                await TransportOpened();
            }
            else if (datagramType == typeof(DataTransferDatagram))
            {
                var datagram = DataTransferDatagram.TranslateFromMemory(buffer.Slice(processed), _RfcContext, out var needMoreData, out processed);
                if (!needMoreData && _s7Context.TryDetectDatagramType(datagram.Payload, out var s7DatagramType))
                {
                    await S7DatagramReceived(s7DatagramType, datagram.Payload);
                }
            }

            return processed;
        }


        private async Task SendTcpConnectionRequest()
        {
            var result = await _socket.SendAsync(ConnectionRequestDatagram.TranslateToMemory(ConnectionRequestDatagram.BuildCr(_RfcContext)));
            if (result == SocketError.Success)
            {
                UpdateConnectionState(ConnectionState.PendingOpenTransport);
            }
        }


    }
}
