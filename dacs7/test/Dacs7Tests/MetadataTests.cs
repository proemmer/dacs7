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
        // The dacs7 simulation server implements the communication setup, read and write jobs only. It does not
        // answer block info (userdata) jobs at all, so this test runs into the read timeout. It needs a real plc,
        // like it had before the snap7 test server was removed.
        [Fact(Skip = "Needs a real plc, the simulation server does not implement block info jobs.")]
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
