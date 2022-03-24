using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class ConnectionTests
    {

        public enum PlcShutdonMode
        {
            StopAfterConnect,
            StopAfterCrReceived,
            StopAfterCrReceivedAndCcWasResponded,
            StopAfterPlcSetupReceived,
            StopAfterPlcSetupWasReceivedAndResponded,
            StopAfterFirstMessageAfterSetup
        }




        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(150)]
        [InlineData(1000)]
        [InlineData(2000)]
        [InlineData(2500)]
        [InlineData(5000)]
        [InlineData(4995)]
        [InlineData(5010)]
        public async Task ServerDiconnectedAfterConnectTest(int sleepTime)
        {
            _ = Task.Run(() => RunPlcSimu(1999, sleepTime, PlcShutdonMode.StopAfterConnect));
            Dacs7Client client = new("127.0.0.1:1999", PlcConnectionType.Basic, 500)
            {
                PduSize = 960
            };
            await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(150)]
        [InlineData(1000)]
        [InlineData(2000)]
        [InlineData(2500)]
        [InlineData(5000)]
        [InlineData(4995)]
        [InlineData(5010)]
        public async Task ServerDisconnectedAfterFirstMessageReceivedTest(int sleepTime)
        {
            _ = Task.Run(() => RunPlcSimu(1998, sleepTime, PlcShutdonMode.StopAfterCrReceived));
            Dacs7Client client = new("127.0.0.1:1998", PlcConnectionType.Basic, 500)
            {
                PduSize = 960
            };
            await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());

        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(150)]
        [InlineData(1000)]
        [InlineData(2000)]
        [InlineData(2500)]
        [InlineData(5000)]
        [InlineData(4995)]
        [InlineData(5010)]
        public async Task ServerDisconnectedAfterCCResponseTest(int sleepTime)
        {
            _ = Task.Run(() => RunPlcSimu(1997, sleepTime, PlcShutdonMode.StopAfterCrReceivedAndCcWasResponded));
            Dacs7Client client = new("127.0.0.1:1997", PlcConnectionType.Basic, 500)
            {
                PduSize = 960
            };
            await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(150)]
        [InlineData(1000)]
        [InlineData(2000)]
        [InlineData(2500)]
        [InlineData(5000)]
        [InlineData(4995)]
        [InlineData(5010)]
        public async Task ServerDisconnectAftePlcSetupReceivedTest(int sleepTime)
        {
            _ = Task.Run(() => RunPlcSimu(1996, sleepTime, PlcShutdonMode.StopAfterPlcSetupReceived));
            Dacs7Client client = new("127.0.0.1:1996", PlcConnectionType.Basic, 500)
            {
                PduSize = 960
            };
            await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());
        }




        [Theory]
        [InlineData(5, 3)]
        [InlineData(10, 3)]
        [InlineData(100, 3)]
        public async Task ServerDisconnectAfterSucessfullyConnectedAndWaitingForManualReconnectTest(int sleepTime, int loops)
        {
            Dacs7Client client = new("127.0.0.1:1995", PlcConnectionType.Basic, 500)
            {
                PduSize = 960
            };

            for (int i = 0; i < loops; i++)
            {
                _ = Task.Run(() => RunPlcSimu(1995, sleepTime, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

                await client.ConnectAsync();
                Assert.True(client.IsConnected, "Not Connected!");

                await Task.Delay((sleepTime * 2) + 100);
                Assert.False(client.IsConnected, "Not Disconnected!");

                _ = Task.Run(() => RunPlcSimu(1995, sleepTime, PlcShutdonMode.StopAfterCrReceived));

                await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());
                Assert.False(client.IsConnected, "Not Connected!");


                _ = Task.Run(() => RunPlcSimu(1995, sleepTime, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

                await client.ConnectAsync();
                Assert.True(client.IsConnected, "Not Connected!");

                await Task.Delay((sleepTime * 2) + 100);
                Assert.False(client.IsConnected, "Not Disconnected!");

                await client.DisconnectAsync();
                Assert.False(client.IsConnected, "Not Disconnected!");
            }
        }


        [Theory]
        [InlineData(100)]
        public async Task ServerDisconnectAfterSucessfullyConnectedAndWaitingForAutoReconnectTest(int sleepTime)
        {
            // simaulate plc disconnect after sleeptime
            _ = Task.Run(() => RunPlcSimu(1994, sleepTime, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

            Dacs7Client client = new("127.0.0.1:1994", PlcConnectionType.Basic, 500, null, 500)
            {
                PduSize = 960
            };

            await client.ConnectAsync();
            Assert.True(client.IsConnected, "Not Connected!");

            await Task.Delay(sleepTime * 2);
            Assert.False(client.IsConnected, "Not Disconnected!");

            _ = Task.Run(() => RunPlcSimu(1994, 2000, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

            await Task.Delay(500); // wait for autoReconnect
            Assert.True(client.IsConnected, "Not Reconnected!");

            await client.DisconnectAsync();
            Assert.False(client.IsConnected, "Not Disconnected!");
        }

        [Theory]
        [InlineData(1, 3)]
        [InlineData(10, 3)]
        [InlineData(100, 3)]
        public async Task AutoReconnectTest(int sleepTime, int loops)
        {
            Dacs7Client client = new("127.0.0.1:1993", PlcConnectionType.Basic, 5000)
            {
                PduSize = 960
            };


            for (int i = 0; i < loops; i++)
            {
                _ = Task.Run(() => RunPlcSimu(1993, sleepTime, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

                await client.ConnectAsync();
                Assert.True(client.IsConnected, "Not Connected!");

                await Task.Delay((sleepTime * 2) + 100);
                Assert.False(client.IsConnected, "Not Disconnected!");

                _ = Task.Run(() => RunPlcSimu(1993, sleepTime, PlcShutdonMode.StopAfterCrReceived));

                await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());
                Assert.False(client.IsConnected, "Not Connected!");


                _ = Task.Run(() => RunPlcSimu(1993, sleepTime, PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded));

                await client.ConnectAsync();
                Assert.True(client.IsConnected, "Not Connected!");

                await Task.Delay((sleepTime * 2) + 100);
                Assert.False(client.IsConnected, "Not Disconnected!");



                await client.DisconnectAsync();
                Assert.False(client.IsConnected, "Not Disconnected!");
            }

        }


        private static async Task RunPlcSimu(int port, int delay, PlcShutdonMode endMode = 0)
        {
            TcpListener listener = new(IPAddress.Loopback, port);
            listener.Start();

            TcpClient client = listener.AcceptTcpClient();
            byte[] buffer = new byte[50];
            using (NetworkStream stream = client.GetStream())
            {
                if (endMode != PlcShutdonMode.StopAfterConnect)
                {
                    if ((int)endMode >= (int)PlcShutdonMode.StopAfterCrReceived)
                    {
                        // CR, CC
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                        if ((int)endMode >= (int)PlcShutdonMode.StopAfterCrReceivedAndCcWasResponded)
                        {
                            byte[] sendData = new byte[] { 0x03, 0x00, 0x00, 0x16, 0x11, 0xd0, 0x00, 0x01, 0x08, 0x5c, 0x00, 0xc0, 0x01, 0x0a, 0xc1, 0x02, 0x01, 0x00, 0xc2, 0x02, 0x01, 0x02 };
                            await stream.WriteAsync(sendData, 0, sendData.Length);
                        }
                    }
                    if ((int)endMode >= (int)PlcShutdonMode.StopAfterPlcSetupReceived)
                    {
                        // PlcSetup
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                        if ((int)endMode >= (int)PlcShutdonMode.StopAfterPlcSetupWasReceivedAndResponded)
                        {
                            byte[] sendData = new byte[] { 0x03, 0x00, 0x00, 0x1b, 0x02, 0xf0, 0x80, 0x32, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0xf0, 0x00, 0x00, 0x05, 0x00, 0x05, 0x01, 0xe0 };
                            await stream.WriteAsync(sendData, 0, sendData.Length);
                        }
                    }
                    if ((int)endMode >= (int)PlcShutdonMode.StopAfterFirstMessageAfterSetup)
                    {
                        // PlcSetup
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                    }
                }

                await Task.Delay(delay);
            }

            client.Close();
            listener.Stop();
        }
    }
}
