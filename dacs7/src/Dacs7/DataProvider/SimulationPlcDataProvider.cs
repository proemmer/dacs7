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
        private static readonly Lazy<SimulationPlcDataProvider> _default = new(() => new SimulationPlcDataProvider());
        private readonly Dictionary<PlcArea, Dictionary<ushort, PlcDataEntry>> _plcData = new();


        public static SimulationPlcDataProvider Instance => _default.Value;

        public bool Register(PlcArea area, ushort dataLength, ushort dbNumber = 0)
        {
            return Register(area, dataLength, default, dbNumber);
        }

        public bool Register(PlcArea area, ushort dataLength, Memory<byte> data, ushort dbNumber = 0)
        {
            if (!_plcData.TryGetValue(area, out Dictionary<ushort, PlcDataEntry> areaData))
            {
                areaData = new Dictionary<ushort, PlcDataEntry>();
                _plcData.Add(area, areaData);
            }

            if (!areaData.TryGetValue(dbNumber, out PlcDataEntry dataEntry))
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
            if (!_plcData.TryGetValue(area, out Dictionary<ushort, PlcDataEntry> areaData))
            {
                return false;
            }

            if (!areaData.TryGetValue(dbNumber, out PlcDataEntry dataEntry))
            {
                return false;
            }

            dataEntry?.Dispose();
            return true;
        }

        public void ReleaseAll()
        {
            // create a copy and clear the list, so we ensure currently active read and write calls will work and all following will fail.
            List<Dictionary<ushort, PlcDataEntry>> copy = _plcData.Values.ToList();
            _plcData.Clear();
            foreach (Dictionary<ushort, PlcDataEntry> areaData in copy)
            {
                foreach (PlcDataEntry dataEntry in areaData.Values)
                {
                    dataEntry?.Dispose();
                }
            }
        }


        public Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
        {
            List<ReadResultItem> result = new();
            foreach (ReadRequestItem item in readItems)
            {

                if (!_plcData.TryGetValue(item.Area, out Dictionary<ushort, PlcDataEntry> areaData))
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                if (!areaData.TryGetValue(item.DbNumber, out PlcDataEntry dataEntry))
                {
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }


                int size = item.NumberOfItems * item.ElementSize;
                if (item.TransportSize == DataTransportSize.Bit)
                {
                    int byteOffset = item.Offset / 8;
                    int bitNumber = item.Offset % 8;
                    if ((byteOffset + size) > dataEntry.Length)
                    {
                        result.Add(new ReadResultItem(item, ItemResponseRetValue.OutOfRange));
                        continue;
                    }

                    Memory<byte> data = new byte[] { Converter.GetBit(dataEntry.Data.Span[byteOffset], bitNumber) ? (byte)0x01 : (byte)0x00 };
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.Success, data));
                }
                else
                {
                    if ((item.Offset + size) > dataEntry.Length)
                    {
                        result.Add(new ReadResultItem(item, ItemResponseRetValue.OutOfRange));
                        continue;
                    }

                    Memory<byte> data = new byte[size];
                    dataEntry.Data.Slice(item.Offset, size).CopyTo(data);
                    result.Add(new ReadResultItem(item, ItemResponseRetValue.Success, data));
                }
            }
            return Task.FromResult(result);
        }

        public Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
        {
            List<WriteResultItem> result = new();
            foreach (WriteRequestItem item in writeItems)
            {
                if (!_plcData.TryGetValue(item.Area, out Dictionary<ushort, PlcDataEntry> areaData))
                {
                    result.Add(new WriteResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                if (!areaData.TryGetValue(item.DbNumber, out PlcDataEntry dataEntry))
                {
                    result.Add(new WriteResultItem(item, ItemResponseRetValue.DataError));
                    continue;
                }

                int size = item.NumberOfItems * item.ElementSize;
                if (item.TransportSize == DataTransportSize.Bit)
                {
                    int byteOffset = item.Offset / 8;
                    int bitNumber = item.Offset % 8;
                    if ((byteOffset + size) > dataEntry.Length)
                    {
                        result.Add(new WriteResultItem(item, ItemResponseRetValue.OutOfRange));
                        continue;
                    }

                    dataEntry.Data.Span[byteOffset] = Converter.SetBit(dataEntry.Data.Span[byteOffset], bitNumber, item.Data.Span[0] == 0x01);
                }
                else
                {
                    if ((item.Offset + size) > dataEntry.Length)
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
