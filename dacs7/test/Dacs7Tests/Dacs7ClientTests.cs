#define TEST_PLC

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
using Dacs7.Protocols.S7;

namespace Dacs7Tests
{



#if TEST_PLC

    public class Dacs7ClientTests
    {
        //private const string Ip = "127.0.0.1";//"127.0.0.1";
        private const string Ip = "192.168.0.148";
        //private const string Ip = "192.168.1.17";//"127.0.0.1";
        //private const string ConnectionString = "Data Source=" + Ip + ":102,0,2;PduSize=240"; //"Data Source=192.168.1.10:102,0,2";
        private const string ConnectionString = "Data Source=" + Ip + ":102,0,2;Connect Timeout=10000"; //"Data Source=192.168.1.10:102,0,2";
        private const int TestDbNr = 250;
        private const int TestByteOffset = 524;
        private const int TestByteOffset2 = 525;
        private const int TestBitOffset = 16 * 8; // DBX16.0
        private const int TestBitOffset2 = 16 * 8 + 1; // DBX16.1
        private const int LongDbNumer = 558;


        public Dacs7ClientTests()
        {
            //Manually instantiate all Ack types, because we have a different executing assembly in the test framework and so this will not be done automatically
            new S7AckDataProtocolPolicy();
            new S7ReadJobAckDataProtocolPolicy();
            new S7WriteJobAckDataProtocolPolicy();
        }


        [Fact]
        public void ConnectionStringTest()
        {
            var client = new Dacs7Client();
            const string connectionString = "Data Source = " + Ip + ":102,0,2";
            client.Connect(connectionString);
            Assert.True(client.IsConnected);
        }

