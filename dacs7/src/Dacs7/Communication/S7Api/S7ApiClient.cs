using Dacs7.Communication.S7Api;
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
    internal class S7ApiClient : SocketBase
    {

        private uint _cpDescricpion = 0;
        private Task _receiveTask;
        private AsyncAutoResetEvent<bool> _sentEvent = new AsyncAutoResetEvent<bool>();

        private readonly S7ApiConfiguration _config;
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

        public S7ApiClient(S7ApiConfiguration configuration) : base(configuration)
        {
            _config = configuration;
        }


        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        /// </summary>
        public override async Task OpenAsync()
        {
            await base.OpenAsync();
            await InternalOpenAsync();
        }

        protected override async Task InternalOpenAsync(bool internalCall = false)
        {
            try
            {
                if (_shutdown) return;
                _identity = null;
                ushort number = 0;
                ushort cref = 0;
                var devName = new byte[129];

                // Connect
                var result = Native.S7_get_device(0, ref number, devName);
                if (result != 0)
                {
                    throw new S7ApiException(result, $"ns7_get_device = {result}, number = {number}"); // todo create real exception
                }

                var vfdName = new byte[129];
                result = Native.S7_get_vfd(devName, 0, ref number, vfdName);
                if (result != 0 || number == 0)
                {
                    throw new S7ApiException(result, $"s7_get_vfd = {result}, number = {number}"); // todo create real exception
                }

                result = Native.S7_init(devName, vfdName, ref _cpDescricpion);
                if (result != 0)
                {
                    throw new S7ApiException(result, $"s7_init = {result}, number = {number}"); // todo create real exception
                }

                result = Native.S7_get_cref(_cpDescricpion, Encoding.ASCII.GetBytes(_config.CpDescription), ref cref);
                if (result != 0)
                {
                    throw new S7ApiException(result, $"s7_get_cref = {result}, number = {number}"); // todo create real exception
                }

                if (_cpDescricpion >= 0)
                {
                    _disableReconnect = false; // we have a connection, so enable reconnect
                    _receiveTask = Task.Factory.StartNew(() => StartReceive(), TaskCreationOptions.LongRunning);
                    await PublishConnectionStateChanged(true);
                }
                else
                {
                    
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
                //int ret = Native.SCP_send(_cpDescricpion, (ushort)data.Length, sendData);
                //if (ret < 0)
                //{
                //    return Task.FromResult(SocketError.Fault);
                //}
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
            await base.CloseAsync();
            await DisposeSocket();
        }

        private async Task DisposeSocket()
        {
            if (_cpDescricpion != 0)
            {
                Native.S7_shut(_cpDescricpion);
                _cpDescricpion = 0;
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
                //while (_cpDescricpion >= 0)
                //{
                //    try
                //    {

                //        //await _sentEvent.WaitAsync();

                //        // 0xffff  wait forever
                //        var result = Native.SCP_receive(_cpDescricpion, 0xffff, receivedLength, (ushort)ReceiveBufferSize, receiveBuffer);

                //        var received = receivedLength[0];
                //        if (result == -1 || received == 0)
                //        {
                //            await Task.Delay(1);
                //            continue;
                //        }

                //        var toProcess = received + (receiveOffset - bufferOffset);
                //        var processed = 0;
                //        do
                //        {
                //            var off = bufferOffset + processed;
                //            var length = toProcess - processed;
                //            var slice = span.Slice(off, length);
                //            File.AppendAllText("TraceIn.txt", $"===  Start Received {length} bytes ====");
                //            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                //            File.AppendAllText("TraceIn.txt", ByteArrayToString(slice.ToArray()));
                //            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                //            File.AppendAllText("TraceIn.txt", $"===  End Received ====");
                //            File.AppendAllText("TraceIn.txt", Environment.NewLine);
                //            var proc = await ProcessData(slice);
                //            if (proc == 0)
                //            {
                //                if (length > 0)
                //                {

                //                    receiveOffset += received;
                //                    bufferOffset = receiveOffset - (toProcess - processed);
                //                }
                //                else
                //                {
                //                    receiveOffset = 0;
                //                    bufferOffset = 0;
                //                }
                //                break;
                //            }
                //            processed += proc;
                //        } while (processed < toProcess);
                //    }
                //    catch (Exception ex) when (!(ex is S7OnlineException)) { }
                //}
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
    }
}
