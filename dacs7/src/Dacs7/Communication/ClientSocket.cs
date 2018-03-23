using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Buffers;

namespace Dacs7.Communication
{

    internal class ClientSocket : SocketBase
    {
        private Socket _socket;

        public override string Identity
        {
            get
            {

                if (_identity == null)
                {
                    if (_socket != null)
                    {
                        var epLocal = _socket.LocalEndPoint as IPEndPoint;
                        IPEndPoint epRemote = null;
                        try
                        {
                            epRemote = _socket.RemoteEndPoint as IPEndPoint;
                            _identity = $"{epLocal.Address}:{epLocal.Port}-{(epRemote != null ? epRemote.Address.ToString() : _configuration.Hostname)}:{(epRemote != null ? epRemote.Port : _configuration.ServiceName)}";
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        };
                    }
                    else
                        return string.Empty;
                }
                return _identity;
            }
        }

        public ClientSocket(ClientSocketConfiguration configuration) : base(configuration)
        {
            
        }


        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        /// </summary>
        public async override Task OpenAsync()
        {
            try
            {
                _identity = null;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = _configuration.ReceiveBufferSize
                };
                await _socket.ConnectAsync(_configuration.Hostname, _configuration.ServiceName);
                if (IsReallyConnected())
                {
                    if (!_configuration.KeepAlive)
                        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    var ignore = Task.Factory.StartNew(() => StartReceive(), TaskCreationOptions.LongRunning);
                    await PublishConnectionStateChanged(true);
                }
                else
                    await HandleSocketDown();
            }
            catch (Exception)
            {
                await HandleSocketDown();
            }
        }

        public override async Task<SocketError> SendAsync(Memory<byte> data)
        {
            // Write the locally buffered data to the network.
            try
            {
                var result = await _socket.SendAsync(new ArraySegment<byte>(data.ToArray()), SocketFlags.None);
            }
            catch (Exception)
            {
                //TODO
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                //{
                //    throw;
                //}
                return SocketError.Fault;
            }
            return SocketError.Success;
        }

        public async override Task CloseAsync()
        {
            await base.CloseAsync();
            if (_socket != null)
            {
                try
                { 
                    _socket.Dispose();
                }
                catch (ObjectDisposedException) { }
                _socket = null;
            }

        }

        private async Task StartReceive()
        {
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(_socket.ReceiveBufferSize * 2);
            var receiveOffset = 0;
            var bufferOffset = 0;
            var span = new Memory<byte>(receiveBuffer);
            var useAsync = (_configuration as ClientSocketConfiguration).Async;
            try
            {
                while (true)
                {
                    try
                    {
                        var buffer = new ArraySegment<byte>(receiveBuffer, receiveOffset, _socket.ReceiveBufferSize);
                        
                        var received = await _socket.ReceiveAsync(buffer, SocketFlags.Partial);
                        
                        if (received == 0)
                            return;

                        var toProcess = received + (receiveOffset - bufferOffset);
                        var processed = 0;
                        do
                        {
                            var off = bufferOffset + processed;
                            var length = toProcess - processed;
                            var slice = span.Slice(off, length);
                            var proc = await ProcessData(slice);
                            if(proc == 0)
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
                    catch(Exception){}
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                await HandleSocketDown();
            }

        }

        private bool IsReallyConnected()
        {
            var blocking = true;

            try
            {
                blocking = _socket.Blocking;
                _socket.Blocking = false;
                _socket.Send(new byte[0], 0, 0);
            }
            catch (SocketException se)
            {
                // 10035 == WSAEWOULDBLOCK
                if (!se.SocketErrorCode.Equals(10035))
                    throw;   //Throw the Exception for handling in OnConnectedToServer
            }

            //restore blocking mode
            _socket.Blocking = blocking;
            return true;
        }

    }
}
