using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    internal class AsyncAutoResetEvent<T>
    {
        private readonly static Task<T> _completed = Task.FromResult<T>(default);
        private readonly Queue<TaskCompletionSource<T>> _waits = new Queue<TaskCompletionSource<T>>();
        private bool _signaled;
        private T _lastValue;

        public AsyncAutoResetEvent()
        {
        }

        public Task<T> WaitAsync(int timeout = -1)
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    var result = _lastValue;
                    _lastValue = default;
                    return Task.FromResult(_lastValue);
                }
                else if(timeout == 0)
                {
                    return _completed;
                }
                else
                {
                    
                    var tcs = new TaskCompletionSource<T>();
                    CancellationTokenRegistration registration = default;
                    CancellationTokenSource cts = null;
                    if (timeout > -1)
                    {
                        cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));
                        registration = cts.Token.Register(() =>
                        {
                            tcs.TrySetResult(default);
                        }, false);
                    }

                    _waits.Enqueue(tcs);
                    return tcs.Task.ContinueWith<T>(t => 
                    {
                        if (cts != null)
                        {
                            registration.Dispose();
                            cts.Dispose();
                        }
                        return t.Result;
                    });


                }
            }
        }
        public void Set(T value)
        {
            TaskCompletionSource<T> toRelease = null;
            lock (_waits)
            {
                if (_waits.Count > 0)
                    toRelease = _waits.Dequeue();
                else if (!_signaled)
                {
                    _signaled = true;
                    _lastValue = value;
                }
            }
            toRelease?.SetResult(value);
        }
    }
}
