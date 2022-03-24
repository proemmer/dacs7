using Dacs7.Metadata;
using Dacs7.ReadWrite;
using Dacs7Tests.ServerHelper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class MetadataTests
    {
        [Fact]
        public async Task ReadMetadataOfNotExistingBlock()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                IPlcBlockInfo x = await client.ReadBlockInfoAsync(PlcBlockType.Db, 66);
                Assert.Equal(0, x.CodeSize);
            });
        }

        [Fact]
        public async Task ReadOfNotExistingBlock()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                IEnumerable<DataValue> x = await client.ReadAsync("DB66.0,B");
            });
        }
    }
}
