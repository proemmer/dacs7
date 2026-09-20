using Dacs7.DataProvider;
using Dacs7.Domain;
using Dacs7.ReadWrite;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    /// <summary>
    /// Pins where the server listens. By default this is the loopback interface only, because the server
    /// is not authenticated, everyone who reaches it can read and write the simulated data blocks.
    /// If one of these tests fails after an update, the exposure of the server has changed.
    /// </summary>
    public class ServerBindAddressTests
    {
        private const string Localhost = "127.0.0.1";

        [Fact]
        public async Task ServerListensOnLoopbackOnlyByDefault()
        {
            const int port = 5361;
            Dacs7Server server = new(port, CreateProvider());
            await server.ConnectAsync();
            try
            {
                Assert.True(server.IsListening);
                Assert.Equal(Localhost, server.BindAddress);
                Assert.Equal(new[] { new IPEndPoint(IPAddress.Loopback, port) }, ListenersOn(port));
                await AssertClientCanRead(Localhost, port);
            }
            finally
            {
                await server.DisconnectAsync();
            }

            Assert.False(server.IsListening);
            Assert.Empty(ListenersOn(port));
        }

        [Fact]
        public async Task ServerCanListenOnAllInterfaces()
        {
            const int port = 5362;
            Dacs7Server server = new("any", port, CreateProvider());
            await server.ConnectAsync();
            try
            {
                IPEndPoint[] listeners = ListenersOn(port);
                Assert.Contains(listeners, ep => ep.Address.Equals(IPAddress.Any) || ep.Address.Equals(IPAddress.IPv6Any));
                await AssertClientCanRead(Localhost, port); // dual mode, so ipv4 clients are served too
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerBindsToAGivenAddressOnly()
        {
            const int port = 5363;
            Dacs7Server server = new(Localhost, port, CreateProvider());
            await server.ConnectAsync();
            try
            {
                Assert.Equal(new[] { new IPEndPoint(IPAddress.Loopback, port) }, ListenersOn(port));
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerResolvesAHostname()
        {
            const int port = 5364;
            Dacs7Server server = new("localhost", port, CreateProvider());
            await server.ConnectAsync();
            try
            {
                Assert.NotEmpty(ListenersOn(port));
                await AssertClientCanRead(Localhost, port);
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerCanBeStartedAgainAfterItWasStopped()
        {
            const int port = 5365;
            Dacs7Server server = new(port, CreateProvider());
            await server.ConnectAsync();
            await server.DisconnectAsync();

            await server.ConnectAsync();
            try
            {
                Assert.True(server.IsListening);
                Assert.NotEmpty(ListenersOn(port));
                await AssertClientCanRead(Localhost, port);
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task ServerAndClientCanUseIPv6()
        {
            if (!Socket.OSSupportsIPv6)
            {
                return;
            }

            const int port = 5367;
            Dacs7Server server = new("::1", port, CreateProvider());
            await server.ConnectAsync();
            try
            {
                Assert.Equal(new[] { new IPEndPoint(IPAddress.IPv6Loopback, port) }, ListenersOn(port));
                await AssertClientCanRead($"[::1]", port); // an ipv6 address is written in brackets
            }
            finally
            {
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task MaxConnectionsLimitsTheNumberOfClients()
        {
            const int port = 5368;
            Dacs7Server server = new(port, CreateProvider()) { MaxConnections = 1 };
            await server.ConnectAsync();
            Dacs7Client first = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 2000);
            try
            {
                await first.ConnectAsync();

                Dacs7Client second = new($"{Localhost}:{port},0,1", PlcConnectionType.Pg, 2000);
                await Assert.ThrowsAsync<Dacs7NotConnectedException>(() => second.ConnectAsync());

                // the rejected client did not disturb the accepted one
                Assert.Single(await first.ReadAsync("DB1.0,b,10"));
            }
            finally
            {
                await first.DisconnectAsync();
                await server.DisconnectAsync();
            }
        }

        [Fact]
        public async Task StartingOnAnAlreadyUsedEndpointFails()
        {
            const int port = 5366;
            TcpListener blocker = new(IPAddress.Loopback, port);
            blocker.Start();
            try
            {
                Dacs7Server server = new(Localhost, port, CreateProvider());
                Dacs7NotConnectedException exception = await Assert.ThrowsAsync<Dacs7NotConnectedException>(() => server.ConnectAsync());
                Assert.Contains("already in use", exception.Message, StringComparison.Ordinal);
                Assert.False(server.IsListening);
            }
            finally
            {
                blocker.Stop();
            }
        }

        private static SimulationPlcDataProvider CreateProvider()
        {
            SimulationPlcDataProvider provider = new();
            provider.Register(PlcArea.DB, 100, 1);
            return provider;
        }

        private static IPEndPoint[] ListenersOn(int port)
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Where(ep => ep.Port == port).ToArray();
        }

        private static async Task AssertClientCanRead(string host, int port)
        {
            Dacs7Client client = new($"{host}:{port},0,1", PlcConnectionType.Pg, 5000);
            await client.ConnectAsync();
            try
            {
                Assert.Single(await client.ReadAsync("DB1.0,b,10"));
            }
            finally
            {
                await client.DisconnectAsync();
            }
        }
    }
}
