using Dacs7;
using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Dacs7.Helper;

namespace Dacs7Tests
{
    public class Dacs7ClientTests
    {

        private readonly Dacs7Client _client = new Dacs7Client();
        private readonly Dacs7Client _client2 = new Dacs7Client();
        private const string Ip = "127.0.0.1";//"127.0.0.1";
        private const string ConnectionString = "Data Source=" + Ip + ":102,0,2"; //"Data Source=192.168.0.145:102,0,2";
        private const int TestDbNr = 250;
        private const int TestByteOffset = 524;
        private const int TestByteOffset2 = 525;
        private const int TestBitOffset = 16 * 8; // DBX16.0
        private const int TestBitOffset2 = 16 * 8 + 1; // DBX16.1
        private const int LongDbNumer = 558;



        [Fact]
        public void ConnectionStringTest()
        {
            const string connectionString = "Data Source = " + Ip + ":102,0,2; Allow Parallel Jobs = true; Receive Timeout = 5000; Connection Type = Pg";
            _client.Connect(connectionString);
            Assert.True(_client.IsConnected);
        }

        [Fact]
        public void ConnectTest()
        {
            _client.Connect(ConnectionString);
            Assert.True(_client.IsConnected);
            _client.Disconnect();
            Assert.False(_client.IsConnected);
        }

        [Fact]
        public void ConnectAsyncTest()
        {
            _client.ConnectAsync(ConnectionString).Wait();
            Assert.True(_client.IsConnected);
            _client.DisconnectAsync().Wait();
            Assert.False(_client.IsConnected);
        }

