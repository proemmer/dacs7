// #define REALPLC
using Dacs7;
using Dacs7.DataProvider;
using Dacs7.Helper;
using System;
using System.Collections.Generic;
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
            PlcTestServer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            PlcTestServer.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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

        private static readonly SemaphoreSlim _sema = new(1);
        private static readonly Dacs7Server _server = new(5021, SimulationPlcDataProvider.Instance);
        private static readonly Dictionary<ushort, ushort> _dbAreas = new()
        {
            { 1, 20000 },
            { 2, 20000 },
            { 3, 10 },
            { 141, 20000 },
            { 962, 1000 },
            { 4, 10000 },
            { 1993, 10000 },
            { 2241 , 15450 }
        };


#if REALPLC
        public static readonly string Address = "192.168.0.148"; // SoftPLC
        //public static readonly string Address = "192.168.0.220";   // HardPLC
        //public static readonly string Address = "192.168.1.60:102,0,1";   // TIA PLC
        public static readonly PlcConnectionType ConnectionType = PlcConnectionType.Pg;
        public static readonly int Timeout = 5000;
        private static readonly SemaphoreSlim _semaphore = null;

#else
        public static readonly string Address = "127.0.0.1:5021,0,1";
        public static readonly PlcConnectionType ConnectionType = PlcConnectionType.Pg;
        public static readonly int Timeout = 15000;
        private static readonly SemaphoreSlim _semaphore = new(1);
#endif



        public static async Task StartAsync()
        {
            if (_server.IsConnected)
            {
                return;
            }

            using (await SemaphoreGuard.Async(_sema))
            {
                if (_server.IsConnected)
                {
                    return;
                }

                foreach (KeyValuePair<ushort, ushort> item in _dbAreas)
                {
                    SimulationPlcDataProvider.Instance.Register(PlcArea.DB, item.Value, item.Key);
                }

                await _server.ConnectAsync();
            }
        }

        public static async Task StopAsync()
        {
            if (!_server.IsConnected)
            {
                return;
            }
            using (await SemaphoreGuard.Async(_sema))
            {
                if (!_server.IsConnected)
                {
                    return;
                }
                await _server.DisconnectAsync();
            }
        }



        public static async Task ExecuteClientAsync(Func<Dacs7Client, Task> execution, ushort pduSize = 960)
        {
            Dacs7Client client = new(Address, ConnectionType, Timeout)
            {
                PduSize = pduSize
            };
            int retries = 3;

            do
            {
                if (_semaphore != null && !_semaphore.Wait(0))
                {
                    await _semaphore.WaitAsync();
                }

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
                    {
                        throw;
                    }
                }
                finally
                {
                    await client.DisconnectAsync();
                    await Task.Delay(10);
                    if (_semaphore != null)
                    {
                        _semaphore.Release();
                    }
                }
            }
            while (retries > 0);
        }


    }
}
