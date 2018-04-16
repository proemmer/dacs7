
//#define REALPLC

#if REALPLC

using Dacs7;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7Tests
{
    public class ReadTests
    {
        private static string Address = "benjipc677c";


        [Fact]
        public async Task ReadMultibleData()
        {
            var client = new Dacs7Client(Address);
            try
            {
                await client.ConnectAsync();

                var results = (await client.ReadAsync(ReadItemSpecification.Create<byte[]>("DB1114", 0, 1000),
                                                       ReadItemSpecification.Create<byte[]>("DB1114", 2200, 100),
                                                       ReadItemSpecification.Create<byte[]>("DB1114", 1000, 1000),
                                                       ReadItemSpecification.Create<byte[]>("DB1114", 200, 100))).ToArray();


                Assert.Equal(4, results.Count());
                Assert.Equal(1000, results[0].Data.Length);
                Assert.Equal(100, results[1].Data.Length);
                Assert.Equal(1000, results[2].Data.Length);
                Assert.Equal(100, results[3].Data.Length);
            }
            finally
            {
                await client.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ReadWriteBigDBData()
        {
            var client = new Dacs7Client(Address);
            try
            {
                await client.ConnectAsync();
                var results0 = new Memory<byte>(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                var results1 = await client.WriteAsync(WriteItemSpecification.Create("DB1114", 0, results0));
                var results2 = (await client.ReadAsync(ReadItemSpecification.Create<byte[]>("DB1114", 0, 1000)));
                Assert.True(results0.Span.SequenceEqual(results2.FirstOrDefault().Data.Span));
            }
            finally
            {
                await client.DisconnectAsync();
            }
        }
    }
}

#endif