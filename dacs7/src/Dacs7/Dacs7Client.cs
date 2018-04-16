using Dacs7.Communication;
using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{

    public delegate void ConnectionStateChangedEventHandler(Dacs7Client session, Dacs7ConnectionState e);


    public partial class Dacs7Client
    {
        private Dictionary<string, ReadItemSpecification> _registeredTags = new Dictionary<string, ReadItemSpecification>();
        private ClientSocketConfiguration _config;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private ProtocolHandler _protocolHandler;
        private Dacs7ConnectionState _state = Dacs7ConnectionState.Closed;

        /// <summary>
        /// True if the connection is fully applied
        /// </summary>
        public bool IsConnected => _protocolHandler != null && _protocolHandler?.ConnectionState == ConnectionState.Opened;

        /// <summary>
        /// The negotiated pdu size.
        /// </summary>
        public ushort PduSize => _s7Context != null ? _s7Context.PduSize : (ushort)0;

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



            _context = new Rfc1006ProtocolContext
            {
                DestTsap = Rfc1006ProtocolContext.CalcRemoteTsap((ushort)connectionType,
                                                                 portRackSlot.Length > 1 ? portRackSlot[1] : 0,
                                                                 portRackSlot.Length > 2 ? portRackSlot[2] : 2)
            };

            _s7Context = new SiemensPlcProtocolContext
            {

            };

            _protocolHandler = new ProtocolHandler(_config, _context, _s7Context, UpdateConnectionState);

        }


        /// <summary>
        /// Connect to the plc
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await _protocolHandler?.OpenAsync();
        }

        /// <summary>
        /// Disconnect from the plc
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            await _protocolHandler?.CloseAsync();
        }




        private void UpdateConnectionState(ConnectionState state)
        {
            var dacs7State = Dacs7ConnectionState.Closed;
            switch (state)
            {
                case ConnectionState.Closed: dacs7State = Dacs7ConnectionState.Closed; break;
                case ConnectionState.PendingOpenRfc1006: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.Rfc1006Opened: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.PendingOpenPlc: dacs7State = Dacs7ConnectionState.Connecting; break;
                case ConnectionState.Opened: dacs7State = Dacs7ConnectionState.Opened; break;
            }
            if (_state != dacs7State)
            {
                _state = dacs7State;
                ConnectionStateChanged?.Invoke(this, dacs7State);
            }
        }

        private ReadItemSpecification RegisteredOrGiven(string tag)
        {
            if (_registeredTags.TryGetValue(tag, out var nodeId))
            {
                return nodeId;
            }
            return ReadItemSpecification.CreateFromTag(tag);
        }

        private IEnumerable<ReadItemSpecification> CreateNodeIdCollection(IEnumerable<string> values)
        {
            return new List<ReadItemSpecification>(values.Select(item => RegisteredOrGiven(item)));
        }

        private IEnumerable<WriteItemSpecification> CreateWriteNodeIdCollection(IEnumerable<KeyValuePair<string, object>> values)
        {
            return new List<WriteItemSpecification>(values.Select(item =>
            {
                var result = RegisteredOrGiven(item.Key).Clone();
                result.Data = WriteItemSpecification.ConvertDataToMemory(result, item.Value);
                return result;
            }));
        }

        private void UpdateRegistration(List<KeyValuePair<string, ReadItemSpecification>> toAdd, List<KeyValuePair<string, ReadItemSpecification>> toRemove)
        {
            Dictionary<string, ReadItemSpecification> origin;
            Dictionary<string, ReadItemSpecification> newDict;
            do
            {
                origin = _registeredTags;
                var tmp = origin as IEnumerable<KeyValuePair<string, ReadItemSpecification>>;
                if(toAdd != null) tmp = tmp.Union(toAdd);
                if (toRemove != null) tmp = tmp.Except(toRemove);
                newDict = tmp.ToDictionary(pair => pair.Key, pair => pair.Value);
            } while (Interlocked.CompareExchange(ref _registeredTags, newDict, origin) != origin);
        }
    }
}