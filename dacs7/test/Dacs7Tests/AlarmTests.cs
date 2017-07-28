using Dacs7;
using Dacs7.Domain;
using System.Threading;
using Xunit;

namespace Dacs7Tests
{

#if REAL_PLC
    public class AlarmTests
    {

        [Fact]
        public void RegisterAlarmUpdateCallbackTest()
        {
            var client = new Dacs7Client();
            client.ConnectAsync(Dacs7ClientTests.ConnectionString).Wait();
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
            var client = new Dacs7Client();
            client.ConnectAsync(Dacs7ClientTests.ConnectionString).Wait();
            Assert.True(client.IsConnected);

            var alarms = client.ReadPendingAlarmsAsync().Result;

            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfo2Test()
        {
            var db = 250;
            var client = new Dacs7Client();
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.UploadPlcBlock(PlcBlockType.Db, db);

            blkInfo = client.UploadPlcBlock(PlcBlockType.Db, db);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoFromSdbTest()
        {
            var client = new Dacs7Client();
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.ReadBlockInfo(PlcBlockType.Sdb, 0);
            Assert.Equal(0, blkInfo.BlockNumber);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoNoExistingTest()
        {
            var client = new Dacs7Client();
            client.Connect(Dacs7ClientTests.ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.ReadBlockInfo(PlcBlockType.Db, 9999);
            Assert.Equal(9999, blkInfo.BlockNumber);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadPendingAlarmsTest()
        {
            var client = new Dacs7Client();
            client.Connect(Dacs7ClientTests.ConnectionString);
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
