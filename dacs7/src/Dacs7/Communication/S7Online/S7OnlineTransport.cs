using Dacs7.Exceptions;
using Dacs7.Protocols.Fdl;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dacs7.Communication.S7Online
{
    internal class S7OnlineTransport : Transport
    {
        private enum S7OnlineStates
        {
            Disconnected,
            ConnectState1,
            ConnectState2,
            ConnectState3,
            ConnectState4,
            ConnectState5,
            ConnectState6,
            Connected,
            Broken
        }
        private S7OnlineStates _s7OnlineState;
        private readonly FdlProtocolContext _context;

        public S7OnlineTransport(FdlProtocolContext context, S7OnlineConfiguration config) : base(context, config)
        {
            _context = context;
        }

        private async Task<int> S7OnlineHandler(string socketHandle, Memory<byte> buffer)
        {
            var context = _context;
            var processed = 0;
            var datagram = !buffer.IsEmpty ? RequestBlockDatagram.TranslateFromMemory(buffer.Slice(processed), out processed) : null;
            switch (_s7OnlineState)
            {
                case S7OnlineStates.Disconnected:
                    {
                        if ((_context).IsEthernet)
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet1(context))))
                            {
                                _s7OnlineState = S7OnlineStates.ConnectState3;
                            }
                        }
                        else
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildStationRequest(context))))
                            {
                                _s7OnlineState = S7OnlineStates.ConnectState1;
                            }
                            else
                            {
                                _s7OnlineState = S7OnlineStates.Broken;
                                throw new S7OnlineException();
                            }
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState1:
                    {
                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildReadBusParameter(context))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState2;
                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState2:
                    {
                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet1(context))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState3;
                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState3:
                    {
                        if (datagram.Header.OpCode == 0x00 && datagram.Header.Response == 0x01)
                        {
                            context.OpCode = datagram.ApplicationBlock.Opcode;
                            context.Subsystem = datagram.ApplicationBlock.Subsystem;
                        }

                        if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet2(context))))
                        {
                            _s7OnlineState = S7OnlineStates.ConnectState4;
                            processed = buffer.Length;
                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }

                        return processed;
                    }
                case S7OnlineStates.ConnectState4:
                    {
                        if (datagram.Header.Response == 0x01 || datagram.Header.Response == 0x02)
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildEthernet3(context))))
                            {

                                _s7OnlineState = S7OnlineStates.ConnectState5;
                                processed = buffer.Length;
                            }
                            else
                            {
                                _s7OnlineState = S7OnlineStates.Broken;
                                throw new S7OnlineException();
                            }
                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }
                        return processed;
                    }
                case S7OnlineStates.ConnectState5:
                    {
                        if (datagram.Header.Response == 0x02)
                        {
                            if (await SendS7Online(RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.BuildReadBusParameter(context))))
                            {

                                _s7OnlineState = S7OnlineStates.ConnectState5;
                                processed = buffer.Length;
                            }
                            else
                            {
                                _s7OnlineState = S7OnlineStates.Broken;
                                throw new S7OnlineException();
                            }

                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }
                        return processed;
                    }
                case S7OnlineStates.ConnectState6:
                    {
                        if (datagram.Header.Response == 0x02)
                        {
                            _s7OnlineState = S7OnlineStates.Connected;
                            await OnUpdateConnectionState?.Invoke(Protocols.ConnectionState.TransportOpened);
                            processed = buffer.Length;
                        }
                        else
                        {
                            _s7OnlineState = S7OnlineStates.Broken;
                            throw new S7OnlineException();
                        }
                        return processed;
                    }
                case S7OnlineStates.Connected:
                    {

                        if (datagram == null)
                            return processed;

                        var data = datagram.UserData1.Slice(0, datagram.Header.SegLength1);
                        if (await OnDetectAndReceive?.Invoke(data))
                        {
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

        public override Memory<byte> Build(Memory<byte> buffer) => RequestBlockDatagram.TranslateToMemory(RequestBlockDatagram.Build(_context, buffer));

        private Task OnS7OnlineConnectionStateChanged(string socketHandle, bool connected)
        {
            var state = OnGetConnectionState?.Invoke();
            if (state == Protocols.ConnectionState.Closed && connected)
            {
                return S7OnlineHandler(socketHandle, Memory<byte>.Empty);
            }
            else if (state == Protocols.ConnectionState.Opened && !connected)
            {
                _s7OnlineState = S7OnlineStates.Disconnected;
                return OnUpdateConnectionState(Protocols.ConnectionState.Closed);
            }
            return Task.CompletedTask;
        }

        private async Task<bool> SendS7Online(Memory<byte> buffer)
        {
            var result = await Client.SendAsync(buffer);
            return result == SocketError.Success;
        }

        public override void ConfigureClient(ILoggerFactory loggerFactory)
        {
            Client = new S7OnlineClient(Configuration as S7OnlineConfiguration, loggerFactory)
            {
                OnRawDataReceived = OnS7OnlineRawDataReceived,
                OnConnectionStateChanged = OnS7OnlineConnectionStateChanged
            };
        }

    }
}
