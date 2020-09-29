using Dacs7.DataProvider;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class ServerTests
    {

        [Fact]
        public async Task ListenOnPort()
        {
            var dacs7Server = new Dacs7Server(5011, SimulationPlcDataProvider.Instance);
            await dacs7Server.ConnectAsync();

            //var dacstClient = new Dacs7Client("127.0.0.1:5011");
            //await dacstClient.ConnectAsync();

            await Task.Delay(30000);

            //await dacstClient.DisconnectAsync();

            await dacs7Server.DisconnectAsync();
        }
    }
}
