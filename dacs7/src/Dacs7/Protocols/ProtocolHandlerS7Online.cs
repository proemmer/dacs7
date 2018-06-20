using Dacs7.Exceptions;
using Dacs7.Protocols.Fdl;
using Dacs7.Protocols.Rfc1006;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dacs7.Protocols
{
    internal partial class ProtocolHandler
    {
        private enum S7OnlineStates
        {
            ConnectState1,
            ConnectState2,
            ConnectState3,
            Connected,
        }

        private S7OnlineStates _s7OnlineState;


        private async Task<int> OnS7OnlineRawDataReceived(string socketHandle, Memory<byte> buffer)
        {

            if (buffer.Length >= FdlProtocolContext.MinimumBufferSize)
            {
                var processed = 0;
                var datagram = RequestBlockDatagram.TranslateFromMemory(buffer.Slice(processed), out processed);
                if (datagram == null)
                    return processed;

                switch (_s7OnlineState)
                {
                    case S7OnlineStates.ConnectState1:
                        {
                            if (datagram.Header.OpCode == 0x00 && datagram.Header.Response == 0x0100)
                            {
                                _FdlContext.OpCode = datagram.ApplicationBlock.Opcode;
                                _FdlContext.Subsystem = datagram.ApplicationBlock.Subsystem;
                            }

                            var request = RequestBlockDatagram.Build(_FdlContext, S7ConnectionConfig.TranslateToMemory(S7ConnectionConfig.BuildS7ConnectionConfig(_FdlContext)));
                            request.Header.OpCode = 1;
                            request.ApplicationBlock.Ssap = 2;
                            request.ApplicationBlock.RemoteAddress.Station = 114;

                            var result = await _socket.SendAsync(RequestBlockDatagram.TranslateToMemory(request));
                            if (result == SocketError.Success)
                            {
                                processed = buffer.Length;
                            }


                            _s7OnlineState = S7OnlineStates.ConnectState2;
                            return processed;
                        }
                    case S7OnlineStates.ConnectState2:
                        {
                            //if(datagram.Header.Response == 0x02)
                            {
                                _s7OnlineState = S7OnlineStates.Connected;
                                await TransportOpened();
                                processed = buffer.Length;
                            }
                            return processed;
                        }
                    case S7OnlineStates.ConnectState3:
                        {

                        }
                        break;
                    case S7OnlineStates.Connected:
                        {
                            var data = datagram.UserData1.Slice(0, datagram.Header.SegLength1);
                            if (_s7Context.TryDetectDatagramType(data, out var s7DatagramType))
                            {
                                await S7DatagramReceived(s7DatagramType, data);
                                return processed;
                            }
                            else
                            {
                                return 0; // no data processed, buffer is to short
                            }
                        }
                }
            }

            return 1; // move forward
        }

        private Memory<byte> BuildForS7Online(Memory<byte> buffer)
        {
            var dg = RequestBlockDatagram.Build(_FdlContext, buffer);
            return RequestBlockDatagram.TranslateToMemory(dg);
        }

        private Task OnS7OnlineConnectionStateChanged(string socketHandle, bool connected)
        {
            if (_connectionState == ConnectionState.Closed && connected)
            {
                return SendS7OnlineConnectionRequest1();
            }
            else if (_connectionState == ConnectionState.Opened && !connected)
            {
                return Closed();
            }
            return Task.CompletedTask;
        }

        private async Task SendS7OnlineConnectionRequest1()
        {
            var result = await _socket.SendAsync(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.Build(_FdlContext, Memory<byte>.Empty)));
            if (result == SocketError.Success)
            {
                UpdateConnectionState(ConnectionState.PendingOpenTransport);
            }
        }

    }
}
