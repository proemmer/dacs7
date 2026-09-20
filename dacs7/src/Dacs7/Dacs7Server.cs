// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Communication;
using Dacs7.Communication.Socket;
using Dacs7.DataProvider;
using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{

    public sealed partial class Dacs7Server
    {
        private Dictionary<string, ReadItem> _registeredTags = new();
        private Dacs7ConnectionState _state = Dacs7ConnectionState.Closed;
        private readonly ILogger? _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IPlcDataProvider _provider;
        private readonly List<ProtocolHandler> _handler = new();
        private readonly object _handlerLock = new();
        private ServerSocketConfiguration _serverConfig;

        internal ProtocolHandler ProtocolHandler { get; private set; }
        internal Dictionary<string, ReadItem> RegisteredTags => _registeredTags;
        internal SiemensPlcProtocolContext S7Context { get; private set; }

        /// <summary>
        /// The default address the server listens on, if no other address is given.
        /// </summary>
        public const string DefaultBindAddress = "127.0.0.1";

        /// <summary>
        /// The local address the server listens on.
        /// </summary>
        public string BindAddress { get; }

        /// <summary>
        /// The port the server listens on.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// True if the connection is fully applied
        /// </summary>
        public bool IsConnected => ProtocolHandler != null && ProtocolHandler?.ConnectionState == ConnectionState.Opened;

        /// <summary>
        /// True while the server socket is bound and accepting clients.
        /// Use this and not <see cref="IsConnected"/> to check if the server was started,
        /// because a listening server has no plc connection of its own.
        /// </summary>
        public bool IsListening => ProtocolHandler?.IsTransportConnected == true;

        /// <summary>
        /// Maximum number of clients connected at the same time. 0 (the default) means unlimited.
        /// Further connections are closed directly after they were accepted.
        /// </summary>
        public int MaxConnections
        {
            get => _serverConfig.MaxConnections;
            set
            {
                if (_state == Dacs7ConnectionState.Closed)
                {
                    _serverConfig.MaxConnections = value;
                }
                else
                {
                    ThrowHelper.ThrowCouldNotChangeValueWhileConnectionIsOpen(nameof(MaxConnections));
                }
            }
        }

        /// <summary>
        /// Enables tcp keep alive on accepted connections, so connections of clients which vanished
        /// without closing them (e.g. a broken network link) are detected. Disabled by default.
        /// </summary>
        public bool KeepAlive
        {
            get => _serverConfig.KeepAlive;
            set
            {
                if (_state == Dacs7ConnectionState.Closed)
                {
                    _serverConfig.KeepAlive = value;
                }
                else
                {
                    ThrowHelper.ThrowCouldNotChangeValueWhileConnectionIsOpen(nameof(KeepAlive));
                }
            }
        }

        /// <summary>
        /// Maximum Jobs calling
        /// </summary>
        public ushort MaxAmQCalling
        {
            get => S7Context.MaxAmQCalling;
            set
            {
                if (_state == Dacs7ConnectionState.Closed)
                {
                    S7Context.MaxAmQCalling = value;
                }
                else
                {
                    ThrowHelper.ThrowCouldNotChangeValueWhileConnectionIsOpen(nameof(MaxAmQCalling));
                }
            }
        }

        /// <summary>
        /// Maximum Jos waiting for response
        /// </summary>
        public ushort MaxAmQCalled
        {
            get => S7Context.MaxAmQCalled;
            set
            {
                if (_state == Dacs7ConnectionState.Closed)
                {
                    S7Context.MaxAmQCalled = value;
                }
                else
                {
                    ThrowHelper.ThrowCouldNotChangeValueWhileConnectionIsOpen(nameof(MaxAmQCalled));
                }
            }
        }

        /// <summary>
        /// The negotiated pdu size.
        /// </summary>
        public ushort PduSize
        {
            get => S7Context.PduSize;
            set
            {
                if (_state == Dacs7ConnectionState.Closed)
                {
                    S7Context.PduSize = value;
                }
                else
                {
                    ThrowHelper.ThrowCouldNotChangeValueWhileConnectionIsOpen(nameof(PduSize));
                }
            }
        }

        /// <summary>
        /// Register to the connection state events
        /// </summary>
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;


        /// <summary>
        /// Constructor of Dacs7Server. The server only listens on the loopback interface (127.0.0.1),
        /// so only clients on the same machine can connect.
        /// Use <see cref="Dacs7Server(string, int, IPlcDataProvider, ILoggerFactory)"/> to listen on the network.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="provider">The <see cref="IPlcDataProvider"/> which provides the data of the simulated plc.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        public Dacs7Server(int port, IPlcDataProvider provider, ILoggerFactory loggerFactory = null)
            : this(DefaultBindAddress, port, provider, loggerFactory)
        {
        }

        /// <summary>
        /// Constructor of Dacs7Server with a configurable bind address.
        /// </summary>
        /// <param name="bindAddress">
        /// The local address to listen on. This is an ip address (e.g. 127.0.0.1, 192.168.0.10, ::1), a hostname,
        /// or one of the wildcards "*" and "any" to listen on every interface of the machine.
        /// The server is not authenticated, so everyone who can reach the given address can read and write
        /// the simulated data blocks. Therefore only leave the loopback address if this is intended.
        /// </param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="provider">The <see cref="IPlcDataProvider"/> which provides the data of the simulated plc.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        public Dacs7Server(string bindAddress, int port, IPlcDataProvider provider, ILoggerFactory loggerFactory = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _logger = loggerFactory?.CreateLogger<Dacs7Client>();
            S7Context = new SiemensPlcProtocolContext();
            BindAddress = string.IsNullOrWhiteSpace(bindAddress) ? DefaultBindAddress : bindAddress.Trim();
            Port = port;
            ProtocolHandler = new ProtocolHandler(InitializeTransport(BindAddress, port), S7Context, UpdateConnectionState, loggerFactory, NewSocketConnected);
            _loggerFactory = loggerFactory;
            _provider = provider;
        }

        /// <summary>
        /// Connect to the plc
        /// </summary>
        /// <returns></returns>
        public Task ConnectAsync()
        {
            return ProtocolHandler?.OpenAsync();
        }

        /// <summary>
        /// Disconnect from the plc
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            // Close the client connections before the listener, because closing the listener
            // closes the accepted sockets underneath the client connections.
            await CloseClientHandlersAsync().ConfigureAwait(false);

            if (ProtocolHandler != null)
            {
                await ProtocolHandler.CloseAsync().ConfigureAwait(false);
            }

            // clients accepted while closing
            await CloseClientHandlersAsync().ConfigureAwait(false);
        }

        private async Task CloseClientHandlersAsync()
        {
            // CloseAsync removes the handler from _handler via ClientConnectionStateChanged, so work on a snapshot.
            foreach (ProtocolHandler item in TakeAllHandlers())
            {
                if (item != null)
                {
                    await item.CloseAsync().ConfigureAwait(false);
                    item.Dispose();
                }
            }
        }

        /// <summary>
        /// Dispose the ressources
        /// </summary>
        public void Dispose()
        {
            ProtocolHandler?.Dispose();

            foreach (ProtocolHandler item in TakeAllHandlers())
            {
                item.Dispose();
            }
        }

        private List<ProtocolHandler> TakeAllHandlers()
        {
            lock (_handlerLock)
            {
                List<ProtocolHandler> handlers = new(_handler);
                _handler.Clear();
                return handlers;
            }
        }

        /// <summary>
        /// Create a readitem for the given tag or reuse an existing one for this tag.
        /// </summary>
        /// <param name="tag">the absolute adress</param>
        /// <returns></returns>
        internal ReadItem RegisteredOrGiven(string tag)
        {
            return _registeredTags.TryGetValue(tag, out ReadItem nodeId) ? nodeId : ReadItem.CreateFromTag(tag);
        }

        /// <summary>
        /// Updates the ReadItem registration (add and/or remove items)
        /// </summary>
        /// <param name="toAdd">add this registrations</param>
        /// <param name="toRemove">remove this registrations</param>
        internal void UpdateRegistration(List<KeyValuePair<string, ReadItem>> toAdd, List<KeyValuePair<string, ReadItem>> toRemove)
        {
            Dictionary<string, ReadItem> origin;
            Dictionary<string, ReadItem> newDict;
            do
            {
                origin = _registeredTags;
                IEnumerable<KeyValuePair<string, ReadItem>> tmp = origin;
                if (toAdd != null)
                {
                    tmp = tmp.Union(toAdd);
                }

                if (toRemove != null)
                {
                    tmp = tmp.Except(toRemove);
                }

                newDict = tmp.ToDictionary(pair => pair.Key, pair => pair.Value);
            } while (Interlocked.CompareExchange(ref _registeredTags, newDict, origin) != origin);
        }

        /// <summary>
        /// Updates the internal connection state, and publish a change to <see cref="ConnectionStateChanged"/>
        /// </summary>
        /// <param name="state">The new connection state</param>
        private void UpdateConnectionState(ProtocolHandler handler, ConnectionState state)
        {
            Dacs7ConnectionState dacs7State = Dacs7ConnectionState.Closed;
            switch (state)
            {
                case ConnectionState.Closed: dacs7State = Dacs7ConnectionState.Closed; break;
                case ConnectionState.PendingOpenTransport: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.TransportOpened: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.PendingOpenPlc: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.Opened: dacs7State = Dacs7ConnectionState.Opened; break;
            }
            if (_state != dacs7State)
            {
                _state = dacs7State;
                //ConnectionStateChanged?.Invoke(this, dacs7State);
            }
        }


        private void NewSocketConnected(Socket clientSocket)
        {
            ClientSocketConfiguration config = ClientSocketConfiguration.FromSocket(clientSocket);
            SiemensPlcProtocolContext s7Context = new()
            {
                Timeout = S7Context.Timeout,
                PduSize = S7Context.PduSize,
                MaxAmQCalling = S7Context.MaxAmQCalling,
                MaxAmQCalled = S7Context.MaxAmQCalled
            };
            TcpTransport transport = new(
                            new Rfc1006ProtocolContext
                            {
                            },
                            config,
                            clientSocket
                        );
            ProtocolHandler handler = new(transport, s7Context, ClientConnectionStateChanged, _loggerFactory, null, _provider);
            int count;
            lock (_handlerLock)
            {
                _handler.Add(handler);
                count = _handler.Count;
            }
            _logger?.LogInformation("New client was connected to server, total connection is {connections}", count);
        }

        private void ClientConnectionStateChanged(ProtocolHandler handler, ConnectionState state)
        {
            if (state == ConnectionState.Closed)
            {
                bool removed;
                int count;
                lock (_handlerLock)
                {
                    removed = _handler.Remove(handler);
                    count = _handler.Count;
                }

                if (removed)
                {
                    handler.Dispose();
                    _logger?.LogInformation("Client was disconnected from server, total connection is {connections}", count);
                }
            }
        }

        private TcpTransport InitializeTransport(string bindAddress, int port)
        {
            _logger?.LogDebug("Start configuring dacs7 with Socket interface");
            _serverConfig = new ServerSocketConfiguration
            {
                Hostname = bindAddress,
                ServiceName = port
            };
            TcpTransport transport = new(
                new Rfc1006ProtocolContext
                {
                    //DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType, rack, slot),
                },
                _serverConfig
            );
            _logger?.LogDebug("Transport-Configuration: {0}.", transport.Configuration);
            return transport;
        }
    }
}
