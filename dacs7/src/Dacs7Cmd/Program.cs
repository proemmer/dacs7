using Dacs7;
using Dacs7.Domain;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Dacs7Cmd
{
    public class Program
    {
        private static readonly Dacs7Client _client = new Dacs7Client();
        //private const string Ip = "192.168.0.147";
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
            _client.Connect(ConnectionString);
            var red = _client.ReadAny(PlcArea.DB, 0, typeof(int), new[] { 2, LongDbNumer });
            red = _client.ReadAny<bool>(LongDbNumer, 0, 2);
            _client.WriteAny(LongDbNumer, 0, new bool[] { true });
            red = _client.ReadAny<int>(LongDbNumer, 0,2);
            _client.WriteAny(LongDbNumer, 0, new int[] { 1, 2 });
            ReadWriteAnyTest(_client);
            ReadWriteMoreThanOnePduTest();
            ReadWriteMoreThanOnePduParallelTest();
            _client.Disconnect();
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

        }

        public static void ReadWriteAnyTest(Dacs7Client client)
        {
            client.Connect(ConnectionString);
            //Assert.AreEqual(true, _client.IsConnected);

            client.WriteAny(TestDbNr, TestByteOffset, (byte)0x05);
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            client.WriteAny(TestDbNr, TestByteOffset, (byte)0x00);
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);


            client.Disconnect();
            //Assert.AreEqual(true, !_client.IsConnected);
        }

        public static void ReadWriteAnyTest2(Dacs7Client client)
        {
            client.Connect(ConnectionString);
            //Assert.AreEqual(true, _client.IsConnected);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            var bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x05, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x05, bytes[0]);

            client.WriteAny(PlcArea.DB, TestByteOffset, (byte)0x00, new int[] { 1, TestDbNr });
            bytes = client.ReadAny(PlcArea.DB, TestByteOffset, typeof(byte), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(bytes);
            //Assert.AreEqual((byte)0x00, bytes[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            var state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, true, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x01, state[0]);

            client.WriteAny(PlcArea.DB, TestBitOffset, false, new int[] { 1, TestDbNr });
            state = client.ReadAny(PlcArea.DB, TestBitOffset, typeof(bool), new int[] { 1, TestDbNr }) as byte[];
            //Assert.IsNotNull(state);
            //Assert.AreEqual((byte)0x00, state[0]);


            client.Disconnect();
            //Assert.AreEqual(true, !_client.IsConnected);
        }
    }
}
