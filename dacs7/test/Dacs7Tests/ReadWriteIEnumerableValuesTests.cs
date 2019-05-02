using Dacs7.ReadWrite;
using Dacs7Tests.ServerHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7.Tests
{
    [Collection("PlcServer collection")]
    public class ReadWriteIEnumerableValuesTests
    {


        [Fact]
        public async Task ReadWriteSingleBits()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10000,x0" , false },
                    { $"DB2.10000,x5" , false }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();


                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.False((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.False((bool)results[1].Value);


                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10000,x0" , true },
                    { $"DB2.10000,x5" , true }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.True((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.True((bool)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10002,w" , (ushort)0 },
                    { $"DB2.10004,i" , (short)0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(ushort), results[0].Type);
                Assert.Equal((ushort)0, (ushort)results[0].Value);
                Assert.Equal(typeof(short), results[1].Type);
                Assert.Equal((short)0, (short)results[1].Value);

                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10002,w" , (ushort)15 },
                    { $"DB2.10004,i" , (short)25 }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(ushort), results[0].Type);
                Assert.Equal((ushort)15, (ushort)results[0].Value);
                Assert.Equal(typeof(short), results[1].Type);
                Assert.Equal((short)25, (short)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleDWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10006,dw" , (uint)0 },
                    { $"DB2.10010,di" , 0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();



                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((uint)0, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal(0, (int)results[1].Value);


                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10006,dw" , (uint)15 },
                    { $"DB2.10010,di" , 25 }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((ushort)15, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal(25, (int)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingles()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10014,r" , (float)0.0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();



                Assert.Single(results);
                Assert.Equal(typeof(float), results[0].Type);
                Assert.Equal((float)0.0, (float)results[0].Value);

                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10014,r" , (float)0.5 }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(float), results[0].Type);
                Assert.Equal((float)0.5, (float)results[0].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleStrings()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10046,s,20" , "" },
                    { $"DB2.10068,s,20" , "                    " }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("                    ", (string)results[1].Value);


                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10046,s,20" , "Test1" },
                    { $"DB2.10068,s,20" , "Test2               " }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("Test1", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("Test2               ", (string)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }


        [Fact]
        public async Task ReadWriteSingleStrings2()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10090,s,20" , "" },
                    { $"DB2.10112,s,20" , "                    " }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("                    ", (string)results[1].Value);


                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10046,s,20" , "A PARTIAL STRING".PadLeft(20).Substring(0, 18) },
                    { $"DB2.10068,s,20" , "A FULLY STRING IT IS".PadLeft(20) }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("A PARTIAL STRING".PadLeft(20).Substring(0, 18), (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("A FULLY STRING IT IS".PadLeft(20), (string)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }


        [Fact]
        public async Task ReadMultibleByteArrayData()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var readTags = new Dictionary<string, object>
                {
                    { $"DB2.0,b,1000" , 1000 },
                    { $"DB2.2200,b,100" , 100 },
                    { $"DB2.1000,b,1000" , 1000 },
                    { $"DB2.200,b,100" , 100 },
                };

                var results = (await client.ReadAsync(readTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

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
                var data = Enumerable.Repeat((byte)0x00, 1000).ToArray();
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.2500,b,1000" , data }
                };

                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();

                Assert.True(results.FirstOrDefault().Data.ToArray().SequenceEqual(data));

                var data2 = Enumerable.Repeat((byte)0x25, 1000).ToArray();
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.2500,b,1000" , data2 }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.True(results.FirstOrDefault().Data.ToArray().SequenceEqual(data2));

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10022,w,2" , new ushort[] { 0, 0 } }
                };

                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValueDefault = results[0].Value as ushort[];
                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as ushort[]));

                var writeData = new ushort[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10022,w,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValue = results[0].Value as ushort[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleInts()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10026,i,2" , new short[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValueDefault = results[0].Value as short[];
                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as short[]));

                var writeData = new short[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10026,i,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValue = results[0].Value as short[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDWords()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10034,dw,2" , new uint[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValueDefault = results[0].Value as uint[];

                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as uint[]));

                var writeData = new uint[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10034,dw,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValue = results[0].Value as uint[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDInts()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10038,di,2" , new int[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags)).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValueDefault = results[0].GetValue<int[]>();

                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as int[]));

                var writeData = new int[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10038,di,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags)).ToArray();

                results = (await client.ReadAsync(writeTags.Keys)).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValue = results[0].GetValue<int[]>();

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags)).ToArray();

            });
        }


        [Fact]
        public async Task ReadMixedData()
        {
            await PlcTestServer.ExecuteClientAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.4,B,8" , null },
                    { $"DB2.38,B,8" , null },
                    { $"DB2.94,B,10" , null },
                };


                var results = (await client.ReadAsync(originWriteTags.Keys)).ToArray();

                Assert.Equal(3, results.Count());
                Assert.True(results[0].IsSuccessReturnCode);
                Assert.True(results[1].IsSuccessReturnCode);
                Assert.True(results[2].IsSuccessReturnCode);

            });
        }








    }
}
