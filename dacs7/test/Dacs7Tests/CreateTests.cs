using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Dacs7.Tests
{
    public class CreateTests
    {
        [Fact]
        public void CreateTest()
        {
            var ri = ReadItem.Create<byte[]>("DB1", 0, 10);
            var r2i = ReadItem.Create<byte[]>("DB1081", 0, 1081);
        }
    }
}
