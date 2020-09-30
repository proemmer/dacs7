// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{
    public class SimulationPlcDataProvider : IPlcDataProvider
    {
        private static readonly Lazy<SimulationPlcDataProvider> _default = new Lazy<SimulationPlcDataProvider>(() => new SimulationPlcDataProvider());
        private readonly Dictionary<PlcArea, Dictionary<ushort, PlcDataEntry>> _plcData = new Dictionary<PlcArea, Dictionary<ushort, PlcDataEntry>>();


        public static SimulationPlcDataProvider Instance => _default.Value;

        public bool Register(PlcArea area, ushort dataLength, ushort dbNumber = 0) => Register(area, dataLength, default, dbNumber);

        public bool Register(PlcArea area, ushort dataLength, Memory<byte> data, ushort dbNumber = 0)
        {
            if (!_plcData.TryGetValue(area, out var areaData))
            {
                areaData = new Dictionary<ushort, PlcDataEntry>();
                _plcData.Add(area, areaData);
            }

            if (!areaData.TryGetValue(dbNumber, out var dataEntry))
            {
                dataEntry = new PlcDataEntry
                (
                    area: area,
                    dbNumber: dbNumber,
                    length: dataLength,
                    data: data
                );
                areaData.Add(dbNumber, dataEntry);
                return true;
            }
            return false;
        }
    
        public bool Release(PlcArea area, ushort dbNumber = 0)
        {
            if (!_plcData.TryGetValue(area, out var areaData))
            {
                return false;
            }

            if (!areaData.TryGetValue(dbNumber, out var dataEntry))
            {
                return false;
            }

            dataEntry?.Dispose();
            return true;
        }

        public void ReleaseAll()
        {
            // create a copy and clear the list, so we ensure currently active read and write calls will work and all following will fail.
            var copy = _plcData.Values.ToList();
            _plcData.Clear();
            foreach (var areaData in copy)
            {
                foreach (var dataEntry in areaData.Values)
                {
                    dataEntry?.Dispose();
                }
            }
        }
    

        public Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
        {
            var result = new List<ReadResultItem>();
            foreach (var item in readItems)
            {

                if (!_plcData.TryGetValue(item.Area, out var areaData))
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                if (!areaData.TryGetValue(item.DbNumber, out var dataEntry))
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                var size = item.NumberOfItems * item.ElementSize;
                if ((item.Offset + size) > dataEntry.Length)
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.OutOfRange));
                    continue;
                }

                if (item.TransportSize == DataTransportSize.Bit)
                {
                    var byteOffset = item.Offset / 8;
                    var bitNumber = item.Offset % 8;
                    Memory<byte> data = new byte[] { Converter.GetBit(dataEntry.Data.Span[byteOffset], bitNumber) ? 0x01 : 0x00};
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.Success, data));
                }
                else
                { 
                    Memory<byte> data = new byte[size];
                    dataEntry.Data.Slice(item.Offset, size).CopyTo(data);
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.Success, data));
                }
            }
            return Task.FromResult(result);
        }

        public Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
        {
            var result = new List<WriteResultItem>();
            foreach (var item in writeItems)
            {
                if (!_plcData.TryGetValue(item.Area, out var areaData))
                {
                    result.Add(new WriteResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                if (!areaData.TryGetValue(item.DbNumber, out var dataEntry))
                {
                    result.Add(new WriteResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                if (item.TransportSize == DataTransportSize.Bit)
                {
                    var byteOffset = item.Offset / 8;
                    var bitNumber = item.Offset % 8;
                    dataEntry.Data.Span[byteOffset] = Converter.SetBit(dataEntry.Data.Span[byteOffset], bitNumber, item.Data.Span[0] == 0x01);
                }
                else
                {
                    var size = item.NumberOfItems * item.ElementSize;
                    if (size > dataEntry.Length)
                    {
                        result.Add(new WriteResultItem(item, ItemResponseRetValue.OutOfRange));
                        continue;
                    }

                    item.Data.Slice(0, size).CopyTo(dataEntry.Data.Slice(item.Offset, size));
                }
                result.Add(new WriteResultItem(item, ItemResponseRetValue.Success));
            }
            return Task.FromResult(result);
        }

    }
}
