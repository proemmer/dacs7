// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Communication;
using Dacs7.Communication.Socket;
using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{

    public delegate void ConnectionStateChangedEventHandler(Dacs7Client session, Dacs7ConnectionState e);


    public sealed partial class Dacs7Client : IDisposable
    {
        private Dictionary<string, ReadItem> _registeredTags = new Dictionary<string, ReadItem>();
        private Dacs7ConnectionState _state = Dacs7ConnectionState.Closed;
        private readonly ILogger _logger;

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
        public Dacs7Client(string address, PlcConnectionType connectionType = PlcConnectionType.Pg, int timeout = 5000, ILoggerFactory loggerFactory = null, int autoReconnectTime = 5000)
        {
            _logger = loggerFactory?.CreateLogger<Dacs7Client>();
            S7Context = new SiemensPlcProtocolContext { Timeout = timeout };
            ProtocolHandler = new ProtocolHandler(InitializeTransport(address, connectionType, autoReconnectTime), S7Context, UpdateConnectionState, loggerFactory);
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
        public Task DisconnectAsync() => ProtocolHandler?.CloseAsync();

        /// <summary>
        /// Dispose the ressources
        /// </summary>
        public void Dispose() => ProtocolHandler?.Dispose();

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
                if (toAdd != null) tmp = tmp.Union(toAdd);
                if (toRemove != null) tmp = tmp.Except(toRemove);
                newDict = tmp.ToDictionary(pair => pair.Key, pair => pair.Value);
            } while (Interlocked.CompareExchange(ref _registeredTags, newDict, origin) != origin);
        }

        /// <summary>
        /// Updates the internal connection state, and publish a change to <see cref="ConnectionStateChanged"/>
        /// </summary>
        /// <param name="state">The new connection state</param>
        private void UpdateConnectionState(ConnectionState state)
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
                ConnectionStateChanged?.Invoke(this, dacs7State);
            }
        }

        private TcpTransport InitializeTransport(string address, PlcConnectionType connectionType, int autoReconnectTime)
        {
            _logger?.LogDebug("Start configuring dacs7 with Socket interface");
            ParseParametersFromAddress(address, out var host, out var port, out var rack, out var slot);
            var transport = new TcpTransport(
                new Rfc1006ProtocolContext
                {
                    DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType, rack, slot),
                },
                new ClientSocketConfiguration
                {
                    Hostname = host,
                    ServiceName = port,
                    AutoconnectTime = autoReconnectTime
                }
            );
            _logger?.LogDebug("Transport-Configuration: {0}.", transport.Configuration);
            _logger?.LogDebug("Rfc1006 Configuration: connectionType={0}; Rack={1}; Slot={2}", Enum.GetName(typeof(PlcConnectionType), connectionType), rack, slot);
            return transport;
        }

        private static void ParseParametersFromAddress(string address, out string host, out int port, out int rack, out int slot)
        {
            var addressPort = address.Split(':');
            var portRackSlot = addressPort.Length > 1 ?
                                        addressPort[1].Split(',').Select(x => int.Parse(x, NumberStyles.Integer, CultureInfo.InvariantCulture)).ToArray() :
                                        new int[] { 102, 0, 2 };
            host = addressPort[0];
            port = portRackSlot.Length > 0 ? portRackSlot[0] : 102;
            rack = portRackSlot.Length > 1 ? portRackSlot[1] : 0;
            slot = portRackSlot.Length > 2 ? portRackSlot[2] : 2;
        }


    }
}