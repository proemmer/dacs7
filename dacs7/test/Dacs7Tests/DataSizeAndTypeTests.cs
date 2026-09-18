using Dacs7.DataProvider;
using Dacs7.Domain;
using Dacs7.ReadWrite;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class DataSizeAndTypeTests
    {
        private const string Localhost = "127.0.0.1";

        public static IEnumerable<object[]> LargeArrays()
        {
            int port = 5300;
            foreach (ushort pdu in new ushort[] { 240, 480, 960 })
            {
                yield return new object[] { port++, pdu, "i", Enumerable.Range(0, 1000).Select(i => (short)(i - 500)).ToArray() };
                yield return new object[] { port++, pdu, "w", Enumerable.Range(0, 1000).Select(i => (ushort)(i * 60)).ToArray() };
                yield return new object[] { port++, pdu, "di", Enumerable.Range(0, 1000).Select(i => (i * 100000) - 7).ToArray() };
                yield return new object[] { port++, pdu, "dw", Enumerable.Range(0, 1000).Select(i => (uint)i * 4000000u).ToArray() };
                yield return new object[] { port++, pdu, "r", Enumerable.Range(0, 1000).Select(i => i * 0.25f).ToArray() };
                yield return new object[] { port++, pdu, "li", Enumerable.Range(0, 1000).Select(i => (long)i * -1000000007L).ToArray() };
            }
        }

        [Theory]
        [MemberData(nameof(LargeArrays))]
        public async Task LargeArraysAreSplitToFitIntoThePdu(int port, ushort pdu, string type, Array values)
        {
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10000, 1);
            RecordingProvider provider = new(simulation);
            Dacs7Server server = new(port, provider);
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000) { PduSize = pdu };
            try
            {
                await client.ConnectAsync();
                string tag = $"DB1.100,{type},{values.Length}";

                Assert.Equal(ItemResponseRetValue.Success, (await client.WriteAsync(new KeyValuePair<string, object>(tag, values))).Single());
                DataValue result = (await client.ReadAsync(tag)).Single();

                Assert.Equal(ItemResponseRetValue.Success, result.ReturnCode);
                Assert.Equal(values.Cast<object>(), ((Array)result.Value).Cast<object>());
                Assert.All(provider.RequestSizes, size => Assert.True(size <= pdu, $"request of {size} bytes exceeds the pdu of {pdu}"));
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task SmallItemsAreStillReadWithOneRequest()
        {
            const int port = 5350;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 1000, 1);
            RecordingProvider provider = new(simulation);
            Dacs7Server server = new(port, provider);
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000) { PduSize = 240 };
            try
            {
                await client.ConnectAsync();
                while (provider.RequestSizes.TryDequeue(out _)) { }

                IEnumerable<DataValue> results = await client.ReadAsync("DB1.0,b", "DB1.2,w", "DB1.4,i", "DB1.6,di", "DB1.10,r", "DB1.14,x3", "DB1.16,s,10", "DB1.30,c,4", "DB1.40,i,10");

                Assert.All(results, r => Assert.True(r.IsSuccessReturnCode));
                Assert.Single(provider.RequestSizes);
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task NewTypesAreTransferredAsBytes()
        {
            const int port = 5351;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 1000, 1);
            Dacs7Server server = new(port, simulation);
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
            try
            {
                await client.ConnectAsync();

                await AssertRoundTrip(client, "DB1.0,li", -1234567890123L, Be(-1234567890123L));
                await AssertRoundTrip(client, "DB1.8,lw", 0x0102030405060708UL, Be(0x0102030405060708L));
                await AssertRoundTrip(client, "DB1.16,li,2", new long[] { 1, -2 }, Be(1).Concat(Be(-2)).ToArray());
                await AssertRoundTrip(client, "DB1.40,si", (sbyte)-5, new byte[] { 0xFB });
                await AssertRoundTrip(client, "DB1.41,si,3", new sbyte[] { -1, 0, 127 }, new byte[] { 0xFF, 0x00, 0x7F });
                await AssertRoundTrip(client, "DB1.44,wc", 'Ä', new byte[] { 0x00, 0xC4 });
                await AssertRoundTrip(client, "DB1.46,wc,3", "Grü", Encoding.BigEndianUnicode.GetBytes("Grü"));
                await AssertRoundTrip(client, "DB1.60,ws,10", "Grüße", new byte[] { 0x00, 0x0A, 0x00, 0x05 }.Concat(Encoding.BigEndianUnicode.GetBytes("Grüße")).ToArray());

                // a WString[10] is 24 bytes in the plc, only the actual characters are returned
                DataValue wstring = (await client.ReadAsync("DB1.60,ws,10")).Single();
                Assert.Equal(24, wstring.Data.Length);
                Assert.Equal("Grüße", wstring.Value);
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Theory]
        [InlineData("DB1.0,li", typeof(long), typeof(long))]
        [InlineData("DB1.0,li,3", typeof(long), typeof(long[]))]
        [InlineData("DB1.0,si", typeof(sbyte), typeof(sbyte))]
        [InlineData("DB1.0,si,3", typeof(sbyte), typeof(sbyte[]))]
        [InlineData("DB1.0,ws,10", typeof(string), typeof(string))]
        [InlineData("DB1.0,wc", typeof(char), typeof(char))]
        [InlineData("DB1.0,wc,3", typeof(char), typeof(string))]
        public void NewTagTypesAreParsed(string tag, Type varType, Type resultType)
        {
            ReadItem item = ReadItem.CreateFromTag(tag);
            Assert.Equal(varType, item.VarType);
            Assert.Equal(resultType, item.ResultType);
        }

        [Fact]
        public async Task ServerUsesTheNegotiatedPduSize()
        {
            const int port = 5352;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 1000, 1);
            Dacs7Server server = new(port, simulation) { PduSize = 240 };
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000) { PduSize = 960 };
            try
            {
                await client.ConnectAsync();
                Assert.Equal(240, client.PduSize);

                // the client splits this read, because the pdu size is 240
                DataValue value = (await client.ReadAsync("DB1.0,b,800")).Single();
                Assert.Equal(ItemResponseRetValue.Success, value.ReturnCode);
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerRejectsJobsLargerThanThePdu()
        {
            const int port = 5353;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 1000, 1);
            Dacs7Server server = new(port, simulation) { PduSize = 240 };
            await server.ConnectAsync();
            try
            {
                using TcpClient tcp = new();
                await tcp.ConnectAsync(Localhost, port);
                NetworkStream stream = tcp.GetStream();

                await SendAsync(stream, _connectionRequest);
                await ReceiveDatagramAsync(stream);

                // communication setup with pdu size 960, the server negotiates 240
                await SendAsync(stream, Dt(new byte[] { 0x32, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00, 0x00, 0xF0, 0x00, 0x00, 0x01, 0x00, 0x01, 0x03, 0xC0 }));
                byte[] setup = await ReceiveDatagramAsync(stream);
                Assert.Equal(240, BinaryPrimitives.ReadUInt16BigEndian(setup.AsSpan(7 + 12 + 6)));

                // read 800 bytes of DB1, the response does not fit into 240 bytes
                await SendAsync(stream, Dt(new byte[] { 0x32, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0E, 0x00, 0x00, 0x04, 0x01, 0x12, 0x0A, 0x10, 0x02, 0x03, 0x20, 0x00, 0x01, 0x84, 0x00, 0x00, 0x00 }));
                byte[] response = await ReceiveDatagramAsync(stream);

                Assert.Equal(0x02, response[7 + 1]);    // ack
                Assert.Equal(0x85, response[7 + 10]);   // error class
                Assert.Equal(0x00, response[7 + 11]);   // error code
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task DatagramsLargerThanTheFrameSizeAreFragmented()
        {
            const int port = 5354;
            byte[] data = Enumerable.Range(0, 5000).Select(i => (byte)((i * 11) + 5)).ToArray();
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 5000, data.ToArray(), 1);
            Dacs7Server server = new(port, simulation) { PduSize = 1920 };
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000) { PduSize = 1920 };
            try
            {
                await client.ConnectAsync();
                Assert.Equal(1920, client.PduSize);

                // more than one frame of 1024 bytes
                DataValue value = (await client.ReadAsync("DB1.0,b,1800")).Single();
                Assert.Equal(data.Take(1800), value.Data.ToArray());

                byte[] written = Enumerable.Range(0, 1800).Select(i => (byte)(255 - (i % 256))).ToArray();
                Assert.Equal(ItemResponseRetValue.Success, (await client.WriteAsync(new KeyValuePair<string, object>("DB1.2000,b,1800", written))).Single());
                Assert.Equal(written, (await client.ReadAsync("DB1.2000,b,1800")).Single().Data.ToArray());
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ClientReconnectsAfterConnectionBrokeDuringFragmentedDatagram()
        {
            const int port = 5355;
            const int proxyPort = 5356;
            byte[] data = Enumerable.Range(0, 5000).Select(i => (byte)((i * 11) + 5)).ToArray();
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 5000, data.ToArray(), 1);
            Dacs7Server server = new(port, simulation) { PduSize = 1920 };
            await server.ConnectAsync();
            using CuttingProxy proxy = new(proxyPort, port);
            Dacs7Client client = new($"{Localhost}:{proxyPort},0,1", PlcConnectionType.Pg, 3000) { PduSize = 1920 };
            try
            {
                await client.ConnectAsync();

                // the proxy forwards only the first fragment of the response and closes the connection
                proxy.CutNextLargeResponse = true;
                await Assert.ThrowsAnyAsync<Exception>(() => client.ReadAsync("DB1.0,b,1800"));

                await client.DisconnectAsync();
                await client.ConnectAsync();
                Assert.Equal(data.Take(100), (await client.ReadAsync("DB1.0,b,100")).Single().Data.ToArray());
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        private static async Task AssertRoundTrip(Dacs7Client client, string tag, object value, byte[] expectedBytes)
        {
            Assert.Equal(ItemResponseRetValue.Success, (await client.WriteAsync(new KeyValuePair<string, object>(tag, value))).Single());

            int offset = int.Parse(tag.Split('.')[1].Split(',')[0]);
            Assert.Equal(expectedBytes, (await client.ReadAsync($"DB1.{offset},b,{expectedBytes.Length}")).Single().Data.ToArray());

            object read = (await client.ReadAsync(tag)).Single().Value;
            if (value is Array expected)
            {
                Assert.Equal(expected.Cast<object>(), ((Array)read).Cast<object>());
            }
            else
            {
                Assert.Equal(value, read);
            }
        }

        private static byte[] Be(long value)
        {
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            return bytes;
        }

        // TPKT + COTP connection request (src tsap 0x0100, dst tsap 0x0102, tpdu size 1024)
        private static readonly byte[] _connectionRequest = { 0x03, 0x00, 0x00, 0x16, 0x11, 0xE0, 0x00, 0x00, 0x00, 0x01, 0x00, 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02 };

        /// <summary>
        /// Wraps a s7 datagram into TPKT and COTP data transfer.
        /// </summary>
        private static byte[] Dt(byte[] s7)
        {
            byte[] result = new byte[s7.Length + 7];
            result[0] = 0x03;
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), (ushort)result.Length);
            result[4] = 0x02;
            result[5] = 0xF0;
            result[6] = 0x80;
            s7.CopyTo(result, 7);
            return result;
        }

        private static Task SendAsync(NetworkStream stream, byte[] data) => stream.WriteAsync(data, 0, data.Length);

        private static async Task<byte[]> ReceiveDatagramAsync(NetworkStream stream)
        {
            using CancellationTokenSource cts = new(5000);
            byte[] header = new byte[4];
            await ReadExactlyAsync(stream, new ArraySegment<byte>(header), cts.Token);
            byte[] datagram = new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2))];
            header.CopyTo(datagram, 0);
            await ReadExactlyAsync(stream, new ArraySegment<byte>(datagram, 4, datagram.Length - 4), cts.Token);
            return datagram;
        }

        private static async Task ReadExactlyAsync(NetworkStream stream, ArraySegment<byte> buffer, CancellationToken token)
        {
            int read = 0;
            while (read < buffer.Count)
            {
                int received = await stream.ReadAsync(buffer.Array, buffer.Offset + read, buffer.Count - read, token);
                if (received == 0)
                {
                    throw new InvalidOperationException("connection closed");
                }
                read += received;
            }
        }

        /// <summary>
        /// Records the size of each request the server receives.
        /// </summary>
        private sealed class RecordingProvider : IPlcDataProvider
        {
            private readonly IPlcDataProvider _inner;

            public ConcurrentQueue<int> RequestSizes { get; } = new();

            public RecordingProvider(IPlcDataProvider inner)
            {
                _inner = inner;
            }

            public Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
            {
                int response = 14 + readItems.Sum(i => 4 + (i.NumberOfItems * i.ElementSize)) + readItems.Take(readItems.Count - 1).Count(i => (i.NumberOfItems * i.ElementSize) % 2 != 0);
                RequestSizes.Enqueue(Math.Max(12 + (12 * readItems.Count), response));
                return _inner.ReadAsync(readItems);
            }

            public Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
            {
                RequestSizes.Enqueue(12 + (12 * writeItems.Count) + writeItems.Sum(i => 4 + (i.NumberOfItems * i.ElementSize)) + writeItems.Take(writeItems.Count - 1).Count(i => (i.NumberOfItems * i.ElementSize) % 2 != 0));
                return _inner.WriteAsync(writeItems);
            }
        }

        /// <summary>
        /// Forwards a tcp connection, but can forward only the first fragment of a large response and then close the connection.
        /// </summary>
        private sealed class CuttingProxy : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly int _targetPort;

            public volatile bool CutNextLargeResponse;

            public CuttingProxy(int port, int targetPort)
            {
                _targetPort = targetPort;
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _ = AcceptAsync();
            }

            private async Task AcceptAsync()
            {
                while (true)
                {
                    TcpClient inbound;
                    try
                    {
                        inbound = await _listener.AcceptTcpClientAsync();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    _ = ForwardAsync(inbound);
                }
            }

            private async Task ForwardAsync(TcpClient inbound)
            {
                using (inbound)
                using (TcpClient outbound = new())
                {
                    await outbound.ConnectAsync(Localhost, _targetPort);
                    await Task.WhenAny(PumpAsync(inbound.GetStream(), outbound.GetStream(), false), PumpAsync(outbound.GetStream(), inbound.GetStream(), true));
                }
            }

            private async Task PumpAsync(NetworkStream from, NetworkStream to, bool fromServer)
            {
                byte[] buffer = new byte[65536];
                try
                {
                    while (true)
                    {
                        int received = await from.ReadAsync(buffer, 0, buffer.Length);
                        if (received == 0)
                        {
                            return;
                        }

                        if (fromServer && CutNextLargeResponse && received > 1100)
                        {
                            CutNextLargeResponse = false;
                            int firstFragment = (buffer[2] << 8) | buffer[3];
                            await to.WriteAsync(buffer, 0, firstFragment);
                            await Task.Delay(100);
                            return;
                        }
                        await to.WriteAsync(buffer, 0, received);
                    }
                }
                catch (Exception)
                {
                    // connection closed
                }
            }

            public void Dispose()
            {
                _listener.Stop();
            }
        }
    }
}
