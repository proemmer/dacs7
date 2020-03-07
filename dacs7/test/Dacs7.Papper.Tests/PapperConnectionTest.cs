using Customer.Data.DB_SpindlePos_BST1;
using Dacs7.ReadWrite;
using Papper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Papper.Tests
{
    public class PapperConnectionTest
    {
        private Dacs7Client _client;
        private PlcDataMapper _mapper;


        [Fact]
        public async Task TestMultiWrite()
        {
            _client = new Dacs7Client("192.168.1.60:102,0,1", PlcConnectionType.Basic, 5000);
            await _client.ConnectAsync();

            if (_client.IsConnected && (_mapper == null || _mapper.PduSize > _client.PduSize))
            {
                var pduSize = _client.PduSize;
                    _mapper = new PlcDataMapper(pduSize, Papper_OnRead,
                                                         Papper_OnWrite,
                                                         OptimizerType.Items);

                _mapper.AddMapping(typeof(DB_SpindlePos_BST1));
            }


            var data = await _mapper.ReadAsync(PlcReadReference.FromAddress("DB_SpindlePos_BST1.This"));
            await _mapper.WriteAsync(PlcWriteReference.FromAddress("DB_SpindlePos_BST1.This", data));


            await _client.DisconnectAsync();
        }

















        private static void SetPropertyInExpandoObject(dynamic parent, string address, object value) => SetPropertyInExpandoObject(parent, address.Replace("[", ".[").Split('.'), value);

        private static void SetPropertyInExpandoObject(dynamic parent, IEnumerable<string> parts, object value)
        {
            var key = parts.First();
            parts = parts.Skip(1);
            if (parent is IList<object> list)
            {
                var index = int.Parse(key.TrimStart('[').TrimEnd(']'), CultureInfo.InvariantCulture);
                if (parts.Any())
                {
                    SetPropertyInExpandoObject(list.ElementAt(index), parts, value);
                }
                else
                {
                    list[index] = value;
                }
            }
            else
            {
                if (parent is IDictionary<string, object> dictionary)
                {
                    if (parts.Any())
                    {
                        SetPropertyInExpandoObject(dictionary[key], parts, value);
                    }
                    else
                    {
                        dictionary[key] = value;
                    }
                }
            }
        }


        private static object GetPropertyInExpandoObject(dynamic parent, string address) => GetPropertyInExpandoObject(parent, address.Replace("[", ".[").Split('.'));

        private static object GetPropertyInExpandoObject(dynamic parent, IEnumerable<string> parts)
        {
            var key = parts.First();
            parts = parts.Skip(1);
            if (parent is IList<object> list)
            {
                var index = int.Parse(key.TrimStart('[').TrimEnd(']'), CultureInfo.InvariantCulture);
                if (parts.Any())
                {
                    return GetPropertyInExpandoObject(list.ElementAt(index), parts); // TODO
                }
                else
                {
                    return list.ElementAt(index);
                }
            }
            else
            {
                if (parent is IDictionary<string, object> dictionary)
                {
                    if (parts.Any())
                    {
                        return GetPropertyInExpandoObject(dictionary[key], parts);
                    }
                    else
                    {
                        return dictionary[key];
                    }
                }
            }
            return null;
        }








        private async Task Papper_OnRead(IEnumerable<DataPack> reads)
        {

            try
            {
                if (!reads.Any())
                {
                    return;
                }


                var readAddresses = reads.Select(w => ReadItem.Create<byte[]>(w.Selector, (ushort)w.Offset, (ushort)w.Length)).ToList();
                var results = await _client.ReadAsync(readAddresses).ConfigureAwait(false);


                reads.AsParallel().Select((item, index) =>
                {
                    if (results != null)
                    {
                        var result = results.ElementAt(index);
                        item.ApplyData(result.Data);
                        item.ExecutionResult = result.ReturnCode == ItemResponseRetValue.Success ? ExecutionResult.Ok : ExecutionResult.Error;
                    }
                    else
                    {
                        item.ExecutionResult = ExecutionResult.Error;
                    }
                    return true;
                }).ToList();
            }
            catch (Exception ex)
            {
                reads.AsParallel().Select((item, index) =>
                {
                    item.ExecutionResult = ExecutionResult.Error;
                    return true;
                }).ToList();
            }
        }

        private async Task Papper_OnWrite(IEnumerable<DataPack> writes)
        {

            try
            {


                var results = await _client.WriteAsync(writes.SelectMany(BuildWritePackages)).ConfigureAwait(false);



                writes.AsParallel().Select((item, index) =>
                {
                    if (results != null)
                    {
                        item.ExecutionResult = results.ElementAt(index) == ItemResponseRetValue.Success ? ExecutionResult.Ok : ExecutionResult.Error;
                    }
                    else
                    {
                        item.ExecutionResult = ExecutionResult.Error;
                    }
                    return true;
                }).ToList();
            }
            catch (Exception ex)
            {
                writes.AsParallel().Select((item, index) =>
                {
                    item.ExecutionResult = ExecutionResult.Error;
                    return true;
                }).ToList();
            }
        }


        private static IEnumerable<WriteItem> BuildWritePackages(DataPack w)
        {
            var result = new List<WriteItem>();
            if (!w.HasBitMask)
            {
                result.Add(WriteItem.Create(w.Selector, (ushort)w.Offset, w.Data));
            }
            else
            {
                if (w.Data.Length > 0)
                {
                    SetupBitMask(w, result, end: false);
                }

                if (w.Data.Length > 2)
                {
                    result.Add(WriteItem.Create(w.Selector, (ushort)(w.Offset + 1), w.Data.Slice(1, w.Data.Length - 2)));
                }

                if (w.Data.Length > 1)
                {
                    SetupBitMask(w, result, end: true);
                }
            }
            return result;
        }

        private static void SetupBitMask(DataPack w, List<WriteItem> result, bool end = false)
        {
            var bytePos = end ? w.Data.Length - 1 : 0;
            var bm = end ? w.BitMaskEnd : w.BitMaskBegin;
            var currentByte = w.Data.Span[bytePos];
            var currentOffset = (w.Offset + bytePos) * 8;
            for (var j = 0; j < 8; j++)
            {
                if (bm.GetBit(j))
                {
                    result.Add(WriteItem.Create(w.Selector, (ushort)(currentOffset + j), currentByte.GetBit(j)));
                    bm = bm.SetBit(j, false);
                    if (bm == 0)
                    {
                        break;
                    }
                }
            }
        }

    }
}
