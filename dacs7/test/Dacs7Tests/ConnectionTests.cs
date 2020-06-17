using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    public class ConnectionTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(150)]
        [InlineData(1000)]
        [InlineData(2000)]
        public async Task ConnectToPlcButAbortBeforRFC1006Finished(int sleepTime)
        {

            static async Task RunPlcSimu(int delay)
            {
                var listener = new TcpListener(IPAddress.Loopback, 1999);
                listener.Start();

                var client = listener.AcceptTcpClient();
                await Task.Delay(delay);
                client.Close();
                listener.Stop();
            }


            _ = Task.Run(() => RunPlcSimu(sleepTime));

            var client = new Dacs7Client("127.0.0.1:1999", PlcConnectionType.Basic, 5000)
            {
                PduSize = 960
            };

            await Assert.ThrowsAsync<Dacs7NotConnectedException>(async () => await client.ConnectAsync());

        }

        [Theory]
        [InlineData(100)]
        public async Task ConnectToPlcButAbortAfterRFC1006Finished(int sleepTime)
        {

            static async Task RunPlcSimu(int delay)
            {
                var listener = new TcpListener(IPAddress.Loopback, 1999);
                listener.Start();

                var client = listener.AcceptTcpClient();
                var buffer = new byte[50];
                using (NetworkStream stream = client.GetStream())
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);

                    var sendData = new byte[] { 0x03, 0x00, 0x00, 0x16, 0x11, 0xd0, 0x00, 0x01, 0x08, 0x5c, 0x00, 0xc0, 0x01, 0x0a, 0xc1, 0x02, 0x01, 0x00, 0xc2, 0x02, 0x01, 0x02 };
                    await stream.WriteAsync(sendData, 0, sendData.Length);


                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    sendData = new byte[] { 0x03, 0x00, 0x00, 0x1b, 0x02, 0xf0, 0x80, 0x32, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0xf0, 0x00, 0x00, 0x05, 0x00, 0x05, 0x01, 0xe0 };
                    await stream.WriteAsync(sendData, 0, sendData.Length);

                    await Task.Delay(delay);
                }

                client.Close();
                listener.Stop();
            }


            _ = Task.Run(() => RunPlcSimu(sleepTime));

            var client = new Dacs7Client("127.0.0.1:1999", PlcConnectionType.Basic, 5000)
            {
                PduSize = 960
            };

            await client.ConnectAsync();
            Assert.True(client.IsConnected, "Not Connected!");

            await Task.Delay(sleepTime * 2);
            Assert.False(client.IsConnected, "Not Disconnected!");

            _ = Task.Run(() => RunPlcSimu(10000));

            await Task.Delay(5000);
            Assert.True(client.IsConnected, "Not Reconnected!");

            await client.DisconnectAsync();
            Assert.False(client.IsConnected, "Not Disconnected!");
        }
    }
}
