using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dacs7
{

    internal class PlcDataEntry : IDisposable
    {
        private readonly IMemoryOwner<byte> _owner;
        private readonly Memory<byte> _externalData;


        public PlcDataEntry(PlcArea area, ushort dbNumber, ushort length, Memory<byte> data = default)
        {
            Area = area;
            DbNumber = dbNumber;
            Length = length;
            if (!data.IsEmpty)
            {
                _externalData = data;
            }
            else
            {
                _owner = MemoryPool<byte>.Shared.Rent(length);
            }
        }

        public PlcArea Area { get; private set; }
        public ushort DbNumber { get; private set; }
        public ushort Length { get; private set; }


        public Memory<byte> Data => _owner == null ? _externalData : _owner.Memory;

        public void Dispose() => _owner?.Dispose();
    }



    public interface IPlcDataProvider
    {
        bool Register(PlcArea area, ushort dataLength, ushort dbNumber = (ushort)0);
        bool Register(PlcArea area, ushort dataLength, Memory<byte> data, ushort dbNumber = (ushort)0);
        bool Release(PlcArea area, ushort dbNumber = 0);

        Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems);
        Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems);
    }
}
