using Dacs7.Metadata;
using Dacs7Tests.ServerHelper;
using System;
using System.Collections.Generic;
using System.Text;
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
                var x = await client.ReadBlockInfoAsync(PlcBlockType.Db, 66);
                Assert.Equal(0, x.CodeSize);
            });
        }
    }
}
