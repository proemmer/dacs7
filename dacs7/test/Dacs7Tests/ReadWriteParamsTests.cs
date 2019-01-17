

using Dacs7;
using Dacs7.ReadWrite;
using Dacs7Tests.ServerHelper;
using System;
using System.Collections.Generic;
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
        public async Task ReadWriteMultibleCharsLongerThanPDUSize()
        {
            const string datablock = "DB141";
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                
                var data = new List<ReadItem>{ ReadItem.Create<char[]>(datablock, 28, 14),
                                               ReadItem.Create<char[]>(datablock, 46, 10),
                                               ReadItem.Create<char[]>(datablock, 106, 10),
                                               ReadItem.Create<char[]>(datablock, 124, 10),
                                               ReadItem.Create<char[]>(datablock, 134, 10),
                                               ReadItem.Create<char[]>(datablock, 60, 8),
                                               ReadItem.Create<char[]>(datablock, 86, 8),
                                               ReadItem.Create<char[]>(datablock, 94, 8),
                                               ReadItem.Create<char[]>(datablock, 116, 8),
                                               ReadItem.Create<char[]>(datablock, 0, 8),
                                               ReadItem.Create<char[]>(datablock, 8, 8) ,
                                               ReadItem.Create<char[]>(datablock, 16, 8) ,
                                               ReadItem.Create<char[]>(datablock, 76, 6) ,
                                               ReadItem.Create<char[]>(datablock, 24, 4) ,
                                               ReadItem.Create<char[]>(datablock, 42, 4) ,
                                               ReadItem.Create<char[]>(datablock, 56, 4)   };

                var results = (await client.ReadAsync(data)).ToArray();

                Assert.Equal(data.Count, results.Length);
                Assert.True(results.All(x => x.IsSuccessReturnCode));

                var writeResults = await client.WriteAsync(data.Select((r, i) => r.From(results[i].Data)).ToArray());

                Assert.Equal(data.Count, writeResults.Count());
                Assert.True(writeResults.All(x => x == ItemResponseRetValue.Success));
            }, 240);
        }


        [Fact]
        public async Task ReadWriteMultibleCharsStringLongerThanPDUSize()
        {
            const string datablock = "DB141";
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var data = new List<ReadItem>{ ReadItem.Create<char[]>(datablock, 28, 14),
                                               ReadItem.Create<char[]>(datablock, 46, 10),
                                               ReadItem.Create<string>(datablock, 106, 10),
                                               ReadItem.Create<char[]>(datablock, 124, 10),
                                               ReadItem.Create<char[]>(datablock, 134, 10),
                                               ReadItem.Create<char[]>(datablock, 60, 8),
                                               ReadItem.Create<char[]>(datablock, 86, 8),
                                               ReadItem.Create<char[]>(datablock, 94, 8),
                                               ReadItem.Create<char[]>(datablock, 116, 8),
                                               ReadItem.Create<char[]>(datablock, 0, 8),
                                               ReadItem.Create<char[]>(datablock, 8, 8) ,
                                               ReadItem.Create<char[]>(datablock, 16, 8) ,
                                               ReadItem.Create<char[]>(datablock, 76, 6) ,
                                               ReadItem.Create<char[]>(datablock, 24, 4) ,
                                               ReadItem.Create<char[]>(datablock, 42, 4) ,
                                               ReadItem.Create<char[]>(datablock, 56, 4)   };

                var results = (await client.ReadAsync(data)).ToArray();

                Assert.Equal(data.Count, results.Length);
                Assert.True(results.All(x => x.IsSuccessReturnCode));

                var writeResults = await client.WriteAsync(data.Select((r, i) => r.From(results[i].Data)).ToArray());

                Assert.Equal(data.Count, writeResults.Count());
                Assert.True(writeResults.All(x => x == ItemResponseRetValue.Success));
            }, 240);
        }


        [Fact]
        public async Task WriteMultiCharsAndString()
        {
            const string datablock = "DB962";
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var data = new List<WriteItem>{ WriteItem.Create<char[]>(datablock, 60, 14, "|010101010101|".ToCharArray()),
                                                WriteItem.Create<char[]>(datablock, 2, 10, "|01010101|".ToCharArray()),
                                                WriteItem.Create<string>(datablock, 12, 8, "|123456|"),
                                                WriteItem.Create<char[]>(datablock, 36, 6, "|1234|".ToCharArray()),
                                                WriteItem.Create<char[]>(datablock, 24, 4, "|12|".ToCharArray()),
                                                WriteItem.Create<char[]>(datablock, 30, 4, "|12|".ToCharArray()),
                                                WriteItem.Create<char[]>(datablock, 44, 4, "|12|".ToCharArray())};

                var writeResults = await client.WriteAsync(data);
                Assert.Equal(data.Count, writeResults.Count());
                Assert.True(writeResults.All(x => x == ItemResponseRetValue.Success));
            }, 240);
        }



        [Fact]
        public async Task WriteString()
        {
            const string datablock = "DB962";
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var data = new List<WriteItem>{ WriteItem.Create<string>(datablock, 12, 8, "12345678")};

                var writeResults = await client.WriteAsync(data);
                Assert.Equal(data.Count, writeResults.Count());
                Assert.True(writeResults.All(x => x == ItemResponseRetValue.Success));
            }, 240);
        }



        [Fact]
        public async Task ReadWriteString()
        {
            const string datablock = "DB962";
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var data0 = new List<WriteItem> { WriteItem.Create<string>(datablock, 58, 14, "XXXXXXXXXXXXXX") };

                var writeResults1 = await client.WriteAsync(data0);
                Assert.Equal(data0.Count, writeResults1.Count());
                Assert.True(writeResults1.All(x => x == ItemResponseRetValue.Success));


                var data1 = new List<ReadItem> { ReadItem.Create<string>(datablock, 58, 14) };

                var readResults1 = await client.ReadAsync(data1);

                var data = new List<WriteItem> { WriteItem.Create<string>(datablock, 58, 14, "1234567890ABCD") };

                var writeResults2 = await client.WriteAsync(data);
                Assert.Equal(data.Count, writeResults2.Count());
                Assert.True(writeResults2.All(x => x == ItemResponseRetValue.Success));


                var readResults2 = await client.ReadAsync(data1);


                Assert.False( readResults1.FirstOrDefault().Data.Span.SequenceEqual(readResults2.FirstOrDefault().Data.Span));


            }, 240);
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