        [Fact]
        public void ConnectTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ConnectAsyncTest()
        {
            var client = new Dacs7Client();
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);
            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadAnyRaw()
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
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            client.WriteAny(writeOperations);
            var result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count, result.Count());
            Assert.Equal((byte)0x05, result.First()[0]);
            Assert.Equal((byte)0x01, result.First()[0]);
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadAny()
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
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            client.WriteAny(writeOperations);
            var result = client.ReadAny(operations);
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public async Task TestReadWriteAnyAsyncBigData()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            await client.WriteAnyAsync(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            var result = await client.ReadAnyAsync(PlcArea.DB, 0, typeof(byte[]),new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            await client.WriteAnyAsync(PlcArea.DB, 0, buffer, new [] { length, TestDbNr });
            result = await client.ReadAnyAsync(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public async Task TestMultipleReadWriteAnyAsyncBigData()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            await client.WriteAnyAsync(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            var result = await client.ReadAnyAsync(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            await client.WriteAnyAsync(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            result = await client.ReadAnyAsync(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


        [Fact]
        public void TestReadWriteAnyBigData()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            client.WriteAny(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            var result =  client.ReadAny(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            client.WriteAny(PlcArea.DB, 0, buffer, new[] { length, TestDbNr });
            result =  client.ReadAny(PlcArea.DB, 0, typeof(byte[]), new[] { length, TestDbNr });
            Assert.True(buffer.SequenceEqual(result));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestMultipleReadWriteAnyBigData()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            var length = 6534;
            var buffer = new byte[length];

            //Write 0
            client.WriteAny(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            var result =  client.ReadAny(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            for (int i = 0; i < length; i++)
                buffer[i] = ((i % 2) == 0) ? (byte)0x05 : (byte)0x06;

            client.WriteAny(new List<WriteOperationParameter> { WriteOperationParameter.Create(TestDbNr, 0, buffer) });
            result =  client.ReadAny(new List<ReadOperationParameter> { ReadOperationParameter.Create<byte>(TestDbNr, 0, length) });
            Assert.True(buffer.SequenceEqual(result.First() as byte[]));

            client.Disconnect();
            Assert.False(client.IsConnected);
        }



        [Fact]
        public void TestaBunchOfMultiReads()
        {
            var db = 10;
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= 0, Type=typeof(byte), Args = new int[]{1, db}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= 1, Type=typeof(bool), Args = new int[]{1, db}},
            };

            for (int i = 0; i < 100; i++)
            {
                operations.Add(new ReadOperationParameter { Area = PlcArea.DB, Offset = 1 + i, Type = typeof(bool), Args = new int[] { 1, db } });
            }

            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            var result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count(), result.Count());

            operations.RemoveAt(0);
            result = client.ReadAnyRaw(operations);
            Assert.Equal(operations.Count(), result.Count());
            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void TestaBunchOfMultiWrites()
        {
            var db = 11;
            var operations = new List<WriteOperationParameter>();
            var readOperations = new List<ReadOperationParameter>();


            for (int i = 0; i < 100; i++)
            {
                operations.Add(new WriteOperationParameter { Area = PlcArea.DB, Offset = i, Type = typeof(bool), Args = new int[] { 1, db }, Data = false });
                readOperations.Add(new ReadOperationParameter { Area = PlcArea.DB, Offset = i, Type = typeof(bool), Args = new int[] { 1, db } });
            }

            var client = new Dacs7Client();
            client.Connect(ConnectionString);


            //Reset to false
            client.WriteAny(operations);
            var result = client.ReadAny(readOperations).ToList();
            for (int i = 0; i < operations.Count; i++)
            {
                operations[i].Data = !((bool)result[i]);
            }

            client.WriteAny(operations);
            result = client.ReadAny(readOperations).ToList();
            for (int i = 0; i < operations.Count; i++)
            {
                Assert.Equal((bool)operations[i].Data, ((bool)result[i]));
            }


            operations.RemoveAt(0);
            client.WriteAny(operations);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


        [Fact]
        public void TestGeneric()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);

            for (int i = 0; i < 8; i++)
            {
                var offset = TestBitOffset + i;

                //Set to false and read
                client.WriteAny<bool>(TestDbNr, offset, false);
                var boolValue1 = client.ReadAny<bool>(TestDbNr, offset);

                //Set to true and read
                client.WriteAny(TestDbNr, offset, true);
                var boolValue2 = client.ReadAny<bool>(TestDbNr, offset);

                Assert.NotEqual(boolValue1, boolValue2);

                client.WriteAny<int>(TestDbNr, TestByteOffset, 512);
                var intValue1 = client.ReadAny<int>(TestDbNr, TestByteOffset);

                client.WriteAny<int>(TestDbNr, TestByteOffset, i);
                var intValue2 = client.ReadAny<int>(TestDbNr, TestByteOffset);

                Assert.NotEqual(intValue1, intValue2);
                Assert.Equal(512, intValue1);
                Assert.Equal(i, intValue2);

                client.WriteAny(TestDbNr, TestByteOffset, "TEST", 4);
                var strValue1 = client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                var writeVal = i.ToString().PadRight(4, 'X');
                client.WriteAny(TestDbNr, TestByteOffset, writeVal, 4);
                var strValue2 = client.ReadAny<string>(TestDbNr, TestByteOffset, 4)?.FirstOrDefault();

                Assert.NotEqual(strValue1, strValue2);
                Assert.Equal("TEST", strValue1);
                Assert.Equal(writeVal, strValue2);

                var firstWriteVal = "TEST".ToCharArray();
                client.WriteAny(TestDbNr, TestByteOffset, firstWriteVal, 4);
                var charValue1 = client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                var secondWriteVal = i.ToString().PadRight(4, 'X').ToCharArray();
                client.WriteAny(TestDbNr, TestByteOffset, secondWriteVal, 4);
                var charValue2 = client.ReadAny<char>(TestDbNr, TestByteOffset, 4);

                Assert.False(charValue1.SequenceEqual(charValue2));
                Assert.True(firstWriteVal.SequenceEqual(charValue1));
                Assert.True(secondWriteVal.SequenceEqual(charValue2));
            }

            client.Disconnect();

        }

        [Fact]
        public void ReadWriteAnyTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);


            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyDoubleTest()
        {
            var client = new Dacs7Client();
            var client2 = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);
            client2.Connect(ConnectionString);
            Assert.Equal(true, client2.IsConnected);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x04, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x04, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client2.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x01, new int[] { 1, TestDbNr });
            bytes = client2.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client2.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client2.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.Disconnect();
            Assert.False(client.IsConnected);

            client2.Disconnect();
            Assert.False(client2.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyAsyncTest()
        {
            var client = new Dacs7Client();
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);

            client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr }).Wait(); ;
            var bytes = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr }).Wait();
            bytes = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr }).Wait();
            bytes = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr }).Wait();
            bytes = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr }).Wait();
            var state = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr }).Wait();
            state = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);

            client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr }).Wait();
            state = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x01, state[0]);

            client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr }).Wait();
            state = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }).Result as byte[];
            Assert.NotNull(state);
            Assert.Equal((byte)0x00, state[0]);


            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadWriteAnyAsyncDoubleTest()
        {
            var client = new Dacs7Client();
            var client2 = new Dacs7Client();
            var t = new Task[2];
            t[0] = client.ConnectAsync(ConnectionString);
            t[1] = client2.ConnectAsync(ConnectionString);

            Task.WaitAll(t);

            Assert.True(client.IsConnected);
            Assert.Equal(true, client2.IsConnected);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x05, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            var t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            var bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x05, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestByteOffset2, (byte)0x00, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestByteOffset2, typeof(byte), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, true, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t2);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x01, bytes[0]);

            t[0] = client.WriteAnyAsync(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            t[1] = client2.WriteAnyAsync(PlcArea.DB, TestBitOffset2, false, new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            t2 = new Task<byte[]>[2];
            t2[0] = client.ReadAnyAsync(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr });
            t2[1] = client2.ReadAnyAsync(PlcArea.DB, TestBitOffset2, typeof(bool), new int[] { 1, TestDbNr });

            Task.WaitAll(t);

            bytes = t2[0].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            bytes = t2[1].Result as byte[];
            Assert.NotNull(bytes);
            Assert.Equal((byte)0x00, bytes[0]);

            t[0] = client.DisconnectAsync();
            t[1] = client2.DisconnectAsync();

            Task.WaitAll(t);

            Assert.False(client.IsConnected);
            Assert.False(client2.IsConnected);
        }




        [Fact]
        public void ReadBlockInfoAsyncTest()
        {
            var client = new Dacs7Client();
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);


            client.ConnectAsync(ConnectionString).Wait();
            var blkInfo = client.ReadBlockInfoAsync(PlcBlockType.Db, TestDbNr).Result;
            Assert.Equal(TestDbNr, blkInfo.BlockNumber);


            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }



        [Fact]
        public void ReadWriteMoreThanOnePduTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0xFF;

            var sw = new Stopwatch();
            sw.Start();
            client.WriteAny(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

            client.Disconnect();
            Assert.False(client.IsConnected);

        }

        [Fact]
        public void ReadWriteMoreThanOnePduParallelTest()
        {
            var client = new Dacs7Client();
            //client.OnLogEntry += Console.WriteLine;
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = 24000;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0x01;

            var sw = new Stopwatch();

            sw.Start();
            client.WriteAnyParallel(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = client.ReadAnyParallel(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            Assert.NotNull(red);
            Assert.True(testData.SequenceEqual(red));

            client.Disconnect();
            Assert.False(client.IsConnected);

        }

        [Fact]
        public void ReadNotExistingItem()
        {
            var client = new Dacs7Client();
            //client.OnLogEntry += Console.WriteLine;
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            const int length = ushort.MaxValue;
            Assert.Throws<Dacs7ContentException>(() => client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }));

            client.Disconnect();
            Assert.False(client.IsConnected);
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
        public void GetPlcTimeTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetPlcTime();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }


