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
            var ri = ReadItem.Create<byte[]>("DB1", 0, 10);
            var r2i = ReadItem.Create<byte[]>("DB1081", 0, 1081);
        }

        [Fact]
        public void GetNextReferenceIdTest()
        {
            _referenceValue = Convert.ToInt32(ushort.MaxValue);
            var id = unchecked((ushort)Interlocked.Increment(ref _referenceValue));
            if(id == 0)
            {
                id = unchecked((ushort)Interlocked.Increment(ref _referenceValue));
            }
        }
    }
}
