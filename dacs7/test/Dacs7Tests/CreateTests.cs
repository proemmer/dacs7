using System;
using System.Threading;
using Xunit;

namespace Dacs7.Tests
{
    public class CreateTests
    {
        private int _referenceValue = 0;
        [Fact]
        public void CreateTest()
        {
            ReadItem ri = ReadItem.Create<byte[]>("DB1", 0, 10);
            ReadItem r2i = ReadItem.Create<byte[]>("DB1081", 0, 1081);
        }

        [Fact]
        public void GetNextReferenceIdTest()
        {
            _referenceValue = Convert.ToInt32(ushort.MaxValue);
            ushort id = unchecked((ushort)Interlocked.Increment(ref _referenceValue));
            if (id == 0)
            {
                id = unchecked((ushort)Interlocked.Increment(ref _referenceValue));
            }
        }
    }
}