#if REAL_PLC

        
        [Fact]
        public void RegisterAlarmUpdateCallbackTest()
        {
            var client = new Dacs7Client();
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
        public void GetBlocksCountTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksCount();

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksOfType(PlcBlockType.Ob);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void GetBlocksOfTypeTest2()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var bc = client.GetBlocksOfType(PlcBlockType.Sdb);

            foreach (var c in bc)
            {
                Console.WriteLine(c.Number);
            }

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        
        [Fact]
        public void ReadBlockInfoTest()
        {
            var client = new Dacs7Client();

            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.ReadBlockInfo(PlcBlockType.Db, TestDbNr);
            Assert.Equal(TestDbNr, blkInfo.BlockNumber);

            blkInfo = client.ReadBlockInfo(PlcBlockType.Sdb, 1000);
            Assert.Equal(TestDbNr, 1001);

            blkInfo = client.ReadBlockInfo(PlcBlockType.Db, 250);
            Assert.Equal(250, blkInfo.BlockNumber);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadPendingAlarmsAsyncTest()
        {
            var client = new Dacs7Client();
            client.ConnectAsync(ConnectionString).Wait();
            Assert.True(client.IsConnected);

            var alarms = client.ReadPendingAlarmsAsync().Result;

            client.DisconnectAsync().Wait();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfo2Test()
        {
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
            Assert.True(client.IsConnected);

            var blkInfo = client.UploadPlcBlock(PlcBlockType.Db, TestDbNr);

            blkInfo = client.UploadPlcBlock(PlcBlockType.Db, 250);

            client.Disconnect();
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void ReadBlockInfoFromSdbTest()
        {
            var client = new Dacs7Client();
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
            var client = new Dacs7Client();
            client.Connect(ConnectionString);
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

#endif


    }

#endif
}
