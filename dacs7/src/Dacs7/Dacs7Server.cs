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
        private Dictionary<string, ReadItem> _registeredTags = new Dictionary<string, ReadItem>();
        private Dacs7ConnectionState _state = Dacs7ConnectionState.Closed;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IPlcDataProvider _provider;
        private readonly List<ProtocolHandler> _handler = new List<ProtocolHandler>();

        internal ProtocolHandler ProtocolHandler { get; private set; }
        internal Dictionary<string, ReadItem> RegisteredTags => _registeredTags;
        internal SiemensPlcProtocolContext S7Context { get; private set; }

        /// <summary>
        /// True if the connection is fully applied
        /// </summary>
        public bool IsConnected => ProtocolHandler != null && ProtocolHandler?.ConnectionState == ConnectionState.Opened;

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
        /// Constructor of Dacs7Client
        /// </summary>
        /// <param name="address">The address of the plc  [IP or Hostname]:[Rack],[Slot]  where as rack and slot ar optional  default is Rack = 0, Slot = 2</param>
        /// <param name="connectionType">The <see cref="PlcConnectionType"/> for the connection.</param>
        public Dacs7Server(int port, IPlcDataProvider provider, ILoggerFactory loggerFactory = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _logger = loggerFactory?.CreateLogger<Dacs7Client>();
            S7Context = new SiemensPlcProtocolContext();
            ProtocolHandler = new ProtocolHandler(InitializeTransport(port), S7Context, UpdateConnectionState, loggerFactory, NewSocketConnected);
            _loggerFactory = loggerFactory;
            _provider = provider;
        }

        /// <summary>
        /// Connect to the plc
        /// </summary>
        /// <returns></returns>
        public Task ConnectAsync() => ProtocolHandler?.OpenAsync();

        /// <summary>
        /// Disconnect from the plc
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            if (ProtocolHandler != null) 
            {
                await ProtocolHandler.CloseAsync().ConfigureAwait(false);
            }


            foreach (var item in _handler)
            {
                await item.CloseAsync().ConfigureAwait(false);
                item.Dispose();
            }
            _handler.Clear();
        }

        /// <summary>
        /// Dispose the ressources
        /// </summary>
        public void Dispose()
        {
            ProtocolHandler?.Dispose();

            foreach (var item in _handler)
            {
                item.Dispose();
            }
            _handler.Clear();
        }

        /// <summary>
        /// Create a readitem for the given tag or reuse an existing one for this tag.
        /// </summary>
        /// <param name="tag">the absolute adress</param>
        /// <returns></returns>
        internal ReadItem RegisteredOrGiven(string tag)
            => _registeredTags.TryGetValue(tag, out var nodeId) ? nodeId : ReadItem.CreateFromTag(tag);

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
                var tmp = origin as IEnumerable<KeyValuePair<string, ReadItem>>;
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
            var dacs7State = Dacs7ConnectionState.Closed;
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
            var config = ClientSocketConfiguration.FromSocket(clientSocket);
            var s7Context = new SiemensPlcProtocolContext { Timeout = S7Context.Timeout, PduSize = S7Context.PduSize };
            var transport = new TcpTransport(
                            new Rfc1006ProtocolContext
                            {
                            },
                            config,
                            clientSocket
                        );
            var handler = new ProtocolHandler(transport, s7Context, ClientConnectionStateChanged, _loggerFactory, null, _provider);
            _handler.Add(handler);
            _logger.LogInformation("New client was connected to server, total connection is {connections}", _handler.Count);
        }

        private void ClientConnectionStateChanged(ProtocolHandler handler, ConnectionState state)
        {
            if(state == ConnectionState.Closed)
            {
                if (_handler.Remove(handler))
                {
                    handler.Dispose();
                    _logger.LogInformation("Client was disconnected from server, total connection is {connections}", _handler.Count);
                }
            }
        }

        private TcpTransport InitializeTransport(int port)
        {
            _logger?.LogDebug("Start configuring dacs7 with Socket interface");
            var transport = new TcpTransport(
                new Rfc1006ProtocolContext
                {
                    //DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType, rack, slot),
                },
                new ServerSocketConfiguration
                {
                    Hostname = "127.0.0.1",
                    ServiceName = port
                }
            );
            _logger?.LogDebug("Transport-Configuration: {0}.", transport.Configuration);
            return transport;
        }
    }
}
