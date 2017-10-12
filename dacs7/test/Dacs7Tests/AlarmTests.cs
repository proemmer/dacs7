
#define REAL_PLC
using Dacs7;
using Dacs7.Domain;
using Microsoft.Extensions.Logging;
using System.Threading;
using Xunit;

namespace Dacs7Tests
{

#if REAL_PLC
    public class AlarmTests
    {
        private ILoggerFactory _loggerFactory = new LoggerFactory().AddConsole();
        private const string Ip = "192.168.0.148";
        public const string ConnectionString = "Data Source=" + Ip + ":102,0,2;Connect Timeout=10000"; //"Data Source=192.168.1.10:102,0,2";

        [Fact]
        public void RegisterAlarmUpdateCallbackTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);

            var alarmID = client.RegisterAlarmUpdateCallback((alarm) =>
            {
                var numberOfalarms = alarm.CountAlarms;
            });

            Thread.Sleep(10000);

            client.UnregisterAlarmUpdate(alarmID);

            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }



        [Fact]
        public void ReadPendingAlarmsAsyncTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);

            var alarms = client.ReadPendingAlarmsAsync().Result;

            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfo2Test()
        {
            var db = 250;
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.UploadPlcBlock(PlcBlockType.Db, db);

            blkInfo = client.UploadPlcBlock(PlcBlockType.Db, db);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoFromSdbTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.ReadBlockInfo(PlcBlockType.Sdb, 0);
            Assert.Equal(0, blkInfo.BlockNumber);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoNoExistingTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            Assert.Throws<Dacs7ParameterException>( () => client.ReadBlockInfo(PlcBlockType.Db, 9999));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadPendingAlarmsTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var alarms = client.ReadPendingAlarms();

            foreach (var alm in alarms)
            {
                var ts = alm.Timestamp;
                var i = alm.Id;
                var c = alm.IsComing;
                var sc = alm.IsAck;
            }

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


    }

#endif
}
