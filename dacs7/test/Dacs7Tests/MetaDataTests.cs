using Dacs7;
using Dacs7.Domain;
using System;
using Xunit;

namespace Dacs7Tests
{


#if TEST_PLC
    public class MetaDataTests
    {

        [Fact]
        public void GetBlocksCountTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksCount();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksOfType(PlcBlockType.Ob);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest2()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksOfType(PlcBlockType.Sdb);

            foreach (var c in bc)
            {
                Console.WriteLine(c.Number);
            }

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


        [Fact]
        public void ReadBlockInfoTest()
        {
            var db = 250;
            var client = new Dacs7Client(_loggerFactory);

            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.ReadBlockInfo(PlcBlockType.Db, db);
            Assert.Equal(db, blkInfo.BlockNumber);

            blkInfo = client.ReadBlockInfo(PlcBlockType.Sdb, 1000);
            Assert.Equal(db, 1001);

            blkInfo = client.ReadBlockInfo(PlcBlockType.Db, 250);
            Assert.Equal(250, blkInfo.BlockNumber);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }
    }

#endif
}
