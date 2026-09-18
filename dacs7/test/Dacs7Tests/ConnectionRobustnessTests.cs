using Dacs7.DataProvider;
using Dacs7.Domain;
using Dacs7.Protocols.Rfc1006;
using Dacs7.Metadata;
using Dacs7.Protocols.SiemensPlc.Datagrams;
using Dacs7.ReadWrite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class ConnectionRobustnessTests
    {
        private const string Localhost = "127.0.0.1";

        private static byte[] Pattern(int length) => Enumerable.Range(0, length).Select(i => (byte)((i * 7) + 3)).ToArray();

        [Fact]
        public async Task ClientHandlesDatagramsSplitAcrossReceives()
        {
            const int serverPort = 5036;
            const int proxyPort = 5037;
            byte[] data = Pattern(1000);
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 1000, data.ToArray(), 1);
            Dacs7Server server = new(serverPort, simulation);
            await server.ConnectAsync();
            using SplittingProxy proxy = new(proxyPort, serverPort);
            Dacs7Client client = new($"{Localhost}:{proxyPort},0,1", PlcConnectionType.Pg, 5000);
            try
            {
                await client.ConnectAsync();

                for (int i = 0; i < 10; i++)
                {
                    DataValue value = (await client.ReadAsync($"DB1.{i * 10},b,40")).Single();
                    Assert.Equal(data.Skip(i * 10).Take(40), (byte[])value.Value);
                }

                // parallel reads, so the proxy splits responses which were received together
                IEnumerable<Task> parallel = Enumerable.Range(0, 20).Select(async i =>
                {
                    DataValue value = (await client.ReadAsync($"DB1.{i * 20},b,100")).Single();
                    Assert.Equal(data.Skip(i * 20).Take(100), (byte[])value.Value);
                });
                await Task.WhenAll(parallel);
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerConfirmsConnectionRequestWithUnknownParameter()
        {
            const int port = 5038;
            SimulationPlcDataProvider simulation = new();
            Dacs7Server server = new(port, simulation);
            await server.ConnectAsync();
            try
            {
                // connection request with an additional unknown parameter 0xc6 (length 2) in front of the known ones
                byte[] request = { 0x03, 0x00, 0x00, 0x1A, 0x15, 0xE0, 0x00, 0x00, 0x00, 0x01, 0x00, 0xC6, 0x02, 0xAA, 0xBB, 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02 };
                Assert.True(await IsConnectionConfirmedAsync(port, request));

                // the server is still responsive
                Assert.True(await IsConnectionConfirmedAsync(port, _connectionRequest));
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Theory]
        [InlineData(new byte[] { 0xC6, 0x02, 0xAA, 0xBB, 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02 })]
        [InlineData(new byte[] { 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02, 0x00 })]
        [InlineData(new byte[] { 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02, 0xC6, 0x05 })]
        public void ConnectionDatagramsSkipUnknownParameters(byte[] parameters)
        {
            byte[] frame = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 }.Concat(parameters).ToArray();
            frame[3] = (byte)frame.Length;
            frame[4] = (byte)(frame.Length - 5);

            frame[5] = 0xE0;
            Task<int> request = Task.Run(() =>
            {
                using ConnectionRequestDatagram cr = ConnectionRequestDatagram.TranslateFromMemory(frame.ToArray(), out int processed);
                Assert.Equal(0x0A, cr.SizeTpduReceiving.Span[0]);
                return processed;
            });
            Assert.True(request.Wait(5000), "connection request parsing did not terminate");
            Assert.Equal(frame.Length, request.Result);

            frame[5] = 0xD0;
            Task<int> confirm = Task.Run(() =>
            {
                using ConnectionConfirmedDatagram cc = ConnectionConfirmedDatagram.TranslateFromMemory(frame.ToArray(), out int processed);
                Assert.Equal(0x0A, cc.SizeTpduReceiving.Span[0]);
                return processed;
            });
            Assert.True(confirm.Wait(5000), "connection confirmed parsing did not terminate");
            Assert.Equal(frame.Length, confirm.Result);
        }

        [Fact]
        public async Task ServerDoesNotReconnectToDisconnectedClient()
        {
            const int port = 5039;
            RecordingLoggerFactory logs = new();
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10, 1);
            Dacs7Server server = new(port, simulation, logs);
            await server.ConnectAsync();
            try
            {
                Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
                await client.ConnectAsync();
                await client.ReadAsync("DB1.0,b");
                await client.DisconnectAsync();

                // the default reconnect time is 5 seconds
                await Task.Delay(7000);

                Assert.DoesNotContain(logs.Entries, e => e.Category.EndsWith("ClientSocket", StringComparison.Ordinal) && e.Message.StartsWith("Socket connecting", StringComparison.Ordinal));
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerRepliesWhenProviderFails()
        {
            const int port = 5040 + 1; // 5040 is used by windows
            Dacs7Server server = new(port, new FailingProvider());
            await server.ConnectAsync();
            Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
            try
            {
                await client.ConnectAsync();

                DateTime start = DateTime.UtcNow;
                IEnumerable<DataValue> read = await client.ReadAsync("DB1.0,b", "DB1.2,w");
                Assert.All(read, r => Assert.Equal(ItemResponseRetValue.HardwareFault, r.ReturnCode));

                IEnumerable<ItemResponseRetValue> write = await client.WriteAsync(new KeyValuePair<string, object>("DB1.0,b", (byte)1));
                Assert.All(write, r => Assert.Equal(ItemResponseRetValue.HardwareFault, r));

                // answered, not timed out
                Assert.True(DateTime.UtcNow - start < TimeSpan.FromSeconds(4));
            }
            finally
            {
                await client.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ClientsConnectUnderLoad()
        {
            const int port = 5042;
            SimulationPlcDataProvider simulation = new();
            simulation.Register(PlcArea.DB, 10, 1);
            Dacs7Server server = new(port, simulation);
            await server.ConnectAsync();
            try
            {
                for (int round = 0; round < 50; round++)
                {
                    // no retry: a connect must not miss a fast connection confirmed
                    await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
                    {
                        Dacs7Client client = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 5000);
                        try
                        {
                            await client.ConnectAsync();
                            Assert.True((await client.ReadAsync("DB1.0,b")).Single().IsSuccessReturnCode);
                        }
                        finally
                        {
                            await client.DisconnectAsync();
                        }
                    }));
                }
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public void BlocksOfTypeContainsAllBlocks()
        {
            // two entries: block number, flags, language
            byte[] data = { 0x00, 0x01, 0x22, 0x05, 0x00, 0x02, 0x22, 0x05 };
            List<IPlcBlock> blocks = S7PlcBlocksOfTypeAckDatagram.TranslateFromSslData(data, data.Length);
            Assert.Equal(new[] { 1, 2 }, blocks.Select(b => b.Number));
        }

        // TPKT + COTP connection request (src tsap 0x0100, dst tsap 0x0102, tpdu size 1024)
        private static readonly byte[] _connectionRequest = { 0x03, 0x00, 0x00, 0x16, 0x11, 0xE0, 0x00, 0x00, 0x00, 0x01, 0x00, 0xC0, 0x01, 0x0A, 0xC1, 0x02, 0x01, 0x00, 0xC2, 0x02, 0x01, 0x02 };

        private static async Task<bool> IsConnectionConfirmedAsync(int port, byte[] request)
        {
            using TcpClient tcp = new();
            await tcp.ConnectAsync(Localhost, port);
            NetworkStream stream = tcp.GetStream();
            await stream.WriteAsync(request, 0, request.Length);

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

        /// <summary>
        /// Forwards a tcp connection and splits each larger chunk from the server into two sends,
        /// so the client receives datagrams in several parts.
        /// </summary>
        private sealed class SplittingProxy : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly int _targetPort;

            public SplittingProxy(int port, int targetPort)
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
                    inbound.NoDelay = outbound.NoDelay = true;
                    await outbound.ConnectAsync(Localhost, _targetPort);
                    await Task.WhenAny(PumpAsync(inbound.GetStream(), outbound.GetStream(), false), PumpAsync(outbound.GetStream(), inbound.GetStream(), true));
                }
            }

            private static async Task PumpAsync(NetworkStream from, NetworkStream to, bool split)
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

                        // the connection setup is left untouched, only the data responses are split
                        if (split && received > 40)
                        {
                            int half = received / 2;
                            await to.WriteAsync(buffer, 0, half);
                            await Task.Delay(20);
                            await to.WriteAsync(buffer, half, received - half);
                        }
                        else
                        {
                            await to.WriteAsync(buffer, 0, received);
                        }
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

        private sealed class FailingProvider : IPlcDataProvider
        {
            public Task<List<ReadResultItem>> ReadAsync(List<ReadRequestItem> readItems) => throw new InvalidOperationException("provider failure");

            public Task<List<WriteResultItem>> WriteAsync(List<WriteRequestItem> writeItems) => throw new InvalidOperationException("provider failure");
        }

        private sealed class RecordingLoggerFactory : ILoggerFactory
        {
            public ConcurrentQueue<(string Category, LogLevel Level, string Message)> Entries { get; } = new();

            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);
            public void Dispose() { }

            private sealed class Logger : ILogger
            {
                private readonly RecordingLoggerFactory _factory;
                private readonly string _category;

                public Logger(RecordingLoggerFactory factory, string category)
                {
                    _factory = factory;
                    _category = category;
                }

                public IDisposable BeginScope<TState>(TState state) => null;
                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    _factory.Entries.Enqueue((_category, logLevel, formatter(state, exception)));
                }
            }
        }
    }
}
