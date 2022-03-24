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
                Memory<byte> resultsDefault0 = new(Enumerable.Repeat((byte)0x00, 1000).ToArray());
                System.Collections.Generic.IEnumerable<ItemResponseRetValue> resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                System.Collections.Generic.IEnumerable<DataValue> resultsDefault2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                DataValue first = resultsDefault2.FirstOrDefault();
                byte[] copy1 = new byte[first.Data.Length];
                first.Data.CopyTo(copy1);

                Assert.True(resultsDefault0.Span.SequenceEqual(first.Data.Span), "1");
                Assert.True(resultsDefault0.Span.SequenceEqual(copy1), "2");

                Memory<byte> results0 = new(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                System.Collections.Generic.IEnumerable<ItemResponseRetValue> results1 = await client.WriteAsync(WriteItem.Create(datablock, offset, results0));
                System.Collections.Generic.IEnumerable<DataValue> results2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                DataValue second = results2.FirstOrDefault();
                byte[] copy2 = new byte[second.Data.Length];
                second.Data.CopyTo(copy2);


                resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                Assert.True(results0.Span.SequenceEqual(results2.FirstOrDefault().Data.Span), "3");
                Assert.True(results0.Span.SequenceEqual(copy2), "4");


                Memory<byte> results00 = new(Enumerable.Repeat((byte)0x01, 1000).ToArray());
                System.Collections.Generic.IEnumerable<ItemResponseRetValue> results01 = await client.WriteAsync(WriteItem.Create(datablock, offset, results00));
                System.Collections.Generic.IEnumerable<DataValue> results02 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                DataValue third = results02.FirstOrDefault();
                byte[] copy3 = new byte[third.Data.Length];
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
