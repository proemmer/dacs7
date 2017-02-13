using Dacs7;
using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Dacs7Cmd
{
    public class Program
    {
        private static readonly Dacs7Client _client = new Dacs7Client();
        //private const string Ip = "192.168.0.146";
        private const string Ip = "127.0.0.1";
        private const string ConnectionString = "Data Source=" + Ip + ":102,0,2"; //"Data Source=192.168.0.145:102,0,2";
        private const int TestDbNr = 250;
        private const int TestByteOffset = 524;
        private const int TestByteOffset2 = 525;
        private const int TestBitOffset = 16 * 8; // DBX16.0
        private const int TestBitOffset2 = 16 * 8 + 1; // DBX16.1
        private const int LongDbNumer = 560;

        public static void Main(string[] args)
        {
            _client.OnConnectionChange += _client_OnConnectionChange;
            _client.Connect(ConnectionString);

            GenericsSample();
            MultiValuesSample();

            var red = _client.ReadAny(PlcArea.DB, 0, typeof(int), new[] { 2, LongDbNumer });
            var boolValue = _client.ReadAny<bool>(LongDbNumer, 0, 2);


            _client.WriteAny(LongDbNumer, 0, new bool[] { true, true });


            var intValue = _client.ReadAny<int>(LongDbNumer, 0,2);
            _client.WriteAny(LongDbNumer, 0, new int[] { 1, 2 });


            ReadWriteAnyTest();
            ReadWriteMoreThanOnePduTest();
            ReadWriteMoreThanOnePduParallelTest();



            _client.Disconnect();

            Thread.Sleep(1000);
        }

        public static void GenericsSample()
        {
            var boolValue = _client.ReadAny<bool>(TestDbNr, TestBitOffset);
            var intValue = _client.ReadAny<int>(TestDbNr, TestByteOffset);

            const int numberOfArrayElements = 2;
            //var boolEnumValue = _client.ReadAny<bool>(TestDbNr, TestBitOffset, numberOfArrayElements);
            var intEnumValue = _client.ReadAny<int>(TestDbNr, TestByteOffset, numberOfArrayElements);
        }


        public static void MultiValuesSample()
        {
            var operations = new List<ReadOperationParameter>
            {
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}},
                new ReadOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}}
            };

            var result = _client.ReadAny(operations); //result is IEnumerable<byte[]>

            var writeOperations = new List<WriteOperationParameter>
            {
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestByteOffset, Type=typeof(byte), Args = new int[]{1, TestDbNr}, Data = (byte)0x05},
                new WriteOperationParameter{Area = PlcArea.DB, Offset= TestBitOffset, Type=typeof(bool), Args = new int[]{1, TestDbNr}, Data = true}
            };

            _client.WriteAny(writeOperations);

        }

        private static void _client_OnConnectionChange(object sender, PlcConnectionNotificationEventArgs e)
        {
            Console.WriteLine($"ConnectionState: {e.From} = {e.IsConnected}");
        }

        public static void ReadWriteMoreThanOnePduTest()
        {

            //Assert.AreEqual(true, _client.IsConnected);

            const int length = 240;

            var testData = new byte[length];
            for (var i = 0; i < testData.Length; i++)
                testData[i] = 0xFF;

            var sw = new Stopwatch();
            sw.Start();
            //_client.WriteAny(PlcArea.DB, 0, testData, new[] { length, LongDbNumer });
            _client.WriteAny(LongDbNumer, 0, testData);
            sw.Stop();
            Console.WriteLine("Write time: {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            var red = _client.ReadAny(PlcArea.DB, 0, typeof(byte), new[] { length, LongDbNumer }) as byte[];
            sw.Stop();
            Console.WriteLine("Read time: {0}ms", sw.ElapsedMilliseconds);

            //Assert.IsNotNull(red);
            //Assert.IsTrue(testData.SequenceEqual(red));

        }

        public static void ReadWriteMoreThanOnePduParallelTest()
        {

            //_client.OnLogEntry += Console.WriteLine;
            //_client.Connect(ConnectionString);
            //Assert.AreEqual(true, _client.IsConnected);

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

            //Assert.IsNotNull(red);
            //Assert.IsTrue(testData.SequenceEqual(red));
            //client.Disconnect();
            

        }

        public static void ReadWriteAnyTest()
        {
            //client.Connect(ConnectionString);
            //Assert.AreEqual(true, _client.IsConnected);

            _client.WriteAny(TestDbNr, TestByteOffset, (byte)0x05);
            var bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            _client.WriteAny(TestDbNr, TestByteOffset, (byte)0x00);
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);


            //client.Disconnect();
            //Assert.AreEqual(true, !_client.IsConnected);
        }

        public static void ReadWriteAnyTest2()
        {
            //client.Connect(ConnectionString);
            //Assert.AreEqual(true, _client.IsConnected);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = _client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            _client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = _client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);


            //client.Disconnect();
            //Assert.AreEqual(true, !_client.IsConnected);
        }
    }
}
