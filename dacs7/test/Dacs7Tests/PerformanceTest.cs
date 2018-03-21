using Dacs7;
using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Dacs7Tests
{

#if TEST_PLC
    public class PerformanceTest
    {

        private const string Ip = "127.0.0.1";//"127.0.0.1";
        //private const string Ip = "192.168.1.10";//"127.0.0.1";
        private const string ConnectionString = "Data Source=" + Ip + ":102,0,2"; //"Data Source=192.168.1.10:102,0,2";


        public PerformanceTest()
        {
            //Manually instantiate all Ack types, because we have a different executing assembly in the test framework and so this will not be done automatically
            new S7AckDataProtocolPolicy();
            new S7ReadJobAckDataProtocolPolicy();
            new S7WriteJobAckDataProtocolPolicy();
        }

        [Fact]
        public void PerfTest()
        {
            var client = new Dacs7Client(_loggerFactory);
            client.Connect(ConnectionString);
            var offset = 0;


            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100; i++)
            {
                var reads = new List<ReadOperationParameter> {
                    ReadOperationParameter.CreateForBit(250, offset, 0),
                    ReadOperationParameter.CreateForBit(250, offset, 1),
                    ReadOperationParameter.CreateForBit(250, offset, 2),
                    ReadOperationParameter.CreateForBit(250, offset, 3),
                    ReadOperationParameter.CreateForBit(250, offset, 4)
                 };
                var result = client.ReadAny(reads);

                if(!(bool)result.FirstOrDefault())
                {
                    Console.WriteLine($"Bit 0 is false!");
                }
                Console.WriteLine($"{i}");
            }
            sw.Stop();
            client.Disconnect();
        }
    }

#endif
}
