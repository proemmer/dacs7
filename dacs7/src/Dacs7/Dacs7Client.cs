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


        public bool IsConnected => _protocolHandler != null && _protocolHandler?.ConnectionState == ConnectionState.Opened;

        public ushort PduSize => _s7Context != null ? _s7Context.PduSize : (ushort)0;

        public ushort ReadItemMaxLength => _s7Context != null ? _s7Context.ReadItemMaxLength : (ushort)0;

        public ushort WriteItemMaxLength => _s7Context != null ? _s7Context.PduSize : (ushort)0;


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

        private List<ReadItemSpecification> CreateNodeIdCollection(IEnumerable<string> values)
        {
            return new List<ReadItemSpecification>(values.Select(item => RegisteredOrGiven(item)));
        }

        private List<WriteItemSpecification> CreateWriteNodeIdCollection(IEnumerable<KeyValuePair<string, object>> values)
        {
            return new List<WriteItemSpecification>(values.Select(item =>
            {
                var result = RegisteredOrGiven(item.Key).Clone();
                result.Data = ConvertDataToMemory(result, item.Value);
                return result;
            }));
        }

        private ReadItemSpecification RegisteredOrGiven(string tag)
        {
            if (_registeredTags.TryGetValue(tag, out var nodeId))
            {
                return nodeId;
            }
            return Create(tag);
        }




        private static ReadItemSpecification Create(string tag)
        {
            var parts = tag.Split(new[] { ',' });
            var start = parts[0].Split(new[] { '.' });
            var withPrefix = start.Length == 3;
            PlcArea selector = 0;
            ushort length = 1;
            ushort offset = UInt16.Parse(start.Last());
            ushort db = 0;

            // TODO: support othe mnemonics
            switch (start[withPrefix ? 1 : 0].ToUpper())
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

            if (parts.Length > 2)
            {
                length = UInt16.Parse(parts[2]);
            }

            Type vtype = typeof(object);
            Type rType = typeof(object);
            switch (parts[1].ToLower())
            {
                case "b":
                    vtype = typeof(byte);
                    rType = length > 1 ? typeof(byte[]) : vtype;
                    break;
                case "c":
                    vtype = typeof(char);
                    rType = length > 1 ? typeof(char[]) : vtype;
                    break;
                case "w":
                    vtype = typeof(UInt16);
                    rType = length > 1 ? typeof(UInt16[]) : vtype;
                    break;
                case "dw":
                    vtype = typeof(UInt32);
                    rType = length > 1 ? typeof(UInt32[]) : vtype;
                    break;
                case "i":
                    vtype = typeof(Int16);
                    rType = length > 1 ? typeof(Int16[]) : vtype;
                    break;
                case "di":
                    vtype = typeof(Int32);
                    rType = length > 1 ? typeof(Int32[]) : vtype;
                    break;
                case "r":
                    vtype = typeof(Single);
                    rType = length > 1 ? typeof(Single[]) : vtype;
                    break;
                case "s":
                    vtype = typeof(string);
                    rType = length > 1 ? typeof(string[]) : vtype;
                    break;
                case var s when Regex.IsMatch(s, "^x\\d+$"):
                    vtype = typeof(bool);
                    rType = length > 1 ? typeof(bool[]) : vtype;
                    offset = (ushort)((offset * 8) + UInt16.Parse(s.Substring(1)));
                    break;
            }



            return new ReadItemSpecification
            {
                Area = selector,
                DbNumber = db,
                Offset = offset,
                Length = length,
                VarType = vtype,
                ResultType = rType
            };
        }

        private static string FromArea(PlcArea area)
        {
            // TODO: support othe mnemonics
            switch (area)
            {
                case PlcArea.IB: return "I";
                case PlcArea.FB: return "M";
                case PlcArea.QB: return "A";
                case PlcArea.TM: return "T";
                case PlcArea.CT: return "C";
            }
            throw new InvalidOperationException();
        }

        private static string CalculateByteArrayTag(string area, int offset, int length)
        {
            return $"{area}.{offset},b,{length}";
        }

        private static Memory<byte> ConvertDataToMemory(WriteItemSpecification item, object data)
        {
            if (data is string && item.ResultType != typeof(string))
                data = Convert.ChangeType(data, item.ResultType);

            switch (data)
            {
                case byte b:
                    return new byte[] { b };
                case byte[] ba:
                    return ba;
                case Memory<byte> ba:
                    return ba;
                case char c:
                    return new byte[] { Convert.ToByte(c) };
                case char[] ca:
                    return ca.Select(x => Convert.ToByte(x)).ToArray();
                case string s:
                    {
                        Memory<byte> result = new byte[s.Length + 2];
                        result.Span[0] = (byte)s.Length;
                        result.Span[1] = (byte)s.Length;
                        Encoding.ASCII.GetBytes(s).AsSpan().CopyTo(result.Span.Slice(2));
                        return result;
                    }
                case Int16 i16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteInt16BigEndian(result.Span, i16);
                        return result;
                    }
                case UInt16 ui16:
                    {
                        Memory<byte> result = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(result.Span, ui16);
                        return result;
                    }
                case Int32 i32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteInt32BigEndian(result.Span, i32);
                        return result;
                    }
                case UInt32 ui32:
                    {
                        Memory<byte> result = new byte[4];
                        BinaryPrimitives.WriteUInt32BigEndian(result.Span, ui32);
                        return result;
                    }
                case Int64 i64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteInt64BigEndian(result.Span, i64);
                        return result;
                    }
                case UInt64 ui64:
                    {
                        Memory<byte> result = new byte[8];
                        BinaryPrimitives.WriteUInt64BigEndian(result.Span, ui64);
                        return result;
                    }
            }
            throw new InvalidCastException();
        }

        private static object ConvertMemoryToData(ReadItemSpecification item, Memory<byte> data)
        {

            if (item.ResultType == typeof(byte))
            {
                return data.Span[0];
            }
            else if (item.ResultType == typeof(byte[]) || item.ResultType == typeof(Memory<byte>))
            {
                return data;
            }
            else if (item.ResultType == typeof(char))
            {
                return Convert.ToChar(data.Span[0]);
            }
            else if (item.ResultType == typeof(char[]) || item.ResultType == typeof(Memory<char>))
            {
                return null; // TODO
            }
            else if (item.ResultType == typeof(string))
            {
                var length = data.Span[1];
                return Encoding.ASCII.GetString(data.Span.Slice(2, length).ToArray());
            }
            else if (item.ResultType == typeof(Int16))
            {
                return BinaryPrimitives.ReadInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt16))
            {
                return BinaryPrimitives.ReadUInt16BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(Int32))
            {
                return BinaryPrimitives.ReadInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt32))
            {
                return BinaryPrimitives.ReadUInt32BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(Int64))
            {
                return BinaryPrimitives.ReadInt64BigEndian(data.Span);
            }
            else if (item.ResultType == typeof(UInt64))
            {
                return BinaryPrimitives.ReadUInt64BigEndian(data.Span);
            }
            throw new InvalidCastException();
        }

    }
}