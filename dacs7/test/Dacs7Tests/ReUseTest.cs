using Dacs7;
using Dacs7Tests.ServerHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7Tests
{
    [Collection("PlcServer collection")]
    public class ReUseTest
    {
        [Fact()]
        public async Task ExecuteClientAsync()
        {
            var client = new Dacs7Client(PlcTestServer.Address, PlcTestServer.ConnectionType, PlcTestServer.Timeout);

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await client.ConnectAsync();
                    var results = (await client.ReadAsync(ReadItem.Create<bool>("DB3", 0))).ToArray();

                    Assert.Single(results);
                    Assert.True(results[0].IsSuccessReturnCode);
                }
                finally
                {
                    await client.DisconnectAsync();
                }
            }
        }
    }
}
