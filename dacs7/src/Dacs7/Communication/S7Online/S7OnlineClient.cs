using Dacs7.Communication.S7Online;
using Dacs7.Exceptions;
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dacs7.Communication
{
    internal class S7OnlineClient : SocketBase
    {
        private bool _disableReconnect;
        private bool _closeCalled;
        private int _connectionHandle = -1;
        private Task _receiveTask;
        private AsyncAutoResetEvent<bool> _sentEvent = new AsyncAutoResetEvent<bool>();

        private readonly S7OnlineConfiguration _config;
        public override string Identity
        {
            get
            {

                if (_identity == null)
                {
                    //if (_socket != null)
                    //{
                    //    var epLocal = _socket.LocalEndPoint as IPEndPoint;
                    //    IPEndPoint epRemote = null;
                    //    try
                    //    {
                    //        epRemote = _socket.RemoteEndPoint as IPEndPoint;
                    //        _identity = $"{epLocal.Address}:{epLocal.Port}-{(epRemote != null ? epRemote.Address.ToString() : _configuration.Hostname)}:{(epRemote != null ? epRemote.Port : _configuration.ServiceName)}";
                    //    }
                    //    catch (Exception)
                    //    {
                    //        return string.Empty;
                    //    };
                    //}
                    //else
                    //    return string.Empty;
                }
                return _identity;
            }
        }

        public S7OnlineClient(S7OnlineConfiguration configuration) : base(configuration)
        {
            _config = configuration;
        }


        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        /// </summary>
        public override Task OpenAsync()
        {
            _closeCalled = false;
            _disableReconnect = true;
            return InternalOpenAsync();
        }

        private async Task InternalOpenAsync(bool internalCall = false)
        {
            try
            {
                if (_closeCalled) return;
                _identity = null;


                // Connect
                _connectionHandle = Native.SCP_open("S7ONLINE");    // TODO: Configurable

                if (_connectionHandle >= 0)
                {
                    _disableReconnect = false; // we have a connection, so enable reconnect
                    _receiveTask = Task.Factory.StartNew(() => StartReceive(), TaskCreationOptions.LongRunning);
                    await PublishConnectionStateChanged(true);
                }
                else
                {
                    throw new S7OnlineException(); // todo create real exception
                }
            }
            catch (Exception)
            {
                await DisposeSocket();
                await HandleSocketDown();
                if (!internalCall) throw;
            }
        }

        public override Task<SocketError> SendAsync(Memory<byte> data)
        {
            // Write the locally buffered data to the network.
            try
            {
                var sendData = data.ToArray();
                int ret = Native.SCP_send(_connectionHandle, (ushort)data.Length, sendData);
                if (ret < 0)
                {
                    return Task.FromResult(SocketError.Fault);
                }
                File.AppendAllText("TraceOut.txt", $"===  Start Sent {data.Length} bytes ====");
                File.AppendAllText("TraceOut.txt", Environment.NewLine);
                File.AppendAllText("TraceOut.txt", ByteArrayToString(sendData));
                File.AppendAllText("TraceOut.txt", Environment.NewLine);
                File.AppendAllText("TraceOut.txt", $"===  End Sent ====");
                File.AppendAllText("TraceOut.txt", Environment.NewLine);
                _sentEvent.Set(true);
            }
            catch (Exception)
            {
                //TODO
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                //{
                //    throw;
                //}
                return Task.FromResult(SocketError.Fault);
            }
            return Task.FromResult( SocketError.Success);
        }

        public async override Task CloseAsync()
        {
            _disableReconnect = _closeCalled = true;
            await base.CloseAsync();
            await DisposeSocket();
        }

        private async Task DisposeSocket()
        {
            if (_connectionHandle != -1)
            {
                Native.SCP_close(_connectionHandle);
                _connectionHandle = -1;
            }
            _sentEvent.Set(false);

            if (_receiveTask != null)
            {
                await _receiveTask;
            }
        }

        private async Task StartReceive()
        {
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
            var receiveOffset = 0;
            var bufferOffset = 0;
            var span = new Memory<byte>(receiveBuffer);
            var receivedLength = new int[1];
            try
            {
                while (_connectionHandle >= 0)
                {
                    try
                    {

                        //await _sentEvent.WaitAsync();

                        // 0xffff  wait forever
                        var result = Native.SCP_receive(_connectionHandle, 0xffff, receivedLength, (ushort)ReceiveBufferSize, receiveBuffer);

                        var received = receivedLength[0];
                        if (result == -1 || received == 0)
                        {
                            await Task.Delay(1);
                            continue;
                        }

                        var toProcess = received + (receiveOffset - bufferOffset);
                        var processed = 0;
                        do
                        {
                            var off = bufferOffset + processed;
                            var length = toProcess - processed;
                            var slice = span.Slice(off, length);
                            File.AppendAllText("TraceIn.txt", $"===  Start Received {length} bytes ====");
                            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                            File.AppendAllText("TraceIn.txt", ByteArrayToString(slice.ToArray()));
                            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                            File.AppendAllText("TraceIn.txt", $"===  End Received ====");
                            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                            var proc = await ProcessData(slice);
                            if (proc == 0)
                            {
                                if (length > 0)
                                {

                                    receiveOffset += received;
                                    bufferOffset = receiveOffset - (toProcess - processed);
                                }
                                else
                                {
                                    receiveOffset = 0;
                                    bufferOffset = 0;
                                }
                                break;
                            }
                            processed += proc;
                        } while (processed < toProcess);
                    }
                    catch (Exception ex) when (!(ex is S7OnlineException)) { }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                _ = HandleSocketDown();
            }

        }
        protected override Task HandleSocketDown()
        {
            _ = HandleReconnectAsync();
            return PublishConnectionStateChanged(false);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }


        private async Task HandleReconnectAsync()
        {
            if (!_disableReconnect && _configuration.AutoconnectTime > 0)
            {
                await Task.Delay(_configuration.AutoconnectTime);
                await InternalOpenAsync(true);
            }
        }
    }
}
