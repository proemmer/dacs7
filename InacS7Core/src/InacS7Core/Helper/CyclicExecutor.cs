using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InacS7Core.Heper
{

    public class CyclicExecutor : AbstractGenericSingleton<CyclicExecutor>
    {
        private class Execution
        {
            #region Fields
            private readonly string _name = string.Empty;
            private string _label = string.Empty;
            private int _milliseconds;
            private DateTime _eventNow = DateTime.MinValue;
            private readonly Action _action;
            private readonly bool _asThread;
            private readonly bool _singleRun;
            #endregion

            public Execution(string aName, string aLabel, int aMilliseconds, Action aMethod, bool aAsThread = false, bool aSingleRun = false)
            {
                _name = aName;
                Label = aLabel;
                _milliseconds = aMilliseconds;
                _action = aMethod;
                _asThread = aAsThread;
                _singleRun = aSingleRun;
                Reset();
            }
            public void Set()
            {
                _eventNow = DateTime.Now;
            }
            public void Reset()
            {
                _eventNow = DateTime.Now.AddMilliseconds(_milliseconds);
            }
            public bool Enabled { get; set; }
            public string Name { get { return _name; } }
            public DateTime EventNow { get { return _eventNow; } }
            public Action Action { get { return _action; } }
            public bool AsThread { get { return _asThread; } }
            public bool SingleRun { get { return _singleRun; } }
            public bool ThreadIsRunning { get; set; }
            public int Milliseconds
            {
                get { return _milliseconds; }
                set { _milliseconds = value; }
            }
            private string Label
            {
                get { return _label; }
                set { _label = value; }
            }
        }

        #region Fields
        private readonly AutoResetEvent _are = new AutoResetEvent(false);
        private bool _threadRunning = true;
        private readonly IDictionary<string, Execution> _timers = new Dictionary<string,Execution>();
        private readonly ReaderWriterLockSlim _timersRWLock = new ReaderWriterLockSlim();
        #endregion

        #region Singleton
        public static CyclicExecutor Instance
        {
            get
            {
                if (!Initialised)
                {
                    Init(new CyclicExecutor());
                }
                return UniqueInstance;
            }
        }

        private CyclicExecutor()
        {
            Task.Factory.StartNew(ThreadCyclicExecutor, TaskCreationOptions.LongRunning);
        }
        #endregion

        /// <summary>
        /// Checks if an existing execution is enabled or not.
        /// </summary>
        /// <param name="name">Name of the execution</param>
        /// <returns>returns the state of the execution</returns>
        public bool Enabled(string name)
        {
            _timersRWLock.EnterReadLock();
            try
            {
                if (!_threadRunning)
                    return false;
                Execution exec = null;
                if(_timers.TryGetValue(name, out exec) && exec != null)
                    return exec.Enabled;
                return false;
            }
            finally
            {
                _timersRWLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Enable or disable an existing execution, or start it immediately.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="state"></param>
        /// <param name="startImmediately"></param>
        public void Enabled(string name, bool state, bool startImmediately = false)
        {

            _timersRWLock.EnterReadLock();
            try
            {
                if (!_threadRunning)
                    return;
                Execution exec = null;
                if (_timers.TryGetValue(name, out exec) && exec != null)
                {
                    exec.Enabled = state;
                    if (state && startImmediately)
                        exec.Set();
                }
            }
            finally
            {
                _timersRWLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Update the execution time of an existing execution
        /// </summary>
        /// <param name="name">Name of the execution which should be updated</param>
        /// <param name="milliseconds"></param>
        public void Update(string name, int milliseconds)
        {
            _timersRWLock.EnterReadLock();
            try
            {
                if (!_threadRunning)
                    return;
                Execution exec = null;
                if (_timers.TryGetValue(name, out exec) && exec != null)
                {
                    exec.Milliseconds = milliseconds;
                }
            }
            finally
            {
                _timersRWLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Add a new execution to the executor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="label"></param>
        /// <param name="milliseconds"></param>
        /// <param name="action"></param>
        /// <param name="enabled"></param>
        /// <param name="ownThread"></param>
        /// <param name="aSingleRun"></param>
        public void Add(string name, string label, int milliseconds, Action action, bool enabled = false, bool ownThread = false, bool executeOnlyOnce = false)
        {
            _timersRWLock.EnterWriteLock();
            try
            {
                if (!_threadRunning)
                    return;
                var exec = new Execution(name, label, milliseconds, action, ownThread, executeOnlyOnce)
                {
                    Enabled = enabled
                };

                if (!_timers.ContainsKey(name))
                    _timers.Add(name, exec);
                else
                    _timers[name] = exec;
            }
            finally
            {
                _timersRWLock.ExitWriteLock();
            }
        }

        public bool Contains(string aName)
        {
            if (string.IsNullOrEmpty(aName))
                return false;

            _timersRWLock.EnterReadLock();
            try
            {
                return _threadRunning && _timers.ContainsKey(aName);
            }
            finally
            {
                _timersRWLock.ExitReadLock();
            }
        }

        public void Shutdown()
        {
            if (!_threadRunning)
                return;
            _threadRunning = false;
            _are.WaitOne();
        }


        private IList<Execution> GetSnapshotOfExecutions(Func<Execution,bool> where = null)
        {
            _timersRWLock.EnterReadLock();
            try
            {
                return where == null ? _timers.Values.ToList() : _timers.Values.Where(where).ToList();
            }
            finally
            {
                _timersRWLock.ExitReadLock();
            }
            
        }

        private void ThreadCyclicExecutor()
        {
            while (_threadRunning)
            {
                Thread.Sleep(10);

                var now = DateTime.Now;
                foreach (var exec in GetSnapshotOfExecutions(st => st.Enabled && now >= st.EventNow && !st.ThreadIsRunning))
                {
                    if (exec.AsThread)
                    {
                        var closure = exec;
                        exec.ThreadIsRunning = true;
                        var dummy = Task.Factory.StartNew(() => DelegateSingletonTimer(closure), TaskCreationOptions.LongRunning);
                    }
                    else
                    {
                        try
                        {
                            exec.Action();
                        }
                        catch (Exception ex)
                        {
                            _threadRunning = false;
                            throw new Exception($"Exception in ThreadCyclicExecutor: Name = <{exec.Name}>, Exception.Message = <{ex.Message}>, Exception.StackTrace = <{ex.StackTrace}>", ex);
                        }
                        finally
                        {
                            exec.Reset();
                        }  
                    }

                    if (exec.SingleRun)
                        exec.Enabled = false;
                }
            }
            _are.Set();
        }

        private void DelegateSingletonTimer(Execution exec)
        {
            try
            {
                exec.Action();
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in ThreadCyclicExecutor: Name = <{exec.Name}>, Exception.Message = <{ex.Message}>, Exception.StackTrace = <{ex.StackTrace}>", ex);
            }
            finally
            {
                exec.Reset();
                exec.ThreadIsRunning = false;
            }
        }
    }
}
