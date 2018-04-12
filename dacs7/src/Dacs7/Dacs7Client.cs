using Dacs7.Communication;
using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {
        private Dictionary<string, ReadItemSpecification> _registeredTags = new Dictionary<string, ReadItemSpecification>();
        private ClientSocketConfiguration _config;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private ProtocolHandler _protocolHandler;

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

            _protocolHandler = new ProtocolHandler(_config, _context, _s7Context);


        }

        /// <summary>
        /// Connect to the plc
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await _protocolHandler.OpenAsync();
        }

        /// <summary>
        /// Disconnect from the plc
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            await _protocolHandler.CloseAsync();
        }



        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Returns the registered shortcuts</returns>
        public async Task<IEnumerable<string>> RegisterAsync(params string[] values) => await RegisterAsync(values as IEnumerable<string>);

        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> RegisterAsync(IEnumerable<string> values)
        {
            var added = new List<KeyValuePair<string, ReadItemSpecification>>();
            var enumerator = values.GetEnumerator();
            var resList = CreateNodeIdCollection(values).Select(x =>
            {
                enumerator.MoveNext();
                added.Add(new KeyValuePair<string, ReadItemSpecification>(enumerator.Current, x));
                return x.ToString();
            }).ToList();
            AddRegisteredTag(added);
            return await Task.FromResult(resList);
        }

        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> UnregisterAsync(params string[] values)
        {
            return await UnregisterAsync(values as IEnumerable<string>);
        }

        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> UnregisterAsync(IEnumerable<string> values)
        {
            var result = new List<string>();
            foreach (var item in values)
            {
                if (_registeredTags.Remove(item))
                    result.Add(item);
            }

            return await Task.FromResult(result);

        }

        /// <summary>
        /// Retruns true if the given tag is already registred
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool IsTagRegistered(string tag) => _registeredTags.ContainsKey(tag);


        private void AddRegisteredTag(IEnumerable<KeyValuePair<string, ReadItemSpecification>> tags)
        {
            foreach (var item in tags)
            {
                _registeredTags.Add(item.Key, item.Value);
            }
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

        private ReadItemSpecification RegisteredOrGiven(string tag)
        {
            if (_registeredTags.TryGetValue(tag, out var nodeId))
            {
                return nodeId;
            }
            return ReadItemSpecification.CreateFromTag(tag);
        }

        private static string CalculateByteArrayTag(string area, int offset, int length)
        {
            return $"{area}.{offset},b,{length}";
        }

    }
}