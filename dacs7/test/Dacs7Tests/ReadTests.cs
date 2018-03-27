using Dacs7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7Tests
{
    public class ReadTests
    {
        private static string Address = "benjipc677c";




        [Fact]
        public async void ReadWriteBigDBData()
        {
            var client = new Dacs7Client(Address);
            try
            {
                await client.ConnectAsync();

                var results0 = new Memory<byte>(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                var results1 = await client.WriteAsync(1114, 0, results0);
                var results2 = await client.ReadAsync(1114, 0, 1000);
                if (results0.Span.SequenceEqual(results2.Span))
                {

                }

            }
            finally
            {
                await client.DisconnectAsync();
            }
        }
    }
}
