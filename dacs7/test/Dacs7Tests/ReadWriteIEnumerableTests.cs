﻿

using Dacs7;
using Dacs7Tests.ServerHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dacs7Tests
{
    [Collection("PlcServer collection")]
    public class ReadWriteIEnumerableTests
    {
        

        [Fact]
        public async Task ReadWriteSingleBits()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10000,x0" , false },
                    { $"DB2.10000,x5" , false }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();


                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


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

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(bool), results[0].Type);
                Assert.True((bool)results[0].Value);
                Assert.Equal(typeof(bool), results[1].Type);
                Assert.True((bool)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleWords()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10002,w" , (ushort)0 },
                    { $"DB2.10004,i" , (short)0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


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

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(ushort), results[0].Type);
                Assert.Equal((ushort)15, (ushort)results[0].Value);
                Assert.Equal(typeof(short), results[1].Type);
                Assert.Equal((short)25, (short)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleDWords()
        {
            await ExecuteAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10006,dw" , (uint)0 },
                    { $"DB2.10010,di" , (int)0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();



                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((uint)0, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal((int)0, (int)results[1].Value);


                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10006,dw" , (uint)15 },
                    { $"DB2.10010,di" , (int)25 }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(uint), results[0].Type);
                Assert.Equal((ushort)15, (uint)results[0].Value);
                Assert.Equal(typeof(int), results[1].Type);
                Assert.Equal((int)25, (int)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingles()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10014,r" , (Single)0.0 }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();



                Assert.Single(results);
                Assert.Equal(typeof(Single), results[0].Type);
                Assert.Equal((Single)0.0, (Single)results[0].Value);

                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10014,r" , (Single)0.5 }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(Single), results[0].Type);
                Assert.Equal((Single)0.5, (Single)results[0].Value);

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteSingleStrings()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10046,s,20" , "" },
                    { $"DB2.10068,s,20" , "                    " }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


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

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Equal(2, results.Count());
                Assert.Equal(typeof(string), results[0].Type);
                Assert.Equal("Test1", (string)results[0].Value);
                Assert.Equal(typeof(string), results[1].Type);
                Assert.Equal("Test2               ", (string)results[1].Value);

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }



        [Fact]
        public async Task ReadMultibleByteArrayData()
        {
            await ExecuteAsync(async (client) =>
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
            await ExecuteAsync(async (client) =>
            {
                var data = Enumerable.Repeat((byte)0x00, 1000).ToArray();
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.2500,b,1000" , data }
                };

                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.True(results.FirstOrDefault().Data.ToArray().SequenceEqual(data));

                var data2 = Enumerable.Repeat((byte)0x25, 1000).ToArray();
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.2500,b,1000" , data2 }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.True(results.FirstOrDefault().Data.ToArray().SequenceEqual(data2));

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleWords()
        {
            await ExecuteAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10022,w,2" , new ushort[] { 0, 0 } }
                };

                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValueDefault = results[0].Value as ushort[];
                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as ushort[]));

                var writeData = new ushort[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10022,w,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(ushort[]), results[0].Type);

                var resultValue = results[0].Value as ushort[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleInts()
        {
            await ExecuteAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10026,i,2" , new short[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValueDefault = results[0].Value as short[];
                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as short[]));

                var writeData = new short[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10026,i,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(short[]), results[0].Type);

                var resultValue = results[0].Value as short[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDWords()
        {
            await ExecuteAsync(async (client) =>
            {

                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10034,dw,2" , new uint[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValueDefault = results[0].Value as uint[];

                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as uint[]));

                var writeData = new uint[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10034,dw,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(uint[]), results[0].Type);

                var resultValue = results[0].Value as uint[];

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }

        [Fact]
        public async Task ReadWriteMultibleDInts()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.10038,di,2" , new int[] { 0, 0 } }
                };
                var writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();
                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();


                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValueDefault = results[0].GetValue<int[]>();

                Assert.True(resultValueDefault.SequenceEqual(originWriteTags.FirstOrDefault().Value as int[]));

                var writeData = new int[] { 22, 21 };
                var writeTags = new Dictionary<string, object>
                {
                    { $"DB2.10038,di,2" , writeData }
                };

                writeResults = (await client.WriteAsync(writeTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

                results = (await client.ReadAsync(writeTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Single(results);
                Assert.Equal(typeof(int[]), results[0].Type);

                var resultValue = results[0].GetValue<int[]>();

                Assert.True(resultValue.SequenceEqual(writeData));

                writeResults = (await client.WriteAsync(originWriteTags.Select(w => WriteItem.CreateFromTag(w.Key, w.Value)))).ToArray();

            });
        }


        [Fact]
        public async Task ReadMixedData()
        {
            await ExecuteAsync(async (client) =>
            {
                var originWriteTags = new Dictionary<string, object>
                {
                    { $"DB2.4,B,8" , null },
                    { $"DB2.38,B,8" , null },
                    { $"DB2.94,B,10" , null },
                };


                var results = (await client.ReadAsync(originWriteTags.Select(w => ReadItem.CreateFromTag(w.Key)))).ToArray();

                Assert.Equal(3, results.Count());
                Assert.True(results[0].IsSuccessReturnCode);
                Assert.True(results[1].IsSuccessReturnCode);
                Assert.True(results[2].IsSuccessReturnCode);

            });
        }








        private static async Task ExecuteAsync(Func<Dacs7Client, Task> execution)
        {
            var client = new Dacs7Client(PlcTestServer.Address, PlcTestServer.ConnectionType, PlcTestServer.Timeout);
            var retries = 3;

            do
            {
                try
                {
                    await client.ConnectAsync();
                    await execution(client);
                    break;
                }
                catch (Dacs7NotConnectedException)
                {
                    await Task.Delay(1000);
                    retries--;
                    if (retries <= 0)
                        throw;
                }
                finally
                {
                    await client.DisconnectAsync();
                }
            }
            while (retries > 0);
        }
    }
}
