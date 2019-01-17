#define REALPLC
using Dacs7;
using Snap7;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;



namespace Dacs7Tests.ServerHelper
{

    // Windows - most likely you ar running the server in a pc on wich is
    //           installed step 7 : open a command prompt and type
    //             "net stop s7oiehsx"    (Win32) or
    //             "net stop s7oiehsx64"  (Win64)
    //           And after this test :
    //             "net start s7oiehsx"   (Win32) or
    //             "net start s7oiehsx64" (Win64)

    public class PlcServerFixture : IDisposable
    {
        public PlcServerFixture()
        {
            PlcTestServer.Start();
        }

        public void Dispose()
        {
            PlcTestServer.Stop();
        }

    }

    [CollectionDefinition("PlcServer collection")]
    public class PlcServerCollection : ICollectionFixture<PlcServerFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }


    internal static class PlcTestServer
    {

        
        private static readonly object _lock = new object();
        private static readonly S7Server _server = new S7Server();
        private static readonly Dictionary<int, byte[]> _dbAreas = new Dictionary<int, byte[]>
        {
            {1, new byte[20000] },
            {2, new byte[20000] },
            {3, new byte[10] },
            {141, new byte[20000] },
        };


#if REALPLC
        public static readonly string Address = "192.168.0.148"; // SoftPLC
        //public static readonly string Address = "192.168.0.220";   // HardPLC
        //public static readonly string Address = "192.168.1.60:102,0,1";   // TIA PLC
        public static readonly PlcConnectionType ConnectionType = PlcConnectionType.Pg;
        public static readonly int Timeout = 5000;
        private static readonly SemaphoreSlim _semaphore = null;

#else
        public static readonly string Address = "127.0.0.1";
        public static readonly PlcConnectionType ConnectionType = PlcConnectionType.Pg;
        public static readonly int Timeout = 15000;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
#endif



        public static int Start()
        {
            if (_server.ServerStatus == 1)
            {
                return 0;
            }

            lock (_lock)
            {
                if (_server.ServerStatus == 1)
                {
                    return 0;
                }

                foreach (var item in _dbAreas)
                {
                    var data = item.Value;
                    _server.RegisterArea(S7Server.srvAreaDB, item.Key, ref data, data.Length);
                }

                return _server.Start();
            }
        }

        public static int Stop()
        {
            return _server.Stop();
        }



        public static async Task ExecuteClientAsync(Func<Dacs7Client, Task> execution, ushort pduSize = 960)
        {
            var client = new Dacs7Client(Address, ConnectionType, Timeout)
            {
                PduSize = pduSize
            };
            var retries = 3;

            do
            {
                if(_semaphore != null) await _semaphore.WaitAsync();
                try
                {
                    await client.ConnectAsync();
                    await execution(client);
                    break;
                }
                catch (Exception ex) when (ex is Dacs7NotConnectedException || ex is Dacs7ReadTimeoutException) // because of snap7
                {
                    await Task.Delay(1000);
                    retries--;
                    if (retries <= 0)
                        throw;
                }
                finally
                {
                    await client.DisconnectAsync();
                    await Task.Delay(10);
                    if (_semaphore != null) _semaphore.Release();
                }
            }
            while (retries > 0);
        }


    }
}
