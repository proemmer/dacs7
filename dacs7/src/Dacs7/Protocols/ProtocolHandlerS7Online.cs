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
            Disconnected,
            ConnectState1,
            ConnectState2,
            ConnectState3,
            ConnectState4,
            ConnectState5,
            Connected,
        }

        private S7OnlineStates _s7OnlineState;



        private async Task<int> S7OnlineHandler(string socketHandle, Memory<byte> buffer)
        {
            var processed = 0;
            var datagram = !buffer.IsEmpty ? RequestBlockDatagram.TranslateFromMemory(buffer.Slice(processed), out processed) : null;
            switch (_s7OnlineState)
            {
                case S7OnlineStates.Disconnected:
                    {
                        if(_FdlContext.IsEthernet)
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet1(_FdlContext))))
                            {
                                _s7OnlineState = S7OnlineStates.ConnectState3;
                            }
                        }
                        else
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildCreateRemote(_FdlContext))))
                            {
                                _s7OnlineState = S7OnlineStates.ConnectState1;
                            }
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState1:
                    {
                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildReadFdlOnConnect(_FdlContext))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState2;
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState2:
                    {
                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet1(_FdlContext))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState3;
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState3:
                    { 
                        if (datagram.Header.OpCode == 0x00 && datagram.Header.Response == 0x01)
                        {
                            _FdlContext.OpCode = datagram.ApplicationBlock.Opcode;
                            _FdlContext.Subsystem = datagram.ApplicationBlock.Subsystem;
                        }

                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet2(_FdlContext))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState4;
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState4:
                    {
                        if (datagram.Header.Response == 0x01)
                        {
                            _s7OnlineState = S7OnlineStates.Connected;
                            await TransportOpened();
                            processed = buffer.Length;
                            _s7OnlineState = S7OnlineStates.Connected;
                            UpdateConnectionState(ConnectionState.PendingOpenTransport);
                        }
                        else
                            throw new S7OnlineException();
                        return processed;
                    }
                case S7OnlineStates.Connected:
                    {
                       
                        if (datagram == null)
                            return processed;

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
            return 1; // move forward
        }

        private Task<int> OnS7OnlineRawDataReceived(string socketHandle, Memory<byte> buffer)
        {
            if (buffer.Length >= FdlProtocolContext.MinimumBufferSize)
            {
                return S7OnlineHandler(socketHandle, buffer);
            }
            return Task.FromResult(1); // move forward
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
                return S7OnlineHandler(socketHandle, Memory<byte>.Empty);
            }
            else if (_connectionState == ConnectionState.Opened && !connected)
            {
                _s7OnlineState = S7OnlineStates.Disconnected;
                return Closed();
            }
            return Task.CompletedTask;
        }

        private async Task<bool> SendS7Online(Memory<byte> buffer)
        {
            var result = await _socket.SendAsync(buffer);
            return result == SocketError.Success;
        }




    }
}
