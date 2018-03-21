using Dacs7.Communication;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{

    public enum ConnectionState
    {
        Closed,
        PendingOpenRfc1006,
        Rfc1006Opened,
        PendingOpenPlc,
        Opened
    }

    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public partial class Dacs7Client
    {
        private ConnectionState _connectionState = ConnectionState.Closed;
        private ClientSocketConfiguration _config;
        private ClientSocket _socket;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private const int ReconnectPeriod = 10;

        public Dacs7Client(string address, PlcConnectionType connectionType = PlcConnectionType.Pg)
        {
            var addressPort = address.Split(':');
            var portRackSlot = addressPort.Length > 1 ? 
                                        addressPort[1].Split(',').Select(x => Int32.Parse(x)).ToArray() : 
                                        new int[] { 102, 0, 2 };

            _config = new ClientSocketConfiguration
            {
                Hostname = addressPort[0],
                ServiceName = portRackSlot.Length > 0 ? portRackSlot[0] : 102
            };
            _socket = new ClientSocket(_config)
            {
                OnRawDataReceived = OnRawDataReceived,
                OnConnectionStateChanged = OnConnectionStateChanged
            };


            _context = new Rfc1006ProtocolContext
            {
                DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType, 
                                                                 portRackSlot.Length > 1 ? portRackSlot[1] : 0,
                                                                 portRackSlot.Length > 2 ? portRackSlot[2] : 2)
            };
        }



        public async Task ConnectAsync()
        {
            await _socket.OpenAsync();
            if(!_socket.IsConnected)
            {
                // Throw!
            }
        }

        public async Task DisconnectAsync()
        {
            await _socket.CloseAsync();
        }

        public Task<IEnumerable<object>> ReadAsync(params string[] values) => ReadAsync(values as IEnumerable<string>);

        public Task<IEnumerable<object>> ReadAsync(IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(params KeyValuePair<string, object>[] values) => WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        public Task WriteAsync(IEnumerable<KeyValuePair<string, object>> values)
        {
            throw new NotImplementedException();

        }





        private async void OnConnectionStateChanged(string socketHandle, bool connected)
        {
            if(_connectionState == ConnectionState.Closed && connected)
            {
                await OpenRfc1006();
            }
            else if(_connectionState == ConnectionState.Opened && !connected)
            {
                await PlcClosed();
            }
        }


        private void OnRawDataReceived(string socketHandle, Memory<byte> aBuffer)
        {
            // DETECT TYPE
        }




        private async Task OpenRfc1006()
        {
            var sendData = ConnectionRequestDatagram.TranslateToMemory(ConnectionRequestDatagram.BuildCr(_context));
            var result = await _socket.SendAsync(sendData);
            if (result == System.Net.Sockets.SocketError.Success)
                _connectionState = ConnectionState.PendingOpenRfc1006;
        }

        private async Task OpenPlc()
        {
            var sendData = DataTransferDatagram
                                    .TranslateToMemory(
                                        DataTransferDatagram
                                        .Build(_context,
                                            S7CommunicationJobDatagram
                                            .TranslateToMemory(
                                                S7CommunicationJobDatagram
                                                .Build(_s7Context))).FirstOrDefault());
            var result = await _socket.SendAsync(sendData);
            if (result == System.Net.Sockets.SocketError.Success)
                _connectionState = ConnectionState.PendingOpenPlc;
        }

        private async Task Rfc1006Opened()
        {
            _connectionState = ConnectionState.Rfc1006Opened;
            await OpenPlc();
        }

        private Task PlcOpened()
        {
            _connectionState = ConnectionState.Opened;
            // Received CommJobJobAck
            return Task.CompletedTask;
        }

        private Task PlcClosed()
        {
            _connectionState = ConnectionState.Closed;
            return Task.CompletedTask;
        }
    }
}