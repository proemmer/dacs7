using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7
{
    public class AsyncAutoResetEvent<T>
    {
        private readonly static Task<T> _completed = Task.FromResult<T>(default);
        private readonly Queue<TaskCompletionSource<T>> _waits = new Queue<TaskCompletionSource<T>>();
        private bool _signaled;
        private T _lastValue = default;

        public AsyncAutoResetEvent()
        {
        }

        public Task<T> WaitAsync(int timeout = -1)
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    var result = Task.FromResult(_lastValue);
                    _lastValue = default;
                    _signaled = false;
                    return result;
                }
                else if (timeout == 0)
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
        public bool Set(T value)
        {
            TaskCompletionSource<T> toRelease = null;
            lock (_waits)
            {
                if (_waits.Count > 0)
                {
                    toRelease = _waits.Dequeue();
                }
                else if (!_signaled)
                {
                    _signaled = true;
                    _lastValue = value;
                }
                else
                {
                    // Could not set because it is already set.
                    return false;
                }
            }
            toRelease?.SetResult(value);
            return true;
        }
    }
}
