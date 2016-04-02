using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;

namespace InacS7Core.Communication
{
    public class ClientSocketConfiguration : ISocketConfiguration
    {
        public string Hostname { get; set; }
        public int ServiceName { get; set; }
        public int ReceiveBufferSize { get; set; }
        public bool Autoconnect { get; set; }
        public string NetworkAdapter { get; set; }
        public bool KeepAlive { get; set; }

        public ClientSocketConfiguration()
        {
            NetworkAdapter = null;
            Hostname = "localhost";
            ServiceName = 22112;
            ReceiveBufferSize = 65536;  // buffer size to use for each socket I/O operation 
            Autoconnect = true;
            KeepAlive = false;
        }

        public static ClientSocketConfiguration FromSocket(Socket socket)
        {
            var ep = socket.RemoteEndPoint as IPEndPoint;
            var keepAlive = socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive);
            return new ClientSocketConfiguration
            {
                Hostname = ep.Address.ToString(),
                ServiceName = ep.Port,
                ReceiveBufferSize = socket.ReceiveBufferSize,  // buffer size to use for each socket I/O operation 
                KeepAlive = keepAlive != null
            };
        }
    }

    internal class ClientSocket : SocketBase
    {
        #region Fields
        private Socket _socket;
        #endregion

        #region Properties
        public override string Identity { get
            {
                if (_socket != null)
                {
                    var epLocal = _socket.LocalEndPoint as IPEndPoint;
                    var epRemote = _socket.RemoteEndPoint as IPEndPoint;
                    return _identity ?? (_identity = string.Format("{0}:{1}-{2}:{3}",
                        epLocal.Address.ToString(),
                        epLocal.Port,
                        epRemote.Address.ToString(),
                        epRemote.Port));
                }
                return string.Empty;
            }
        }
        public DateTime LastUsage { get; private set; }
        #endregion

        #region Ctor
        public ClientSocket(ClientSocketConfiguration configuration) : base(configuration)
        {
            Init();
        }
        internal ClientSocket(Socket socket,
            OnConnectionStateChangedHandler connectionStateChanged,
            OnSocketShutdownHandler shutdown,
            OnSendFinishedHandler sendfinished,
            OnDataReceivedHandler dataReceive
            ) : base(ClientSocketConfiguration.FromSocket(socket))
        {
            if (socket == null)
                throw new ArgumentNullException("socket");
            _socket = socket;
            OnConnectionStateChanged += connectionStateChanged;
            OnSocketShutdown += shutdown;
            OnSendFinished += sendfinished;
            OnRawDataReceived += dataReceive;
            Task.Factory.StartNew(() =>
            {
                PublishConnectionStateChanged(true);
                StartReceive();
            }, TaskCreationOptions.LongRunning);
        }
        #endregion

        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        /// </summary>
        public override bool Open()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.ConnectAsync(_configuration.Hostname, _configuration.ServiceName).Wait();
                if (IsReallyConnected())
                {
                    if (!_configuration.KeepAlive)
                        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    var ignore = Task.Factory.StartNew(() => StartReceive(), TaskCreationOptions.LongRunning);
                    PublishConnectionStateChanged(true);
                }
                else
                    HandleSocketDown();
            }
            catch(Exception)
            {
                HandleSocketDown();
                return false;
            }
            return true;
        }
        public override async Task<SocketError> Send(IEnumerable<byte> data)
        {
            LastUsage = DateTime.Now;
            var result = await SendInternal(data);
            PublishSendFinished();
            return result;
        }
        public override void Close()
        {
            base.Close();
            if (_socket != null)
            {
                try
                {
                    _socket.Dispose();
                }
                catch(ObjectDisposedException) { }
                _socket = null;
            }
        }
        private async void StartReceive()
        {
            try
            {
                byte[] receiveBuffer = new byte[_socket.ReceiveBufferSize];
                while (true)
                {
                    var buffer = new ArraySegment<byte>(receiveBuffer);
                    var received = await _socket.ReceiveAsync(buffer, SocketFlags.Partial);
                    if (received == 0)
                        return;
                    LastUsage = DateTime.Now;
                    PublishDataReceived(buffer.Take(received));
                }

            }
            catch (Exception exception)
            {
                //TODO
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                //if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                //{
                //    throw;
                //}
            }
            finally
            {
                HandleSocketDown();
            }

        }
        protected async Task<SocketError> SendInternal(IEnumerable<byte> data)
        {
            // Write the locally buffered data to the network.
            try
            {
                var result = await _socket.SendAsync(new ArraySegment<byte>(data.ToArray()), SocketFlags.None);
            }
            catch (Exception exception)
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
