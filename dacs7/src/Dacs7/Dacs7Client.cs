﻿using Dacs7.Communication;
using Dacs7.Communication.S7Online;
using Dacs7.Communication.Socket;
using Dacs7.Domain;
using Dacs7.Protocols;
using Dacs7.Protocols.Fdl;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{

    public delegate void ConnectionStateChangedEventHandler(Dacs7Client session, Dacs7ConnectionState e);


    public partial class Dacs7Client
    {
        private Dictionary<string, ReadItem> _registeredTags = new Dictionary<string, ReadItem>();
        private SiemensPlcProtocolContext _s7Context;
        private Dacs7ConnectionState _state = Dacs7ConnectionState.Closed;
        private ILogger _logger;

        internal ProtocolHandler ProtocolHandler { get; private set; }
        internal Dictionary<string, ReadItem> RegisteredTags => _registeredTags;

        /// <summary>
        /// True if the connection is fully applied
        /// </summary>
        public bool IsConnected => ProtocolHandler != null && ProtocolHandler?.ConnectionState == ConnectionState.Opened;

        /// <summary>
        /// Maximum Jobs calling
        /// </summary>
        public ushort MaxAmQCalling
        {
            get => _s7Context != null ? _s7Context.MaxAmQCalling : (ushort)0;
            set
            {
                if(_s7Context != null && _state == Dacs7ConnectionState.Closed)
                {
                    _s7Context.MaxAmQCalling = value;
                }
                else
                {
                    throw new InvalidOperationException($"Value of {nameof(MaxAmQCalling)} can only be changed while connection is closed!");
                }
            }
        }

        /// <summary>
        /// Maximum Jos waiting for response
        /// </summary>
        public ushort MaxAmQCalled
        {
            get => _s7Context != null ? _s7Context.MaxAmQCalled : (ushort)0;
            set
            {
                if (_s7Context != null && _state == Dacs7ConnectionState.Closed)
                {
                    _s7Context.MaxAmQCalled = value;
                }
                else
                {
                    throw new InvalidOperationException($"Value of {nameof(MaxAmQCalled)} can only be changed while connection is closed!");
                }
            }
        }

        /// <summary>
        /// The negotiated pdu size.
        /// </summary>
        public ushort PduSize
        {
            get => _s7Context != null ? _s7Context.PduSize : (ushort)0;
            set
            {
                if (_s7Context != null && _state == Dacs7ConnectionState.Closed)
                {
                    _s7Context.PduSize = value;
                }
                else
                {
                    throw new InvalidOperationException($"Value of {nameof(PduSize)} can only be changed while connection is closed!");
                }
            }
        }

        /// <summary>
        /// The maximum read item length of a single telegram.
        /// </summary>
        public ushort ReadItemMaxLength => _s7Context != null ? _s7Context.ReadItemMaxLength : (ushort)0;

        /// <summary>
        /// The maximum write item length of a single telegram.
        /// </summary>
        public ushort WriteItemMaxLength => _s7Context != null ? _s7Context.PduSize : (ushort)0;

        /// <summary>
        /// Register to the connection state events
        /// </summary>
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;


        /// <summary>
        /// Constructor of Dacs7Client
        /// </summary>
        /// <param name="address">The address of the plc  [IP or Hostname]:[Rack],[Slot]  where as rack and slot ar optional  default is Rack = 0, Slot = 2</param>
        /// <param name="connectionType">The <see cref="PlcConnectionType"/> for the connection.</param>
        public Dacs7Client(string address, PlcConnectionType connectionType = PlcConnectionType.Pg, int timeout = 5000, ILoggerFactory loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<Dacs7Client>();
            Transport transport; ;
            if (address.StartsWith("S7ONLINE", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger?.LogDebug("Start configuring dacs7 with S7Online interface");
                // This is currently a not working interface!!!!
                var addressPort = address.Substring(9).Split(':');
                var portRackSlot = addressPort.Length > 1 ?
                            addressPort[1].Split(',').Select(x => Int32.Parse(x)).ToArray() :
                            new int[] { 0, 2 };
                if (!IPAddress.TryParse(addressPort[0], out var ipaddress))
                {
                    ipaddress = IPAddress.Loopback;
                }

                transport = new S7OnlineTransport(new FdlProtocolContext
                {
                    Address = ipaddress,
                    ConnectionType = connectionType,
                    Rack = portRackSlot.Length > 0 ? portRackSlot[0] : 0,
                    Slot = portRackSlot.Length > 1 ? portRackSlot[1] : 2
                }, new S7OnlineConfiguration());
                _logger?.LogDebug("S7Online interface configured.");
            }
            else
            {
                _logger?.LogDebug("Start configuring dacs7 with Socket interface");
                var addressPort = address.Split(':');
                var portRackSlot = addressPort.Length > 1 ?
                                            addressPort[1].Split(',').Select(x => Int32.Parse(x)).ToArray() :
                                            new int[] { 102, 0, 2 };

                var rack = portRackSlot.Length > 1 ? portRackSlot[1] : 0;
                var slot = portRackSlot.Length > 2 ? portRackSlot[2] : 2;
                transport = new TcpTransport(new Rfc1006ProtocolContext
                {
                    DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType,
                                                                     rack,
                                                                     slot),
                }, new ClientSocketConfiguration
                {
                    Hostname = addressPort[0],
                    ServiceName = portRackSlot.Length > 0 ? portRackSlot[0] : 102
                });
                _logger?.LogDebug("Transport-Configuration: {0}.", transport.Configuration);
                _logger?.LogDebug("Rfc1006 Configuration: connectionType={0}; Rack={1}; Slot={2}", Enum.GetName(typeof(PlcConnectionType), connectionType), rack, slot);
                _logger?.LogDebug("Socket interface configured.");
            }

            _s7Context = new SiemensPlcProtocolContext
            {
                Timeout = timeout
            };

            ProtocolHandler = new ProtocolHandler(transport, _s7Context, UpdateConnectionState, loggerFactory);

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

        internal ReadItem RegisteredOrGiven(string tag)
        {
            if (_registeredTags.TryGetValue(tag, out var nodeId))
            {
                return nodeId;
            }
            return ReadItem.CreateFromTag(tag);
        }

        internal void UpdateRegistration(List<KeyValuePair<string, ReadItem>> toAdd, List<KeyValuePair<string, ReadItem>> toRemove)
        {
            Dictionary<string, ReadItem> origin;
            Dictionary<string, ReadItem> newDict;
            do
            {
                origin = _registeredTags;
                var tmp = origin as IEnumerable<KeyValuePair<string, ReadItem>>;
                if(toAdd != null) tmp = tmp.Union(toAdd);
                if (toRemove != null) tmp = tmp.Except(toRemove);
                newDict = tmp.ToDictionary(pair => pair.Key, pair => pair.Value);
            } while (Interlocked.CompareExchange(ref _registeredTags, newDict, origin) != origin);
        }
    }
}