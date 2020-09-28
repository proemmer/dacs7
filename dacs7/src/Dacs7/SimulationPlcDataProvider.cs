using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7
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

                if ((item.Offset + item.NumberOfItems) > dataEntry.Length)
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.OutOfRange));
                    continue;
                }

                Memory<byte> data = new byte[item.NumberOfItems];
                dataEntry.Data.Slice(item.Offset, item.NumberOfItems).CopyTo(data);
                result.Add(new ReadResultItem(item, ItemResponseRetValue.Success, data));
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

                if ((item.Offset + item.NumberOfItems) > dataEntry.Length)
                {
                    result.Add(new WriteResultItem(item, ItemResponseRetValue.OutOfRange));
                    continue;
                }

                item.Data.Slice(0, item.NumberOfItems).CopyTo(dataEntry.Data.Slice(item.Offset, item.NumberOfItems));
                result.Add(new WriteResultItem(item, ItemResponseRetValue.Success));
            }
            return Task.FromResult(result);
        }

    }
}
