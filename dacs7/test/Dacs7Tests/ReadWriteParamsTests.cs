

using Dacs7;
using Dacs7Tests.ServerHelper;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7Tests
{
    [Collection("PlcServer collection")]
    public class ReadWriteParamsTests
    {
        [Fact]
        public async Task ReadWriteSingleBits()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var baseOffset = 10000 * 8;
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, false),
                       WriteItem.Create(datablock, baseOffset + 5, false))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<bool>(datablock, baseOffset),
                                                       ReadItem.Create<bool>(datablock, baseOffset + 5))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.False((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.False((bool)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, true),
                                       WriteItem.Create(datablock, baseOffset + 5, true))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<bool>(datablock, baseOffset),
                                                       ReadItem.Create<bool>(datablock, baseOffset + 5))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.True((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.True((bool)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, baseOffset, false),
                                       WriteItem.Create(datablock, baseOffset + 5, false))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10002, (ushort)0),
                                                            WriteItem.Create(datablock, 10004, (short)0))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<ushort>(datablock, 10002),
                                                      ReadItem.Create<short>(datablock, 10004))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(ushort), results[0].Type);
                Assert.Equal((ushort)0, (ushort)results[0].Value);
                Assert.Equal(typeof(short), results[1].Type);
                Assert.Equal((short)0, (short)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10002, (ushort)15),
                                                        WriteItem.Create(datablock, 10004, (short)25))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<ushort>(datablock, 10002),
                                                  ReadItem.Create<short>(datablock, 10004))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(ushort), results[0].Type);
                Assert.Equal((ushort)15, (ushort)results[0].Value);
                Assert.Equal(typeof(short), results[1].Type);
                Assert.Equal((short)25, (short)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10002, (ushort)0),
                                                            WriteItem.Create(datablock, 10004, (short)0))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleDWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10006, (uint)0),
                                                            WriteItem.Create(datablock, 10010, (int)0))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<uint>(datablock, 10006),
                                                      ReadItem.Create<int>(datablock, 10010))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((uint)0, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal((int)0, (int)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10006, (uint)15),
                                                        WriteItem.Create(datablock, 10010, (int)25))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<uint>(datablock, 10006),
                                                  ReadItem.Create<int>(datablock, 10010))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((ushort)15, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal((int)25, (int)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10006, (uint)0),
                                                            WriteItem.Create(datablock, 10010, (int)0))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingles()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10014, (Single)0.0))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<Single>(datablock, 10014))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(Single), results[0].Type);
                Assert.Equal((Single)0.0, (Single)results[0].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10014, (Single)0.5))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<Single>(datablock, 10014))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(Single), results[0].Type);
                Assert.Equal((Single)0.5, (Single)results[0].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10014, (Single)0.0))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleStrings()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10046, ""),
                                                            WriteItem.Create(datablock, 10068, "                    "))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<string>(datablock, 10046, 20),
                                                      ReadItem.Create<string>(datablock, 10068, 20))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("                    ", (string)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10046, "Test1"),
                                                        WriteItem.Create(datablock, 10068, "Test2               "))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<string>(datablock, 10046, 20),
                                                  ReadItem.Create<string>(datablock, 10068, 20))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("Test1", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("Test2               ", (string)results[1].Value);

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, 10046, ""),
                                                            WriteItem.Create(datablock, 10068, "                    "))).ToArray();

            });
        }



        [Fact]
        public async Task ReadMultibleByteArrayData()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var results = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, 0, 1000),
                                                       ReadItem.Create<byte[]>(datablock, 2200, 100),
                                                       ReadItem.Create<byte[]>(datablock, 1000, 1000),
                                                       ReadItem.Create<byte[]>(datablock, 200, 100))).ToArray();


                Assert.Equal(4, results.Count());
                Assert.Equal(1000, results[0].Data.Length);
                Assert.Equal(100, results[1].Data.Length);
                Assert.Equal(1000, results[2].Data.Length);
                Assert.Equal(100, results[3].Data.Length);
            });
        }

        [Fact]
        public async Task ReadWriteBigDBData()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                const ushort offset = 2500;
                var resultsDefault0 = new Memory<byte>(Enumerable.Repeat((byte)0x00, 1000).ToArray());
                var resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                var resultsDefault2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));

                var results0 = new Memory<byte>(Enumerable.Repeat((byte)0x25, 1000).ToArray());
                var results1 = await client.WriteAsync(WriteItem.Create(datablock, offset, results0));
                var results2 = (await client.ReadAsync(ReadItem.Create<byte[]>(datablock, offset, 1000)));


                resultsDefault1 = await client.WriteAsync(WriteItem.Create(datablock, offset, resultsDefault0));
                Assert.True(results0.Span.SequenceEqual(results2.FirstOrDefault().Data.Span));
            });
        }

        [Fact]
        public async Task ReadWriteMultibleWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                const int startAddress = 10022;
                var writeDataDefault = new ushort[] { 0, 0 };
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<ushort>(datablock, startAddress, 2))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValueDefault = results[0].Value as ushort[];

                var writeData = new ushort[] { 22, 21 };
                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeData))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<ushort>(datablock, startAddress, 2))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValue = results[0].Value as ushort[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleInts()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeDataDefault = new short[] { 0, 0 };
                const int startAddress = 10026;
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<short>(datablock, startAddress, 2))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValueDefault = results[0].Value as short[];

                var writeData = new short[] { 22, 21 };
                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeData))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<short>(datablock, startAddress, 2))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValue = results[0].Value as short[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeDataDefault = new uint[] { 0, 0 };
                const int startAddress = 10034;
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<uint>(datablock, startAddress, 2))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValueDefault = results[0].Value as uint[];

                var writeData = new uint[] { 22, 21 };
                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeData))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<uint>(datablock, startAddress, 2))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValue = results[0].Value as uint[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDInts()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                const string datablock = "DB1";
                var writeDataDefault = new int[] { 0, 0 };
                const int startAddress = 10038;
                var writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();


                var results = (await client.ReadAsync(ReadItem.Create<int>(datablock, startAddress, 2))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValueDefault = results[0].GetValue<int[]>();

                Assert.True(resultValueDefault.SequenceEqual(writeDataDefault));

                var writeData = new int[] { 22, 21 };
                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeData))).ToArray();

                results = (await client.ReadAsync(ReadItem.Create<int>(datablock, startAddress, 2))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValue = results[0].GetValue<int[]>();

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(WriteItem.Create(datablock, startAddress, writeDataDefault))).ToArray();

            });
        }


        [Fact]
        public async Task ReadMixedData()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var results = (await client.ReadAsync(ReadItem.CreateFromTag("DB1.4,B,8"),
                                                      ReadItem.CreateFromTag("DB1.38,B,8"),
                                                      ReadItem.CreateFromTag("DB1.94,B,10"))).ToArray();



                Assert.Equal(3, results.Count());
                Assert.True(results[0].IsSuccessReturnCode);
                Assert.True(results[1].IsSuccessReturnCode);
                Assert.True(results[2].IsSuccessReturnCode);

            });
        }

    }
}