        [Fact]
        public void TestMulti()
        {
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}}
            };

            var writeOperations = new List<WriteOperationParameter>
            {
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
            };

            _client.Connect(ConnectionString);
            _client.WriteAny(writeOperations);
            var result = _client.ReadAnyRaw(operations);
            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }


        [Fact]
        public void TestGeneric()
        {
            _client.Connect(ConnectionString);

            for (int i = 0; i < 8; i++)
            {
                var offset = TestBitOffset + i;

                //Set to false and read
                _client.WriteAny<bool>(TestDbNr, offset, false);
                var boolValue1 = _client.ReadAny<bool>(TestDbNr, offset);

                //Set to true and read
                _client.WriteAny(TestDbNr, offset, true);
                var boolValue2 = _client.ReadAny<bool>(TestDbNr, offset);

                Assert.NotEqual(boolValue1, boolValue2);

                _client.WriteAny<int>(TestDbNr, TestByteOffset, 512);
                var intValue1 = _client.ReadAny<int>(TestDbNr, TestByteOffset);

                _client.WriteAny<int>(TestDbNr, TestByteOffset, i);
                var intValue2 = _client.ReadAny<int>(TestDbNr, TestByteOffset);

                Assert.NotEqual(intValue1, intValue2);
                Assert.Equal(512, intValue1);
                Assert.Equal(i, intValue2);

                _client.WriteAny(TestDbNr, TestByteOffset, "TEST", 4);
                var strValue1 = _client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                var writeVal = i.ToString().PadRight(4, 'X');
                _client.WriteAny(TestDbNr, TestByteOffset, writeVal, 4);
                var strValue2 = _client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                Assert.NotEqual(strValue1, strValue2);
                Assert.Equal("TEST", strValue1);
                Assert.Equal(writeVal, strValue2);

                var firstWriteVal = "TEST".ToCharArray();
                _client.WriteAny(TestDbNr, TestByteOffset, firstWriteVal, 4);
                var charValue1 = _client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                var secondWriteVal = i.ToString().PadRight(4, 'X').ToCharArray();
                _client.WriteAny(TestDbNr, TestByteOffset, secondWriteVal, 4);
                var charValue2 = _client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                Assert.False(charValue1.SequenceEqual(charValue2));
                Assert.True(firstWriteVal.SequenceEqual(charValue1));
                Assert.True(secondWriteVal.SequenceEqual(charValue2));
            }

            _client.Disconnect();

        }

        [Fact]
        public void ReadWriteAnyTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);


            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyDoubleTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);
            _client2.Connect(ConnectionString);
            Assert.Equal(true, _client2.IsConnected);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = _client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = _client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = _client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = _client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);

            _client2.Disconnect();
            Assert.Equal(true, !_client2.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyAsyncTest()
        {
            _client.ConnectAsync(ConnectionString).Wait();
            Assert.Equal(true, _client.IsConnected);

            _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr }).Wait(); ;
            var bytes = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr }).Wait();
            bytes = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr }).Wait();
            bytes = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr }).Wait();
            bytes = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr }).Wait();
            var state = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr }).Wait();
            state = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr }).Wait();
            state = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr }).Wait();
            state = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);


            _client.DisconnectAsync().Wait();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyAsyncDoubleTest()
        {
            var t = new Task[2];
            t[0] = _client.ConnectAsync(ConnectionString);
            t[1] = _client2.ConnectAsync(ConnectionString);

            Task.WaitAll(t);

            Assert.Equal(true, _client.IsConnected);
            Assert.Equal(true, _client2.IsConnected);

            t[0] = _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            t[1] = _client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x05, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            var t2 = new Task<byte[]>[2];
            t2[0] = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = _client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            var bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            t[0] = _client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            t[1] = _client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x00, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = _client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = _client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            t[1] = _client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, true, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = _client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            t[0] = _client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            t[1] = _client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, false, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = _client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = _client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = _client.DisconnectAsync();
            t[1] = _client2.DisconnectAsync();

            Task.WaitAll(t);

            Assert.Equal(true, !_client.IsConnected);
            Assert.Equal(true, !_client2.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var blkInfo = _client.ReadBlockInfo(PlcBlockType.Db, TestDbNr);
            Assert.Equal(TestDbNr, blkInfo.BlockNumber);

            blkInfo = _client.ReadBlockInfo(PlcBlockType.Sdb, 1000);
            Assert.Equal(TestDbNr, 1001);

            blkInfo = _client.ReadBlockInfo(PlcBlockType.Db, 250);
            Assert.Equal(250, blkInfo.BlockNumber);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfo2Test()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var blkInfo = _client.UploadPlcBlock(PlcBlockType.Db, TestDbNr);

            blkInfo = _client.UploadPlcBlock(PlcBlockType.Db, 250);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoFromSdbTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var blkInfo = _client.ReadBlockInfo(PlcBlockType.Sdb, 0);
            Assert.Equal(0, blkInfo.BlockNumber);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoNoExistingTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var blkInfo = _client.ReadBlockInfo(PlcBlockType.Db, 9999);
            Assert.Equal(9999, blkInfo.BlockNumber);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadPendingAlarmsTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var alarms = _client.ReadPendingAlarms();

            foreach (var alm in alarms)
            {
                var ts = alm.Timestamp;
                var i = alm.Id;
                var c = alm.IsComing;
                var sc = alm.IsAck;
            }

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }



        [Fact]
        public void ReadBlockInfoAsyncTest()
        {
            _client.ConnectAsync(ConnectionString).Wait();
            Assert.Equal(true, _client.IsConnected);


            _client.ConnectAsync(ConnectionString).Wait();
            var blkInfo = _client.ReadBlockInfoAsync(PlcBlockType.Db, TestDbNr).Result;
            Assert.Equal(TestDbNr, blkInfo.BlockNumber);


            _client.DisconnectAsync().Wait();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadPendingAlarmsAsyncTest()
        {
            _client.ConnectAsync(ConnectionString).Wait();
            Assert.Equal(true, _client.IsConnected);

            var alarms = _client.ReadPendingAlarmsAsync().Result;

            _client.DisconnectAsync().Wait();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void ReadWriteMoreThanOnePduTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0xFF;

            var sw = new Stopwatch();
            sw.Start();
            _client.WriteAny(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = _client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

        }

        [Fact]
        public void ReadWriteMoreThanOnePduParallelTest()
        {

            //_client.OnLogEntry += Console.WriteLine;
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0x01;

            var sw = new Stopwatch();

            sw.Start();
            _client.WriteAnyParallel(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = _client.ReadAnyParallel(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

        }

        [Fact]
        public void ReadNotExistingItem()
        {

            //_client.OnLogEntry += Console.WriteLine;
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            const int length = ushort.MaxValue;
            Assert.Throws<Exception>(() => _client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }));
        }

        [Fact]
        public void RegisterAlarmUpdateCallbackTest()
        {
            _client.ConnectAsync(ConnectionString).Wait();
            Assert.Equal(true, _client.IsConnected);

            var alarmID = _client.RegisterAlarmUpdateCallback((alarm) =>
            {
                var numberOfalarms = alarm.CountAlarms;
            });

            Thread.Sleep(10000);

            _client.UnregisterAlarmUpdate(alarmID);

            _client.DisconnectAsync().Wait();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void Convertest()
        {
            Single s = (Single)5.4;

            var b = s.SetNoSwap();
            var c = b.GetNoSwap<Single>();


            var d = s.SetSwap();
            var e = d.GetSwap<Single>();
        }

        [Fact]
        public void GetBlocksCountTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var bc = _client.GetBlocksCount();

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var bc = _client.GetBlocksOfType(PlcBlockType.Ob);

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest2()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var bc = _client.GetBlocksOfType(PlcBlockType.Sdb);

            foreach (var c in bc)
            {
                Console.WriteLine(c.Number);
            }

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }

        [Fact]
        public void GetPlcTimeTest()
        {
            _client.Connect(ConnectionString);
            Assert.Equal(true, _client.IsConnected);

            var bc = _client.GetPlcTime();

            _client.Disconnect();
            Assert.Equal(true, !_client.IsConnected);
        }
    }
}
