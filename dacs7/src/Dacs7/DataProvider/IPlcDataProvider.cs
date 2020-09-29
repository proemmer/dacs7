using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.DataProvider
{

    public interface IPlcDataProvider
    {
        bool Register(PlcArea area, ushort dataLength, ushort dbNumber = 0);
        bool Register(PlcArea area, ushort dataLength, Memory<byte> data, ushort dbNumber = 0);
        bool Release(PlcArea area, ushort dbNumber = 0);

        Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems);
        Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems);
    }
}
