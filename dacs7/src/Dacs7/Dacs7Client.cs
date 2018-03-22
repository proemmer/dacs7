using Dacs7.Communication;
using Dacs7.Protocols;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dacs7
{



    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public partial class Dacs7Client
    {
        private Dictionary<string, ReadItemSpecification> _registeredTags = new Dictionary<string, ReadItemSpecification>();
        private ClientSocketConfiguration _config;
        private Rfc1006ProtocolContext _context;
        private SiemensPlcProtocolContext _s7Context;
        private const int ReconnectPeriod = 10;
        
        private ProtocolHandler _protocolHandler;

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



        public async Task ConnectAsync()
        {
            await _protocolHandler.OpenAsync();
        }

        public async Task DisconnectAsync()
        {
            await _protocolHandler.CloseAsync();
        }

        public Task<IEnumerable<object>> ReadAsync(params string[] values) => ReadAsync(values as IEnumerable<string>);

        public async Task<IEnumerable<object>> ReadAsync(IEnumerable<string> values)
        {
            var items = CreateNodeIdCollection(values);
            var result =  await _protocolHandler.ReadAsync(items);

            //TODO Validation
            return items;
        }

        public Task WriteAsync(params KeyValuePair<string, object>[] values) => WriteAsync(values as IEnumerable<KeyValuePair<string, object>>);

        public Task WriteAsync(IEnumerable<KeyValuePair<string, object>> values)
        {
            throw new NotImplementedException();

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



        internal void AddRegisteredTag(IEnumerable<KeyValuePair<string, ReadItemSpecification>> tags)
        {
            foreach (var item in tags)
            {
                _registeredTags.Add(item.Key, item.Value);
            }

        }

        internal List<ReadItemSpecification> CreateNodeIdCollection(IEnumerable<string> values)
        {
            return new List<ReadItemSpecification>(values.Select(item => RegisteredOrGiven(item)));
        }

        internal ReadItemSpecification RegisteredOrGiven(string tag)
        {
            if (_registeredTags.TryGetValue(tag, out var nodeId))
            {
                return nodeId;
            }
            return Create(tag);
        }


        internal IEnumerable<string> RemoveRegisteredTag(IEnumerable<string> keys)
        {
            var result = new List<string>();
            foreach (var key in keys)
            {
                if (_registeredTags.Remove(key))
                {
                    result.Add(key);
                }
            }
            return result;
        }


        private ReadItemSpecification Create(string tag)
        {
            var parts = tag.Split(new[] { ',' });
            var start = parts[0].Split(new[] { '.' });
            PlcArea selector = 0;
            ushort length = 1;
            ushort offset = UInt16.Parse(start.Last());
            ushort db = 0;
            switch (start[1])
            {
                case "I": selector = PlcArea.IB; break;
                case "M": selector = PlcArea.FB; break;
                case "A": selector = PlcArea.QB; break;
                case "T": selector = PlcArea.TM; break;
                case "C": selector = PlcArea.CT; break;
                case var s when Regex.IsMatch(s, "^DB\\d+$"):
                    {
                        selector = PlcArea.DB;
                        db = UInt16.Parse(s.Substring(2));
                        break;
                    }
            }

            Type vtype = typeof(object);
            switch (parts[1].ToLower())
            {
                case "b":
                    vtype = typeof(byte);
                    break;
                case "c":
                    vtype = typeof(char);
                    break;
                case "w":
                    vtype = typeof(UInt16);
                    break;
                case "dw":
                    vtype = typeof(UInt32);
                    break;
                case "i":
                    vtype = typeof(Int16);
                    break;
                case "di":
                    vtype = typeof(Int32);
                    break;
                case "r":
                    vtype = typeof(Single);
                    break;
                case "s":
                    vtype = typeof(string);
                    break;
                case var s when Regex.IsMatch(s, "^x\\d+$"):
                    vtype = typeof(bool);
                    offset = (ushort)((offset * 8) + UInt16.Parse(s.Substring(1)));
                    break;
            }

            if (parts.Length > 2)
            {
                length = UInt16.Parse(parts[2]);
            }

            return new ReadItemSpecification
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                Length = length,
                VarType = vtype
            };
        }

    }
}