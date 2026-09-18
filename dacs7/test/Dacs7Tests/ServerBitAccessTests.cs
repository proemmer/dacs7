using Dacs7.DataProvider;
using Dacs7.Domain;
using Dacs7.ReadWrite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class ServerBitAccessTests
    {
        private const string Localhost = "127.0.0.1";

        [Theory]
        [InlineData(19, "DB100.2,x3,1")]
        [InlineData(0, "DB100.0,x0,1")]
        [InlineData(7, "DB100.0,x7,1")]
        [InlineData(8, "DB100.1,x0,1")]
        public void BitRequestRendersBitTag(int bitAddress, string expected)
        {
            ReadRequestItem item = new(PlcArea.DB, 100, 1, bitAddress, ItemDataTransportSize.Bit, default);

            Assert.Equal(DataTransportSize.Bit, item.TransportSize);
            Assert.Equal(expected, item.ToTag());

            ReadItem parsed = ReadItem.CreateFromTag(item.ToTag());
            Assert.Equal(bitAddress, parsed.Offset);
            Assert.Equal(typeof(bool), parsed.ResultType);
        }

        [Fact]
        public void BitWriteRequestRendersBitTag()
        {
            WriteRequestItem item = new(PlcArea.FB, 0, 1, 42, ItemDataTransportSize.Bit, default, new byte[] { 0x01 });

            Assert.Equal(DataTransportSize.Bit, item.TransportSize);
            Assert.Equal("M.5,x2,1", item.ToTag());
        }

        [Fact]
        public void ByteRequestRenderingIsUnchanged()
        {
            ReadRequestItem item = new(PlcArea.DB, 100, 4, 2, ItemDataTransportSize.Byte, default);

            Assert.Equal(DataTransportSize.Byte, item.TransportSize);
            Assert.Equal("DB100.2,B,4", item.ToTag());
        }

        [Fact]
        public async Task ServerPassesBitAccessToProvider()
        {
            const int port = 5031;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10, 100);
            RecordingProvider provider = new(simulation);
            Dacs7Server server = new(port, provider);
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
            try
            {
                await client.ConnectAsync();

                IEnumerable<ItemResponseRetValue> writeResult = await client.WriteAsync(new KeyValuePair<string, object>("DB100.2,x3", true));
                Assert.Equal(ItemResponseRetValue.Success, writeResult.Single());

                DataValue bit = (await client.ReadAsync("DB100.2,x3")).Single();
                DataValue b = (await client.ReadAsync("DB100.2,b")).Single();
                Assert.True((bool)bit.Value);
                Assert.Equal((byte)0x08, (byte)b.Value);

                Assert.Contains(("DB100.2,x3,1", DataTransportSize.Bit), provider.Requests);
                Assert.Contains(("DB100.2,B,1", DataTransportSize.Byte), provider.Requests);
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task RelayProviderForwardsBitAccessToCorrectAddress()
        {
            const int plcPort = 5032;
            const int relayPort = 5033;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 32, 100);
            Dacs7Server plc = new(plcPort, simulation);
            await plc.ConnectAsync();

            Dacs7Client relayClient = new($"{Localhost}:{plcPort},0,1", PlcConnectionType.Pg, 5000);
            RelayPlcDataProvider relay = new();
            relay.UseClient(relayClient);
            Dacs7Server relayServer = new(relayPort, relay);
            await relayServer.ConnectAsync();

            Dacs7Client client = new($"{Localhost}:{relayPort},0,1", PlcConnectionType.Pg, 5000);
            Dacs7Client plcClient = new($"{Localhost}:{plcPort},0,1", PlcConnectionType.Pg, 5000);
            try
            {
                await relayClient.ConnectAsync();
                await client.ConnectAsync();
                await plcClient.ConnectAsync();

                Assert.Equal(ItemResponseRetValue.Success, (await client.WriteAsync(new KeyValuePair<string, object>("DB100.2,x3", true))).Single());

                // bit 3 of byte 2 must be set, byte 19 (the bit address misread as a byte offset) must be untouched
                Assert.Equal((byte)0x08, (byte)(await plcClient.ReadAsync("DB100.2,b")).Single().Value);
                Assert.Equal((byte)0x00, (byte)(await plcClient.ReadAsync("DB100.19,b")).Single().Value);
                Assert.True((bool)(await client.ReadAsync("DB100.2,x3")).Single().Value);
                Assert.False((bool)(await client.ReadAsync("DB100.2,x2")).Single().Value);
            }
            finally
            {
                await client.DisconnectAsync();
                await plcClient.DisconnectAsync();
                await relayClient.DisconnectAsync();
                await relayServer.DisconnectAsync();
                await plc.DisconnectAsync();
            }
        }

        [Fact]
        public async Task StopDoesNotLogErrors()
        {
            const int port = 5034;
            RecordingLoggerFactory logs = new();
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10, 100);
            Dacs7Server server = new(port, simulation, logs);
            await server.ConnectAsync();

            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
            await client.ConnectAsync();
            await client.ReadAsync("DB100.0,b");

            // stop the server while a client is still connected, then again with no client
            await server.DisconnectAsync();
            await client.DisconnectAsync();
            await server.ConnectAsync();
            await server.DisconnectAsync();
            await Task.Delay(200);

            AssertNoErrorsLogged(logs);
        }

        [Fact]
        public async Task ServerKeepsAcceptingUnderConcurrentConnects()
        {
            const int port = 5035;
            RecordingLoggerFactory logs = new();
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10, 100);
            Dacs7Server server = new(port, simulation, logs);
            await server.ConnectAsync();
            try
            {
                for (int round = 0; round < 10; round++)
                {
                    // connects that drop immediately, mixed with connects that must get a connection confirm
                    IEnumerable<Task<bool>> dropped = Enumerable.Range(0, 30).Select(async _ =>
                    {
                        using TcpClient tcp = new();
                        await tcp.ConnectAsync(Localhost, port);
                        return true;
                    });
                    IEnumerable<Task<bool>> handshakes = Enumerable.Range(0, 30).Select(_ => IsConnectionConfirmedAsync(port));

                    bool[] results = await Task.WhenAll(dropped.Concat(handshakes));
                    Assert.All(results, Assert.True);
                }

                Dacs7Client probe = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
                await probe.ConnectAsync();
                Assert.Equal(ItemResponseRetValue.Success, (await probe.ReadAsync("DB100.0,b")).Single().ReturnCode);
                await probe.DisconnectAsync();
            }
            finally
            {
                await server.DisconnectAsync();
            }
            await Task.Delay(200);

            // dropped raw connections are legitimately logged as errors, but nothing may be critical
            Assert.DoesNotContain(logs.Entries, e => e.Level == LogLevel.Critical);
        }

        // TPKT + COTP connection request (src tsap 0x0100, dst tsap 0x0102, tpdu size 1024)
        private static readonly byte[] _connectionRequest = { 0x03, 0x00, 0x00, 0x16, 0x11, 0xE0, 0x00, 0x00, 0x00, 0x01, 0x00, 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02 };

        private static async Task<bool> IsConnectionConfirmedAsync(int port)
        {
            using TcpClient tcp = new();
            await tcp.ConnectAsync(Localhost, port);
            NetworkStream stream = tcp.GetStream();
            await stream.WriteAsync(_connectionRequest, 0, _connectionRequest.Length);

            byte[] response = new byte[6];
            int read = 0;
            using CancellationTokenSource cts = new(5000);
            using (cts.Token.Register(() => tcp.Close()))
            {
                try
                {
                    while (read < response.Length)
                    {
                        int received = await stream.ReadAsync(response, read, response.Length - read, cts.Token);
                        if (received == 0)
                        {
                            return false;
                        }
                        read += received;
                    }
                }
                catch (Exception) when (cts.IsCancellationRequested)
                {
                    return false;
                }
            }
            return response[0] == 0x03 && response[5] == 0xD0; // TPKT version 3, COTP connection confirm
        }

        private static void AssertNoErrorsLogged(RecordingLoggerFactory logs)
        {
            List<string> errors = logs.Entries.Where(e => e.Level >= LogLevel.Error).Select(e => $"{e.Level}: {e.Message} {e.Exception}").ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        }

        private sealed class RecordingProvider : IPlcDataProvider
        {
            private readonly IPlcDataProvider _inner;

            public ConcurrentBag<(string Tag, DataTransportSize TransportSize)> Requests { get; } = new();

            public RecordingProvider(IPlcDataProvider inner)
            {
                _inner = inner;
            }

            public Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems)
            {
                readItems.ForEach(i => Requests.Add((i.ToTag(), i.TransportSize)));
                return _inner.ReadAsync(readItems);
            }

            public Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems)
            {
                writeItems.ForEach(i => Requests.Add((i.ToTag(), i.TransportSize)));
                return _inner.WriteAsync(writeItems);
            }
        }

        private sealed class RecordingLoggerFactory : ILoggerFactory, ILogger
        {
            public ConcurrentQueue<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) => this;
            public void Dispose() { }
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Entries.Enqueue((logLevel, formatter(state, exception), exception));
            }
        }
    }
}
