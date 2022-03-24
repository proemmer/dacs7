using System;
using System.Buffers;

namespace Dacs7.Domain
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

        public void Dispose()
        {
            _owner?.Dispose();
        }
    }
}
