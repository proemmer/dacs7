using Dacs7.ReadWrite;
using Dacs7Tests.ServerHelper;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    [Collection("PlcServer collection")]
    public class ReUseTest
    {
        [Fact()]
        public async Task ExecuteClientAsync()
        {
            Dacs7Client client = new(PlcTestServer.Address, PlcTestServer.ConnectionType, PlcTestServer.Timeout);

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await client.ConnectAsync();
                    DataValue[] results = (await client.ReadAsync(ReadItem.Create<bool>("DB3", 0))).ToArray();

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
