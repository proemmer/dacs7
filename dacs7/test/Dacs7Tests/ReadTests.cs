
#define REALPLC

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
        private static readonly string Address = "benjipc677c";


        [Fact]
        public async Task ReadWriteBits()
        {
            await ExecuteAsync(async (client) =>
            {
                const string datablock = "DB1";
                var baseOffset = 10000 * 8;
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, false),
                       WriteItem.Create(datablock, baseOffset + 5, false))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<bool>(datablock, baseOffset),
                                                       ReadItem.Create<bool>(datablock, baseOffset + 5))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.False((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.False((bool)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, true),
                                       WriteItem.Create(datablock, baseOffset + 5, true))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<bool>(datablock, baseOffset),
                                                       ReadItem.Create<bool>(datablock, baseOffset + 5))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.True((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.True((bool)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, false),
                                       WriteItem.Create(datablock, baseOffset + 5, false))).ToArray();

            });
        }


        [Fact]
        public async Task ReadMultibleByteArrayData()
        {
            await ExecuteAsync(async (client) =>
            {
                const string datablock = "DB1";
                var results = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, 0, 1000),
                                                       ReadItem.Create<byte[]>(datablock, 2200, 100),
                                                       ReadItem.Create<byte[]>(datablock, 1000, 1000),
                                                       ReadItem.Create<byte[]>(datablock, 200, 100))).ToArray();


                Assert.Equal(4, results.Count());
                Assert.Equal(1000, results[0].Data.Length);
                Assert.Equal(100, results[1].Data.Length);
                Assert.Equal(1000, results[2].Data.Length);
                Assert.Equal(100, results[3].Data.Length);
            });
        }

        [Fact]
        public async Task ReadWriteBigDBData()
        {
            await ExecuteAsync(async (client) =>
            {
                const string datablock = "DB1";
                const ushort offset = 2500;
                var resultsDefault0 = new Memory<byte>(Enumerable.Repeat((byte)0x00, 1000).ToArray());
                var resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                var resultsDefault2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                var results0 = new Memory<byte>(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                var results1 = await client.WriteAsync(WriteItem.Create(datablock, offset, results0));
                var results2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));


                resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                Assert.True(results0.Span.SequenceEqual(results2.FirstOrDefault().Data.Span));
            });
        }









        private static async Task ExecuteAsync(Func<Dacs7Client, Task> execution)
        {
            var client = new Dacs7Client(Address);
            try
            {
                await client.ConnectAsync();
                await execution(client);
            }
            finally
            {
                await client.DisconnectAsync();
            }
        }
    }
}

#endif