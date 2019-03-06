using Dacs7.ReadWrite;
using Dacs7Tests.ServerHelper;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    [Collection("PlcServer collection")]
    public class DataConsistentTests
    {

        [Fact]
        public async Task ReadWriteDataAndCheckIfTheResultsDontChangeBecauseOfBufferReusing()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB4";
                const ushort offset = 2500;
                var resultsDefault0 = new Memory<byte>(Enumerable.Repeat((byte)0x00, 1000).ToArray());
                var resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                var resultsDefault2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                var first = resultsDefault2.FirstOrDefault();
                var copy1 = new byte[first.Data.Length];
                first.Data.CopyTo(copy1);

                Assert.True(resultsDefault0.Span.SequenceEqual(first.Data.Span), "1");
                Assert.True(resultsDefault0.Span.SequenceEqual(copy1), "2");

                var results0 = new Memory<byte>(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                var results1 = await client.WriteAsync(WriteItem.Create(datablock, offset, results0));
                var results2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                var second = results2.FirstOrDefault();
                var copy2 = new byte[second.Data.Length];
                second.Data.CopyTo(copy2);


                resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                Assert.True(results0.Span.SequenceEqual(results2.FirstOrDefault().Data.Span), "3");
                Assert.True(results0.Span.SequenceEqual(copy2), "4");


                var results00 = new Memory<byte>(Enumerable.Repeat((byte)0x01, 1000).ToArray());
                var results01 = await client.WriteAsync(WriteItem.Create(datablock, offset, results00));
                var results02 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                var third = results02.FirstOrDefault();
                var copy3 = new byte[third.Data.Length];
                third.Data.CopyTo(copy3);

                resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                Assert.True(results00.Span.SequenceEqual(results02.FirstOrDefault().Data.Span), "5");
                Assert.True(results00.Span.SequenceEqual(copy3), "6");


                Assert.True(resultsDefault0.Span.SequenceEqual(first.Data.Span), "7");
                Assert.True(resultsDefault0.Span.SequenceEqual(copy1), "8");
                Assert.True(first.Data.Span.SequenceEqual(copy1), "9");

                Assert.True(results0.Span.SequenceEqual(second.Data.Span), "10");
                Assert.True(results0.Span.SequenceEqual(copy2), "11");
                Assert.True(second.Data.Span.SequenceEqual(copy2), "12");

                Assert.True(results00.Span.SequenceEqual(third.Data.Span), "13");
                Assert.True(results00.Span.SequenceEqual(copy3), "14");
                Assert.True(third.Data.Span.SequenceEqual(copy3), "15");
            });
        }

    }
}
