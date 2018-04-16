using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Helper
{
    internal class SemaphoreGuard : IDisposable
    {
        private SemaphoreSlim _semaphore;
        private bool IsDisposed { get { return _semaphore == null; } }
        public SemaphoreGuard(SemaphoreSlim semaphore, bool wait = true)
        {
            _semaphore = semaphore;
            if (wait)
            {
                _semaphore.Wait();
            }
        }

        public static async Task<SemaphoreGuard> Async(SemaphoreSlim semaphore)
        {
            var guard = new SemaphoreGuard(semaphore, false);
            await semaphore.WaitAsync();
            return guard;
        }

        public void Dispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString());
            _semaphore.Release();
            _semaphore = null;
        }
    }


}
